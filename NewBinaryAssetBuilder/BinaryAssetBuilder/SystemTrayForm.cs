using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BinaryAssetBuilder
{
	public class SystemTrayForm : Form
	{
		private IContainer components;

		public NotifyIcon systemTrayIcon;

		private ContextMenuStrip systemTrayContextMenu;

		private ToolStripMenuItem exitToolStripMenuItem;

		public SystemTrayForm()
		{
			InitializeComponent();
			base.WindowState = FormWindowState.Minimized;
			Hide();
			base.Resize += SystemTrayForm_Resize;
			base.Move += SystemTrayForm_Move;
			base.Shown += SystemTrayForm_Shown;
		}

		private void SystemTrayForm_Shown(object sender, EventArgs e)
		{
			Hide();
		}

		private void SystemTrayForm_Move(object sender, EventArgs e)
		{
			Hide();
		}

		private void SystemTrayForm_Resize(object sender, EventArgs e)
		{
			Hide();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemTrayForm));
			this.systemTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.systemTrayContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.systemTrayContextMenu.SuspendLayout();
			base.SuspendLayout();
			this.systemTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			this.systemTrayIcon.BalloonTipText = "BinaryAssetBuilder Background Process";
			this.systemTrayIcon.ContextMenuStrip = this.systemTrayContextMenu;
			// this.systemTrayIcon.Icon = (System.Drawing.Icon)resources.GetObject("systemTrayIcon.Icon");
			this.systemTrayIcon.Text = "BinaryAssetBuilder";
			this.systemTrayIcon.Visible = true;
			this.systemTrayContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.exitToolStripMenuItem });
			this.systemTrayContextMenu.Name = "systemTrayContextMenu";
			this.systemTrayContextMenu.Size = new System.Drawing.Size(104, 26);
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
			this.exitToolStripMenuItem.Text = "Exit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(exitToolStripMenuItem_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(292, 266);
			base.Name = "SystemTrayForm";
			base.ShowInTaskbar = false;
			this.Text = "SystemTrayForm";
			this.systemTrayContextMenu.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
