using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BinaryAssetBuilder.Utility;

namespace BinaryAssetBuilder.Core
{
	public class PathMonitor
	{
		private static Tracer _Tracer = Tracer.GetTracer("PathMonitor", "Monitors paths for file changes");

		public static readonly int EventLimit = 20;

		private List<FileSystemWatcher> _Watchers;

		private List<string> _ChangedFiles;

		private int _NumEvents;

		private bool _UnrecoverableErrorOccured;

		public PathMonitor(string[] pathsToMonitor)
		{
			_ChangedFiles = new List<string>();
			_Watchers = new List<FileSystemWatcher>(pathsToMonitor.Length);
			_NumEvents = 0;
			_UnrecoverableErrorOccured = false;
			foreach (string path in pathsToMonitor)
			{
				FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path);
				fileSystemWatcher.Changed += OnChanged;
				fileSystemWatcher.Deleted += OnChanged;
				fileSystemWatcher.Renamed += OnRenamed;
				fileSystemWatcher.Error += OnError;
				fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite;
				fileSystemWatcher.EnableRaisingEvents = true;
				_Watchers.Add(fileSystemWatcher);
			}
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			_NumEvents++;
			_ChangedFiles.Add(e.FullPath);
		}

		private void OnRenamed(object source, RenamedEventArgs e)
		{
			_NumEvents++;
			_ChangedFiles.Add(e.OldFullPath);
		}

		private void OnError(object source, ErrorEventArgs e)
		{
			_UnrecoverableErrorOccured = true;
		}

		private void Flush()
		{
			foreach (FileSystemWatcher watcher in _Watchers)
			{
				FileSystemUtils.FlushVolume(watcher.Path[0]);
			}
		}

		public void Reset()
		{
			_NumEvents = 0;
			_ChangedFiles.Clear();
		}

		public bool IsResultTrustable()
		{
			if (!_UnrecoverableErrorOccured)
			{
				return _NumEvents < EventLimit;
			}
			return false;
		}

		public List<string> GetChangedFiles()
		{
			try
			{
				Flush();
			}
			catch (Exception ex)
			{
				_Tracer.TraceWarning("BinaryAssetBuilder was unsuccessful at flushing a monitored disk volume.\n  It is likely that another application has an open handle to this volume - please close this application or move your repository to another drive.  Path monitoring will be disabled.\n" + ex.Message);
				return new List<string>();
			}
			Thread.Sleep(100);
			return new List<string>(_ChangedFiles);
		}
	}
}
