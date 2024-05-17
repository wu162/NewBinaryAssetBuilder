using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace BinaryAssetBuilder.Core
{
	public class FileNameXmlResolver : XmlUrlResolver
	{
		private string _BaseDirectory;

		private IList<PathMapItem> _Paths = new List2<PathMapItem>();

		public IList<PathMapItem> Paths => _Paths;

		public FileNameXmlResolver(string baseDirectory)
		{
			_BaseDirectory = baseDirectory;
		}

		[DllImport("shlwapi.dll")]
		private static extern bool PathRelativePathTo(StringBuilder result, string from, uint typeFrom, string to, uint typeTo);

		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			string empty = string.Empty;
			string sourceUri = absoluteUri.OriginalString;
			if (absoluteUri.Scheme == "data" || absoluteUri.Scheme == "art" || absoluteUri.Scheme == "audio")
			{
				empty = FileNameResolver.ResolvePath(_BaseDirectory, absoluteUri.OriginalString);
				absoluteUri = new Uri($"file://{empty}");
			}
			else
			{
				if (!(absoluteUri.Scheme == "file"))
				{
					throw new BinaryAssetBuilderException(ErrorCode.IllegalPath, "Illegal URI scheme used in XInclude statement: {0}", absoluteUri.OriginalString);
				}
				StringBuilder stringBuilder = new StringBuilder(256);
				if (!PathRelativePathTo(stringBuilder, _BaseDirectory, 16u, absoluteUri.LocalPath, 128u))
				{
					throw new BinaryAssetBuilderException(ErrorCode.IllegalPath, "Illegal absolute path used in XInclude statement: {0}", absoluteUri.AbsolutePath);
				}
				sourceUri = stringBuilder.ToString();
				empty = absoluteUri.LocalPath;
			}
			_Paths.Add(new PathMapItem(sourceUri, empty));
			return base.GetEntity(absoluteUri, role, ofObjectToReturn);
		}
	}
}
