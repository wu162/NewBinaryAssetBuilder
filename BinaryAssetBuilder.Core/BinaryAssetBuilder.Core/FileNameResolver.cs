using System;
using System.IO;

namespace BinaryAssetBuilder.Core
{
	public class FileNameResolver
	{
		private static Tracer _Tracer = Tracer.GetTracer("FileNameResolver", "Resolves aliased path names specified in documents");

		private static char[] SplitCharacters = new char[1] { ':' };

		public static FileNameXmlResolver GetXmlResolver(string baseDirectory)
		{
			return new FileNameXmlResolver(baseDirectory);
		}

		private static string BuildPathProbe(string baseDirectory, string templatePath)
		{
			if (templatePath == "*")
			{
				return baseDirectory;
			}
			return Path.GetFullPath(Path.Combine(Settings.Current.DataRoot, templatePath));
		}

		public static string ResolvePath(string baseDirectory, string targetPath)
		{
			string text = null;
			if (!Path.IsPathRooted(baseDirectory))
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "An illegal relative path {0} is used as base directory. This is a bug in the code and needs to be fixed.", baseDirectory);
			}
			if (Path.IsPathRooted(targetPath))
			{
				text = targetPath;
			}
			else
			{
				string[] array = targetPath.Split(SplitCharacters);
				if (array.Length == 1)
				{
					text = Path.GetFullPath(Path.Combine(baseDirectory, targetPath));
				}
				else
				{
					if (array.Length > 2)
					{
						throw new BinaryAssetBuilderException(ErrorCode.IllegalPath, "An illegal path {0} is used as a reference to another asset or document.", baseDirectory);
					}
					string text2 = array[1].Replace('/', '\\');
					string text3 = array[0].ToLower();
					switch (text3)
					{
					case "art":
						if (string.IsNullOrEmpty(ShPath.RemoveFileSpec(text2)))
						{
							string[] artPaths = Settings.Current.ArtPaths;
							foreach (string text4 in artPaths)
							{
								string path = ((text4 == "*") ? baseDirectory : Path.Combine(text4, text2.Substring(0, 2)));
								if (!string.IsNullOrEmpty(Settings.Current.Postfix))
								{
									string path2 = $"{Path.GetFileNameWithoutExtension(text2)}_{Settings.Current.Postfix}{Path.GetExtension(text2)}";
									text = Path.GetFullPath(Path.Combine(path, path2));
									if (File.Exists(text))
									{
										break;
									}
								}
								text = Path.GetFullPath(Path.Combine(path, text2));
								if (File.Exists(text))
								{
									break;
								}
							}
						}
						else
						{
							text = SearchPaths(baseDirectory, text2, Settings.Current.ArtPaths);
						}
						break;
					case "data":
						text = SearchPaths(baseDirectory, text2, Settings.Current.DataPaths);
						break;
					case "audio":
						text = SearchPaths(baseDirectory, text2, Settings.Current.AudioPaths);
						break;
					case "root":
						text = Path.GetFullPath(Path.Combine(Settings.Current.DataRoot, text2));
						break;
					default:
						throw new BinaryAssetBuilderException(ErrorCode.IllegalPathAlias, "An illegal alias {0} is used in path {1}.", text3, targetPath);
					}
				}
			}
			_Tracer.TraceData("Result: {0}", text);
			return text;
		}

		public static string GetDataRoot(string filename)
		{
			string result = "";
			string[] dataPaths = Settings.Current.DataPaths;
			foreach (string text in dataPaths)
			{
				if (filename.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
				{
					result = text;
					break;
				}
			}
			return result;
		}

		private static string SearchPaths(string baseDirectory, string path, string[] includePaths)
		{
			string text = null;
			if (includePaths.Length > 1)
			{
				foreach (string text2 in includePaths)
				{
					text = ShPath.Canonicalize(Path.Combine((text2 == "*") ? baseDirectory : text2, path));
					if (File.Exists(text))
					{
						return text;
					}
				}
			}
			else if (includePaths.Length > 0)
			{
				text = ShPath.Canonicalize(Path.Combine((includePaths[0] == "*") ? baseDirectory : includePaths[0], path));
			}
			return text;
		}
	}
}
