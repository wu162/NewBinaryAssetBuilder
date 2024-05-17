using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BinaryAssetBuilder.Core
{
	public class NoScrollRichTextBox : RichTextBox
	{
		private struct POINT
		{
			public long X;

			public long Y;
		}

		private const int EM_SETEVENTMASK = 1073;

		private const int EM_SETTYPOGRAPHYOPTIONS = 1226;

		private const int WM_SETREDRAW = 11;

		private const int PFM_ALIGNMENT = 8;

		private const int SCF_SELECTION = 1;

		private const int EM_GETSCROLLPOS = 1245;

		private const int EM_SETSCROLLPOS = 1246;

		private int updating;

		private int oldEventMask;

		private POINT _scrollpos = default(POINT);

		public void BeginUpdate()
		{
			updating++;
			if (updating <= 1)
			{
				SendMessage(new HandleRef(this, base.Handle), 1245, 0, ref _scrollpos);
				oldEventMask = SendMessage(new HandleRef(this, base.Handle), 1073, 0, 0);
				SendMessage(new HandleRef(this, base.Handle), 11, 0, 0);
			}
		}

		public void EndUpdate()
		{
			updating--;
			if (updating <= 0)
			{
				SendMessage(new HandleRef(this, base.Handle), 1246, 0, ref _scrollpos);
				SendMessage(new HandleRef(this, base.Handle), 11, 1, 0);
				SendMessage(new HandleRef(this, base.Handle), 1073, 0, oldEventMask);
			}
		}

		[DllImport("user32", CharSet = CharSet.Auto)]
		private static extern int SendMessage(HandleRef hWnd, int msg, int wParam, ref POINT lp);

		[DllImport("user32", CharSet = CharSet.Auto)]
		private static extern int SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);
	}
}
