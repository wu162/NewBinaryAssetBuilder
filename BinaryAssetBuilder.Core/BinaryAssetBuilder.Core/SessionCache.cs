using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using EALA.Metrics;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class SessionCache : ISessionCache
	{
		private class FileConfig
		{
			public FileItem LastCached;

			public List<FileItem> All = new List<FileItem>();
		}

		private class CurrentState
		{
			public IDictionary<string, FileConfig> FileConfigs;

			public IDictionary<string, FileItem> Files;

			public DateTime Created;

			public uint AssetCompilersVersion;

			public void FromScratch()
			{
				FileConfigs = new SortedDictionary<string, FileConfig>();
				Files = new SortedDictionary<string, FileItem>();
				Created = DateTime.Now;
				AssetCompilersVersion = 0u;
			}

			public void FromLast(LastState last)
			{
				FileConfigs = new SortedDictionary<string, FileConfig>();
				Files = new SortedDictionary<string, FileItem>();
				Created = last.Created;
				AssetCompilersVersion = last.AssetCompilersVersion;
				FileItem[] files = last.Files;
				foreach (FileItem fileItem in files)
				{
					string text = fileItem.HashItem.Path.ToLower();
					if (!FileConfigs.TryGetValue(text, out var value))
					{
						value = new FileConfig();
						FileConfigs[text] = value;
					}
					value.All.Add(fileItem);
					if (fileItem.Document != null)
					{
						value.LastCached = fileItem;
					}
					string postfix = GetPostfix(fileItem.HashItem.BuildConfiguration, fileItem.HashItem.TargetPlatform);
					Files[text + postfix] = fileItem;
				}
			}
		}

		[Serializable]
		private class LastState
		{
			private FileItem[] _files;

			private uint _version;

			private uint _assetCompilersVersion;

			private DateTime _created;

			private uint _documentProcessorVersion;

			public FileItem[] Files
			{
				get
				{
					return _files;
				}
				set
				{
					_files = value;
				}
			}

			public uint Version
			{
				get
				{
					return _version;
				}
				set
				{
					_version = value;
				}
			}

			public uint AssetCompilersVersion => _assetCompilersVersion;

			public DateTime Created
			{
				get
				{
					return _created;
				}
				set
				{
					_created = value;
				}
			}

			public uint DocumentProcessorVersion
			{
				get
				{
					return _documentProcessorVersion;
				}
				set
				{
					_documentProcessorVersion = value;
				}
			}

			public void FromCurrent(CurrentState current)
			{
				if (current.Files.Count > 0)
				{
					List<FileItem> list = new List<FileItem>();
					foreach (FileItem value in current.Files.Values)
					{
						if (value.HashItem.Exists)
						{
							list.Add(value);
						}
					}
					Files = list.ToArray();
				}
				else
				{
					Files = null;
				}
				Created = current.Created;
				Version = 18u;
				DocumentProcessorVersion = 11u;
				_assetCompilersVersion = current.AssetCompilersVersion;
			}

			public void ReadXml(XmlReader reader)
			{
				reader.MoveToAttribute("d");
				string[] array = reader.Value.Split(';');
				Created = Convert.ToDateTime(array[0]);
				Version = Convert.ToUInt32(array[1]);
				DocumentProcessorVersion = Convert.ToUInt32(array[2]);
				_assetCompilersVersion = Convert.ToUInt32(array[3]);
				reader.MoveToElement();
				reader.Read();
				Files = XmlHelper.ReadCollection(reader, "fic", typeof(FileItem)) as FileItem[];
				reader.Read();
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteStartElement("sc");
				writer.WriteAttributeString("d", $"{Created};{Version};{DocumentProcessorVersion};{AssetCompilersVersion}");
				XmlHelper.WriteCollection(writer, "fic", Files);
				writer.WriteEndElement();
			}
		}

		[NonSerialized]
		private const uint CacheVersion = 18u;

		[NonSerialized]
		private Tracer _Tracer = Tracer.GetTracer("SessionCache", "Provides caching functionality");

		[NonSerialized]
		private CurrentState _Current;

		private LastState _Last;

		private List<string> _dirtyStreams;

		private string _cacheFileName;

		private TimeSpan Age => DateTime.Now - _Last.Created;

		public virtual List<string> DirtyStreams => _dirtyStreams;

		public virtual string CacheFileName => _cacheFileName;

		public virtual uint AssetCompilersVersion
		{
			get
			{
				return _Current.AssetCompilersVersion;
			}
			set
			{
				_Current.AssetCompilersVersion = value;
			}
		}

		public virtual void LoadCache(string sessionCachePath)
		{
			if (!string.IsNullOrEmpty(sessionCachePath))
			{
				try
				{
					if (File.Exists(sessionCachePath + ".deflate"))
					{
						_Tracer.TraceInfo("Loading compressed XML cache");
						using Stream stream = new FileStream(sessionCachePath + ".deflate", FileMode.Open, FileAccess.Read, FileShare.Read);
						DeflateStream input = new DeflateStream(stream, CompressionMode.Decompress);
						XmlReader xmlReader = XmlReader.Create(input);
						xmlReader.Read();
						_Last = new LastState();
						_Last.ReadXml(xmlReader);
					}
					else if (File.Exists(sessionCachePath))
					{
						_Tracer.TraceInfo("Loading XML cache");
						using Stream input2 = new FileStream(sessionCachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
						XmlReader xmlReader2 = XmlReader.Create(input2);
						xmlReader2.Read();
						_Last = new LastState();
						_Last.ReadXml(xmlReader2);
					}
				}
				catch (Exception ex)
				{
					try
					{
						File.Move(sessionCachePath, sessionCachePath + ".corrupt");
					}
					catch
					{
					}
					try
					{
						File.Move(sessionCachePath + ".deflate", sessionCachePath + ".deflate.corrupt");
					}
					catch
					{
					}
					_Tracer.TraceInfo("Session cache file {1} could not be opened: {0} \nPlease rebuild", ex.Message, sessionCachePath);
					throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Session cache could not be opened. \nPlease rebuild");
				}
			}
			_cacheFileName = sessionCachePath;
			if (_Last != null)
			{
				_Tracer.TraceInfo("Session cache age is {0} days, {1} hours, {2} minutes.", Age.Days, Age.Hours, Age.Minutes);
			}
		}

		public virtual void InitializeCache(List<string> knownChangedFiles)
		{
			_Current = new CurrentState();
			_dirtyStreams = new List<string>();
			checkFiles(knownChangedFiles);
			if (_Last != null)
			{
				_Tracer.TraceInfo("Cached session data available.");
				_Current.FromLast(_Last);
			}
			else
			{
				_Tracer.TraceInfo("Cached session data not available.");
				_Current.FromScratch();
				_dirtyStreams = null;
			}
		}

		private void checkFiles(List<string> knownChangedFiles)
		{
			bool flag = knownChangedFiles.Count == 0;
			if (_Last == null)
			{
				return;
			}
			if (_Last.Version != 18)
			{
				_Tracer.TraceInfo("Session cache outdated. Version is {0}. Expected version is {1}.", _Last.Version, 18u);
				_Last = null;
				return;
			}
			if (_Last.DocumentProcessorVersion != 11)
			{
				_Tracer.TraceInfo("DocumentProcessor version mismatch. Version is {0}. Expected version is {1}.", _Last.DocumentProcessorVersion, 11u);
				_Last = null;
				return;
			}
			_Tracer.TraceInfo("Checking {0} files for updates.", _Last.Files.Length);
			int num = 0;
			FileItem[] files = _Last.Files;
			foreach (FileItem fileItem in files)
			{
				if (fileItem.Document != null)
				{
					fileItem.Document.ResetState();
				}
				if (fileItem.HashItem != null)
				{
					fileItem.HashItem.Reset();
				}
				if (!fileItem.HashItem.IsDirty)
				{
					continue;
				}
				if (_dirtyStreams != null)
				{
					if (fileItem.Document == null || fileItem.Document.StreamHints.Count == 0)
					{
						_Tracer.TraceInfo("Building all streams because {0} has no stream hints.", fileItem.HashItem.Path);
						_dirtyStreams = null;
					}
					else
					{
						foreach (string streamHint in fileItem.Document.StreamHints)
						{
							if (!_dirtyStreams.Contains(streamHint))
							{
								_dirtyStreams.Add(streamHint);
							}
						}
					}
				}
				num++;
				if (!flag && !knownChangedFiles.Contains(fileItem.HashItem.Path))
				{
					throw new BinaryAssetBuilderException(ErrorCode.PathMonitor, "Change went undetected by experimental PathMonitor.  Please notify the Sage Pipeline alias. File: " + fileItem.HashItem.Path);
				}
			}
		}

		public virtual void SaveCache(bool compressed)
		{
			MakeCacheable();
			string directoryName = Path.GetDirectoryName(CacheFileName);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string text = CacheFileName;
			if (compressed)
			{
				text += ".deflate";
			}
			using (Stream stream = new FileStream(text + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				Stream w = stream;
				if (compressed)
				{
					w = new DeflateStream(stream, CompressionMode.Compress);
				}
				XmlTextWriter xmlTextWriter = new XmlTextWriter(w, Encoding.UTF8);
				_Last.WriteXml(xmlTextWriter);
				xmlTextWriter.Flush();
				MetricManager.Submit("BAB.SessionCacheSize", stream.Position);
				xmlTextWriter.Close();
			}
			if (File.Exists(text))
			{
				if (File.Exists(text + ".old"))
				{
					File.Delete(text + ".old");
				}
				File.Move(text, text + ".old");
			}
			File.Move(text + ".tmp", text);
		}

		public virtual bool TryGetFile(string path, string configuration, TargetPlatform platform, out FileHashItem hashItem)
		{
			FileItem documentItem = null;
			TryGetFileItem(path, configuration, platform, out documentItem, autoCreateDocument: false);
			hashItem = documentItem.HashItem;
			return hashItem.Exists;
		}

		public virtual bool TryGetDocument(string path, string configuration, TargetPlatform platform, bool autoCreateDocument, out AssetDeclarationDocument document)
		{
			FileItem documentItem = null;
			TryGetFileItem(path, configuration, platform, out documentItem, autoCreateDocument);
			document = documentItem.Document;
			return documentItem.HashItem.Exists;
		}

		public virtual void SaveDocumentToCache(string path, string configuration, TargetPlatform platform, AssetDeclarationDocument document)
		{
			path = path.ToLower();
			configuration = configuration?.ToLower();
			string key = path + GetPostfix(configuration, platform);
			if (_Current.Files.TryGetValue(key, out var value))
			{
				value.Document = document;
			}
		}

		private static string GetPostfix(string configuration, TargetPlatform platform)
		{
			string text = (string.IsNullOrEmpty(configuration) ? "" : (":" + configuration));
			if (platform != 0)
			{
				text = text + ":" + (int)platform;
			}
			return text;
		}

		private bool TryGetFileItem(string path, string configuration, TargetPlatform platform, out FileItem documentItem, bool autoCreateDocument)
		{
			path = path.ToLower();
			configuration = configuration?.ToLower();
			string key = path + GetPostfix(configuration, platform);
			string key2 = path;
			documentItem = null;
			if (!_Current.Files.TryGetValue(key, out documentItem))
			{
				documentItem = new FileItem();
				documentItem.HashItem = new FileHashItem(path, configuration, platform);
				if (documentItem.HashItem.Exists)
				{
					_Current.Files[key] = documentItem;
					if (!_Current.FileConfigs.TryGetValue(key2, out var value))
					{
						value = new FileConfig();
						_Current.FileConfigs[key2] = value;
					}
					value.All.Add(documentItem);
					if (documentItem.Document != null)
					{
						value.LastCached = documentItem;
					}
				}
			}
			if (autoCreateDocument && documentItem.Document == null)
			{
				documentItem.Document = new AssetDeclarationDocument();
				if (!_Current.FileConfigs.TryGetValue(key2, out var value2))
				{
					value2 = new FileConfig();
					_Current.FileConfigs[key2] = value2;
				}
				value2.LastCached = documentItem;
			}
			return documentItem.HashItem.Exists;
		}

		private void MakeCacheable()
		{
			_Last = new LastState();
			_Last.FromCurrent(_Current);
		}
	}
}
