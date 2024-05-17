using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BinaryAssetBuilder.Core
{
	public static class ShPath
	{
		private static class User32
		{
			[DllImport("user32.dll")]
			public static extern IntPtr GetDC(IntPtr hwnd);

			[DllImport("user32.dll")]
			public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
		}

		private static class Gdi32
		{
			[DllImport("gdi32.dll")]
			public static extern bool DeleteDC(IntPtr hdc);

			[DllImport("gdi32.dll")]
			public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

			[DllImport("gdi32.dll")]
			public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hobject);
		}

		private static class Shlwapi
		{
			[DllImport("shlwapi.dll")]
			public static extern IntPtr PathAddBackslash(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathAddExtension(StringBuilder path, string extension);

			[DllImport("shlwapi.dll")]
			public static extern bool PathAppend(StringBuilder path, string more);

			[DllImport("shlwapi.dll")]
			public static extern string PathBuildRoot(StringBuilder result, int drive);

			[DllImport("shlwapi.dll")]
			public static extern bool PathCanonicalize(StringBuilder result, string path);

			[DllImport("shlwapi.dll")]
			public static extern IntPtr PathCombine(StringBuilder result, string directory, string file);

			[DllImport("shlwapi.dll")]
			public static extern bool PathCompactPath(IntPtr dc, StringBuilder result, uint width);

			[DllImport("shlwapi.dll")]
			public static extern bool PathCompactPathEx(StringBuilder result, string path, uint maxLength, uint flags);

			[DllImport("shlwapi.dll")]
			public static extern int PathCommonPrefix(string path1, string path2, StringBuilder result);

			[DllImport("shlwapi.dll")]
			public static extern bool PathFileExists(string path);

			[DllImport("shlwapi.dll")]
			public static extern string PathFindExtension(string path);

			[DllImport("shlwapi.dll")]
			public static extern string PathFindFileName(string path);

			[DllImport("shlwapi.dll")]
			public static extern string PathFindNextComponent(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathFindOnPath(StringBuilder file, string[] otherDirectories);

			[DllImport("shlwapi.dll")]
			public static extern string PathGetArgs(string path);

			[DllImport("shlwapi.dll")]
			public static extern string PathFindSuffixArray(string path, string[] suffixes, int arraySize);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsLFNFileSpec(string path);

			[DllImport("shlwapi.dll")]
			public static extern PathCharType PathGetCharType(char ch);

			[DllImport("shlwapi.dll")]
			public static extern int PathGetDriveNumber(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsDirectory(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsDirectoryEmpty(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsFileSpec(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsPrefix(string prefix, string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsRelative(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsRoot(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsSameRoot(string path1, string path2);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsUNC(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsNetworkPath(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsUNCServer(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsUNCServerShare(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsContentType(string path, string contentType);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsURL(string path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathMakePretty(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathMatchSpec(string file, string pszSpec);

			[DllImport("shlwapi.dll")]
			public static extern int PathParseIconLocation(StringBuilder pszIconFile);

			[DllImport("shlwapi.dll")]
			public static extern void PathQuoteSpaces(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathRelativePathTo(StringBuilder result, string from, FileAttributes fromAttributes, string to, FileAttributes toAttributes);

			[DllImport("shlwapi.dll")]
			public static extern void PathRemoveArgs(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern string PathRemoveBackslash(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern void PathRemoveBlanks(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern void PathRemoveExtension(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathRemoveFileSpec(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathRenameExtension(StringBuilder path, string extension);

			[DllImport("shlwapi.dll")]
			public static extern bool PathSearchAndQualify(string path, StringBuilder result, uint bufferSize);

			[DllImport("shlwapi.dll")]
			public static extern string PathSkipRoot(string path);

			[DllImport("shlwapi.dll")]
			public static extern void PathStripPath(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathStripToRoot(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern void PathUnquoteSpaces(StringBuilder lpsz);

			[DllImport("shlwapi.dll")]
			public static extern bool PathMakeSystemFolder(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathUnmakeSystemFolder(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathIsSystemFolder(string path, FileAttributes dwAttrb);

			[DllImport("shlwapi.dll")]
			public static extern void PathUndecorate(StringBuilder path);

			[DllImport("shlwapi.dll")]
			public static extern bool PathUnExpandEnvStrings(string path, StringBuilder result, uint cchBuf);
		}

		private const int MaxPath = 260;

		public static string AddBackslash(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!(Shlwapi.PathAddBackslash(stringBuilder) != IntPtr.Zero))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static bool AddBackslash(StringBuilder path)
		{
			return Shlwapi.PathAddBackslash(path) != IntPtr.Zero;
		}

		public static string PathAddExtension(string path, string extension)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathAddExtension(stringBuilder, extension))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static bool AddExtension(StringBuilder path, string extension)
		{
			return Shlwapi.PathAddExtension(path, extension);
		}

		public static string BuildRoot(int drive)
		{
			string text = Shlwapi.PathBuildRoot(new StringBuilder(260), drive);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return null;
		}

		public static string Canonicalize(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathCanonicalize(stringBuilder, path))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string Combine(string path, string more)
		{
			StringBuilder stringBuilder = new StringBuilder(260);
			if (!(Shlwapi.PathCombine(stringBuilder, path, more) != IntPtr.Zero))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static bool Append(StringBuilder path, string more)
		{
			return Shlwapi.PathAppend(path, more);
		}

		public static bool SetControlText(Control control, string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			IntPtr dC = User32.GetDC(((IWin32Window)control).Handle);
			IntPtr intPtr = Gdi32.CreateCompatibleDC(dC);
			User32.ReleaseDC(((IWin32Window)control).Handle, dC);
			Gdi32.SelectObject(intPtr, control.Font.ToHfont());
			bool flag = Shlwapi.PathCompactPath(intPtr, stringBuilder, (uint)(control.ClientSize.Width - 10));
			Gdi32.DeleteDC(intPtr);
			if (flag)
			{
				control.Text = stringBuilder.ToString();
			}
			return flag;
		}

		public static string CompactPath(string path, uint maxLength)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathCompactPathEx(stringBuilder, path, maxLength, 0u))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string GetCommonPrefix(string path1, string path2)
		{
			StringBuilder stringBuilder = new StringBuilder(260);
			if (Shlwapi.PathCommonPrefix(path1, path2, stringBuilder) <= 0)
			{
				return string.Empty;
			}
			return stringBuilder.ToString();
		}

		public static bool FileExists(string path)
		{
			return Shlwapi.PathFileExists(path);
		}

		public static string FindExtension(string path)
		{
			return Shlwapi.PathFindExtension(path);
		}

		public static string FindFileName(string path)
		{
			return Shlwapi.PathFindFileName(path);
		}

		public static string FindNextComponent(string path)
		{
			return Shlwapi.PathFindNextComponent(path);
		}

		public static string FindOnPath(string file)
		{
			return FindOnPath(file, null);
		}

		public static string FindOnPath(string file, string[] otherDirectories)
		{
			string[] array = null;
			if (otherDirectories != null)
			{
				if (otherDirectories.Length > 0 && otherDirectories[otherDirectories.Length - 1] == null)
				{
					array = otherDirectories;
				}
				else
				{
					array = new string[otherDirectories.Length + 1];
					Array.Copy(otherDirectories, array, otherDirectories.Length);
					array[otherDirectories.Length] = null;
				}
			}
			StringBuilder stringBuilder = new StringBuilder(file, 260);
			if (Shlwapi.PathFindOnPath(stringBuilder, array))
			{
				return stringBuilder.ToString();
			}
			return null;
		}

		public static string GetArgs(string path)
		{
			return Shlwapi.PathGetArgs(path);
		}

		public static string FindSuffixArray(string path, string[] suffixes)
		{
			if (suffixes == null)
			{
				return null;
			}
			return Shlwapi.PathFindSuffixArray(path, suffixes, suffixes.Length);
		}

		public static bool IsLongFileNameFileSpec(string path)
		{
			return Shlwapi.PathIsLFNFileSpec(path);
		}

		public static PathCharType GetCharType(char c)
		{
			return Shlwapi.PathGetCharType(c);
		}

		public static int GetDriveNumber(string path)
		{
			return Shlwapi.PathGetDriveNumber(path);
		}

		public static bool IsDirectory(string path)
		{
			return Shlwapi.PathIsDirectory(path);
		}

		public static bool IsDirectoryEmpty(string path)
		{
			return Shlwapi.PathIsDirectoryEmpty(path);
		}

		public static bool IsFileSpec(string path)
		{
			return Shlwapi.PathIsFileSpec(path);
		}

		public static bool IsPrefix(string prefix, string path)
		{
			return Shlwapi.PathIsFileSpec(path);
		}

		public static bool IsRelative(string path)
		{
			return Shlwapi.PathIsRelative(path);
		}

		public static bool IsRoot(string path)
		{
			return Shlwapi.PathIsRoot(path);
		}

		public static bool IsSameRoot(string path1, string path2)
		{
			return Shlwapi.PathIsSameRoot(path1, path2);
		}

		public static bool IsUNC(string path)
		{
			return Shlwapi.PathIsUNC(path);
		}

		public static bool IsNetworkPath(string path)
		{
			return Shlwapi.PathIsNetworkPath(path);
		}

		public static bool IsUNCServer(string path)
		{
			return Shlwapi.PathIsUNCServer(path);
		}

		public static bool IsUNCServerShare(string path)
		{
			return Shlwapi.PathIsUNCServerShare(path);
		}

		public static bool IsContentType(string path, string contentType)
		{
			return Shlwapi.PathIsContentType(path, contentType);
		}

		public static bool IsURL(string path)
		{
			return Shlwapi.PathIsURL(path);
		}

		public static string MakePretty(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathMakePretty(stringBuilder);
			return stringBuilder.ToString();
		}

		public static bool MatchSpec(string file, string spec)
		{
			return Shlwapi.PathMatchSpec(file, spec);
		}

		public static int ParseIconLocation(string path, out string parsedPath)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			int result = Shlwapi.PathParseIconLocation(stringBuilder);
			parsedPath = stringBuilder.ToString();
			return result;
		}

		public static string QuoteSpaces(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathQuoteSpaces(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RelativePathTo(string pathFrom, bool fromPathIsDirectory, string pathTo, bool toPathIsDirectory)
		{
			StringBuilder stringBuilder = new StringBuilder(260);
			if (!Shlwapi.PathRelativePathTo(stringBuilder, pathFrom, fromPathIsDirectory ? FileAttributes.Directory : FileAttributes.Normal, pathTo, toPathIsDirectory ? FileAttributes.Directory : FileAttributes.Normal))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string RemoveArgs(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathRemoveArgs(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RemoveBackslash(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathRemoveBackslash(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RemoveBlanks(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathRemoveBlanks(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RemoveExtension(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathRemoveExtension(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RemoveFileSpec(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathRemoveFileSpec(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string RenameExtension(string path, string newExtension)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (Shlwapi.PathRenameExtension(stringBuilder, newExtension))
			{
				return stringBuilder.ToString();
			}
			return null;
		}

		public static string SearchAndQualify(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathSearchAndQualify(path, stringBuilder, 260u))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string SkipRoot(string path)
		{
			return Shlwapi.PathSkipRoot(path);
		}

		public static string StripPath(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathStripPath(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string StripToRoot(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathStripToRoot(stringBuilder))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string UnquoteSpaces(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathUnquoteSpaces(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string MakeSystemFolder(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathMakeSystemFolder(stringBuilder))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static string UnmakeSystemFolder(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathUnmakeSystemFolder(stringBuilder))
			{
				return null;
			}
			return stringBuilder.ToString();
		}

		public static bool IsSystemFolder(string path, FileAttributes attributes)
		{
			return Shlwapi.PathIsSystemFolder(path, attributes);
		}

		public static string Undecorate(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			Shlwapi.PathUndecorate(stringBuilder);
			return stringBuilder.ToString();
		}

		public static string UnExpandEnvironmentStrings(string path)
		{
			StringBuilder stringBuilder = new StringBuilder(path, 260);
			if (!Shlwapi.PathUnExpandEnvStrings(path, stringBuilder, 260u))
			{
				return null;
			}
			return stringBuilder.ToString();
		}
	}
}
