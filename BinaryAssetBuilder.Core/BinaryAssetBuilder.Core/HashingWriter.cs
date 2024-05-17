using System.IO;
using System.Text;

namespace BinaryAssetBuilder.Core
{
	internal class HashingWriter : TextWriter
	{
		private uint m_runningHash;

		private string m_leftoverChars = "";

		private static readonly int HashingSize = 512;

		public override Encoding Encoding => Encoding.Default;

		public HashingWriter(uint runningHash)
		{
			m_runningHash = runningHash;
		}

		public override void Write(char c)
		{
			Write(new string(c, 1));
		}

		public override void Write(char[] buffer, int index, int count)
		{
			Write(new string(buffer, index, count));
		}

		public override void Write(string str)
		{
			string text = str;
			if (m_leftoverChars.Length > 0)
			{
				text = m_leftoverChars + str;
			}
			if (text.Length < HashingSize)
			{
				m_leftoverChars = text;
				return;
			}
			for (int i = 0; i + HashingSize <= text.Length; i += HashingSize)
			{
				string text2 = text.Substring(i, HashingSize);
				m_runningHash = HashProvider.GetTextHash(m_runningHash, text2);
			}
			int num = text.Length % HashingSize;
			if (num == 0)
			{
				m_leftoverChars = "";
			}
			else
			{
				m_leftoverChars = text.Substring(text.Length - num, num);
			}
		}

		public uint GetFinalHash()
		{
			if (m_leftoverChars.Length > 0)
			{
				return HashProvider.GetTextHash(m_runningHash, m_leftoverChars);
			}
			return m_runningHash;
		}
	}
}
