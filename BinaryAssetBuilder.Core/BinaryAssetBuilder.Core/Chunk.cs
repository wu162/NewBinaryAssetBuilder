using System;
using System.Collections.Generic;

namespace BinaryAssetBuilder.Core
{
	public class Chunk : IDisposable
	{
		private const int BufferSize = 1048576;

		private static Stack<byte[]> _BufferStack = new Stack<byte[]>();

		private byte[] _Data = GetBuffer();

		private int _BytesRead;

		public byte[] Data => _Data;

		public int BytesRead
		{
			get
			{
				return _BytesRead;
			}
			set
			{
				_BytesRead = value;
			}
		}

		private static byte[] GetBuffer()
		{
			lock (_BufferStack)
			{
				if (_BufferStack.Count == 0)
				{
					return new byte[1048576];
				}
				return _BufferStack.Pop();
			}
		}

		private static void ReleaseBuffer(byte[] buff)
		{
			lock (_BufferStack)
			{
				_BufferStack.Push(buff);
			}
		}

		public void Dispose()
		{
			if (_Data != null)
			{
				ReleaseBuffer(_Data);
				_Data = null;
			}
		}
	}
}
