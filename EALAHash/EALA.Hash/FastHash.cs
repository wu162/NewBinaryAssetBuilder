using System;
using System.Runtime.InteropServices;

namespace EALAHash
{
	public class FastHash
	{
		public unsafe static uint GetHashCode(uint hash, byte[] buffer, int length)
		{
			fixed (byte* data = &buffer[0])
			{
				return FastHashInternal(hash, (sbyte*)data, (uint)length);
			}
		}

		public unsafe static uint GetHashCode(uint hash, byte[] buffer)
		{
			fixed (byte* data = &buffer[0])
			{
				return FastHashInternal(hash, (sbyte*)data, (uint)buffer.Length);
			}
		}

		public unsafe static uint GetHashCode(byte[] buffer, int length)
		{
			fixed (byte* data = &buffer[0])
			{
				return FastHashInternal((uint)buffer.Length, (sbyte*)data, (uint)length);
			}
		}

		public unsafe static uint GetHashCode(byte[] buffer)
		{
			fixed (byte* data = &buffer[0])
			{
				return FastHashInternal((uint)buffer.Length, (sbyte*)data, (uint)buffer.Length);
			}
		}

		public unsafe static uint GetHashCode(uint hash, string text)
		{
			IntPtr hglobal = Marshal.StringToHGlobalAnsi(text);
			uint result = FastHashInternal(hash, (sbyte*)hglobal.ToPointer(), (uint)text.Length);
			Marshal.FreeHGlobal(hglobal);
			return result;
		}

		public unsafe static uint GetHashCode(string text)
		{
			IntPtr hglobal = Marshal.StringToHGlobalAnsi(text);
			uint result = FastHashInternal((uint)text.Length, (sbyte*)hglobal.ToPointer(), (uint)text.Length);
			Marshal.FreeHGlobal(hglobal);
			return result;
		}
		
		internal unsafe static uint FastHashInternal(uint hash, sbyte* data, uint len)
		{
			if (len != 0 && data != null)
			{
				uint num = len & 3u;
				len >>= 2;
				if (len != 0)
				{
					do
					{
						hash = *(ushort*)data + hash;
						hash ^= (*(ushort*)(data + 2) ^ (hash << 5)) << 11;
						data += 4;
						hash = (hash >> 11) + hash;
						len--;
					}
					while (len != 0);
				}
				switch (num)
				{
					case 3u:
						hash = *(ushort*)data + hash;
						hash ^= ((uint)(data[2] << 2) ^ hash) << 16;
						hash = (hash >> 11) + hash;
						break;
					case 2u:
					{
						hash = *(ushort*)data + hash;
						uint num3 = hash;
						hash = num3 ^ (num3 << 11);
						hash = (hash >> 17) + hash;
						break;
					}
					case 1u:
					{
						hash = (uint)(*data) + hash;
						uint num2 = hash;
						hash = num2 ^ (num2 << 10);
						hash = (hash >> 1) + hash;
						break;
					}
				}
				uint num4 = hash;
				hash = num4 ^ (num4 << 3);
				hash = (hash >> 5) + hash;
				uint num5 = hash;
				hash = num5 ^ (num5 << 2);
				hash = (hash >> 15) + hash;
				return (hash << 10) ^ hash;
			}
			return 0u;
		}
	}
}
