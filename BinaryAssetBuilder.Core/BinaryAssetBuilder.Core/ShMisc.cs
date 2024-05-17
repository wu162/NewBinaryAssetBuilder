using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BinaryAssetBuilder.Core
{
	public static class ShMisc
	{
		private static class Shlwapi
		{
			[DllImport("shlwapi.dll")]
			public static extern int HashData(byte[] data, uint dataSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] hash, uint hashSize);

			[DllImport("shlwapi.dll")]
			public static extern string StrFormatByteSize64(long value, StringBuilder result, uint size);

			[DllImport("shlwapi.dll")]
			public static extern int SHAutoComplete(IntPtr hwndEdit, AutoComplete flags);

			[DllImport("shlwapi.dll")]
			public static extern DialogResult SHMessageBoxCheck(IntPtr hwnd, string text, string title, uint type, DialogResult defaultValue, string registryValue);
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindow(IntPtr wnd, uint cmd);

		[DllImport("ole32.dll")]
		public static extern int CoInitialize(IntPtr unused);

		public static bool HashData(byte[] data, byte[] hash)
		{
			return Shlwapi.HashData(data, (uint)data.Length, hash, (uint)hash.Length) != 0;
		}

		public static string StringFormatByteSize(long value)
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			return Shlwapi.StrFormatByteSize64(value, stringBuilder, (uint)stringBuilder.Capacity);
		}

		public static bool SetAutoComplete(ComboBox control, AutoComplete flags)
		{
			Application.OleRequired();
			IntPtr window = GetWindow(control.Handle, 5u);
			Shlwapi.SHAutoComplete(window, flags);
			return true;
		}

		public static DialogResult MessageBoxCheck(Control parent, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icons, DialogResult defaultValue, string registryValue)
		{
			IntPtr hwnd = parent?.Handle ?? IntPtr.Zero;
			return Shlwapi.SHMessageBoxCheck(hwnd, text, title, (uint)buttons | (uint)icons, defaultValue, registryValue);
		}
	}
}
