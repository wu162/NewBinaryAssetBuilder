using System;
using System.Collections.Generic;
using System.IO;

namespace BinaryAssetBuilder.Core
{
	public class AsynchronousFileReader
	{
		private const int QueueSize = 1;

		private Queue<IAsyncResult> _JobQueue = new Queue<IAsyncResult>();

		private FileStream _Stream;

		private Chunk _CurrentChunk;

		public uint FileSize
		{
			get
			{
				if (_Stream == null)
				{
					return 0u;
				}
				return (uint)_Stream.Length;
			}
		}

		public Chunk CurrentChunk => _CurrentChunk;

		public AsynchronousFileReader(string path)
		{
			_Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8, useAsync: true);
			if (_Stream != null)
			{
				for (int i = 0; i < 1; i++)
				{
					Chunk chunk = new Chunk();
					IAsyncResult item = _Stream.BeginRead(chunk.Data, 0, chunk.Data.Length, null, chunk);
					_JobQueue.Enqueue(item);
				}
			}
		}

		public bool BeginRead()
		{
			if (_JobQueue.Count == 0 || _CurrentChunk != null || _Stream == null)
			{
				return false;
			}
			IAsyncResult asyncResult = _JobQueue.Dequeue();
			asyncResult.AsyncWaitHandle.WaitOne();
			_CurrentChunk = asyncResult.AsyncState as Chunk;
			_CurrentChunk.BytesRead = _Stream.EndRead(asyncResult);
			if (_CurrentChunk.BytesRead != 0)
			{
				return true;
			}
			_CurrentChunk.Dispose();
			while (_JobQueue.Count > 0)
			{
				IAsyncResult asyncResult2 = _JobQueue.Dequeue();
				asyncResult2.AsyncWaitHandle.WaitOne();
				_Stream.EndRead(asyncResult2);
				(asyncResult2.AsyncState as Chunk).Dispose();
			}
			_Stream.Close();
			_Stream = null;
			return false;
		}

		public void EndRead()
		{
			if (_CurrentChunk != null && _Stream != null)
			{
				_CurrentChunk.BytesRead = 0;
				_JobQueue.Enqueue(_Stream.BeginRead(_CurrentChunk.Data, 0, _CurrentChunk.Data.Length, null, _CurrentChunk));
				_CurrentChunk = null;
			}
		}
	}
}
