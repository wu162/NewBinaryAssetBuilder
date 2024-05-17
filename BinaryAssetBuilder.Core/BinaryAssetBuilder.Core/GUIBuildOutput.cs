using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BinaryAssetBuilder.Core
{
	public class GUIBuildOutput : Form
	{
		public class LaunchLinkItem : MenuItem
		{
			private readonly string _Command;

			private readonly string _Argument;

			public LaunchLinkItem(string menuName, string command, string argument)
				: base(menuName)
			{
				_Command = command;
				_Argument = argument;
			}

			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);
				Process process = new Process();
				process.StartInfo.FileName = _Command;
				if (_Argument != null)
				{
					process.StartInfo.Arguments = _Argument;
				}
				process.StartInfo.CreateNoWindow = false;
				process.Start();
			}
		}

		private delegate void InvokeWriteStrDelegate(string s);

		private delegate void InvokeSetColorDelegate(Color color);

		private delegate void InvokeSetBoldDelegate(bool bold);

		private delegate void InvokeWriteDelegate(string source, TraceEventType eventType, string message);

		private delegate void EmptyInvokeDelegate();

		private string _CurClickedFilePath;

		private IContainer components;

		private NoScrollRichTextBox buildOutputText;

		private CheckBox saveOutputToFile;

		private ContextMenu linkContextMenu;

		private MenuItem openContainingFolder;

		private MenuItem openWith;

		public GUIBuildOutput()
		{
			InitializeComponent();
			buildOutputText.LinkClicked += buildOutputText_LinkClicked;
			buildOutputText.BackColor = Color.White;
		}

		private void buildOutputText_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			string text = (_CurClickedFilePath = e.LinkText);
			openWith.MenuItems.Clear();
			openWith.MenuItems.Add(new LaunchLinkItem("Default Application", text, null));
			openWith.MenuItems.Add(new MenuItem("-"));
			string extension = Path.GetExtension(text);
			RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(extension);
			RegistryKey registryKey2 = null;
			if (registryKey != null)
			{
				registryKey2 = registryKey.OpenSubKey("OpenWithList");
			}
			if (registryKey2 != null)
			{
				string[] subKeyNames = registryKey2.GetSubKeyNames();
				string[] array = subKeyNames;
				foreach (string text2 in array)
				{
					openWith.MenuItems.Add(new LaunchLinkItem(text2, text2, text));
				}
			}
			Point pos = buildOutputText.PointToClient(Cursor.Position);
			linkContextMenu.Show(buildOutputText, pos);
		}

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private void InvokeWrite(string s)
		{
			buildOutputText.BeginUpdate();
			int selectionStart = buildOutputText.SelectionStart;
			int selectionLength = buildOutputText.SelectionLength;
			bool flag = selectionStart == buildOutputText.TextLength;
			buildOutputText.AppendText(s);
			if (!flag)
			{
				buildOutputText.SelectionStart = selectionStart;
				buildOutputText.SelectionLength = selectionLength;
			}
			buildOutputText.EndUpdate();
			buildOutputText.Invalidate();
			if (flag)
			{
				buildOutputText.ScrollToCaret();
			}
		}

		public void Write(string s)
		{
			Invoke(new InvokeWriteStrDelegate(InvokeWrite), s);
		}

		private void InvokeSetColor(Color color)
		{
			buildOutputText.SelectionColor = color;
		}

		public void SetFontColor(Color color)
		{
			Invoke(new InvokeSetColorDelegate(InvokeSetColor), color);
		}

		private void InvokeSetBold(bool bold)
		{
			if (buildOutputText.Font.Bold != bold)
			{
				buildOutputText.SelectionFont = new Font(buildOutputText.Font, bold ? FontStyle.Bold : FontStyle.Regular);
			}
		}

		public void SetBold(bool bold)
		{
			Invoke(new InvokeSetBoldDelegate(InvokeSetBold), bold);
		}

		private void InvokeWrite(string source, TraceEventType eventType, string message)
		{
			bool bold = false;
			switch (eventType)
			{
			case TraceEventType.Information:
				SetFontColor(Color.Black);
				break;
			case TraceEventType.Warning:
				SetFontColor(Color.Green);
				break;
			case TraceEventType.Error:
				SetFontColor(Color.Orange);
				break;
			case TraceEventType.Critical:
				SetFontColor(Color.Red);
				bold = true;
				break;
			}
			SetBold(bold);
			if (eventType == TraceEventType.Information || eventType == TraceEventType.Verbose)
			{
				InvokeWrite($"{message}\n");
			}
			else
			{
				InvokeWrite($"{eventType}: {message}\n");
			}
			Update();
		}

		public void Write(string source, TraceEventType eventType, string message)
		{
			if (base.Created)
			{
				Invoke(new InvokeWriteDelegate(InvokeWrite), source, eventType, message);
			}
		}

		public void SaveAndOpenText()
		{
			if (base.Created)
			{
				Invoke(new EmptyInvokeDelegate(InvokeSaveAndOpenText));
			}
		}

		public void DiffThreadClose()
		{
			if (base.Created)
			{
				Invoke(new EmptyInvokeDelegate(base.Close));
			}
		}

		private void InvokeSaveAndOpenText()
		{
			string text = Path.GetTempFileName() + ".rtf";
			buildOutputText.SaveFile(text);
			Process process = new Process();
			process.StartInfo.FileName = text;
			process.StartInfo.CreateNoWindow = false;
			process.Start();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (saveOutputToFile.Checked)
			{
				InvokeSaveAndOpenText();
			}
			base.OnClosing(e);
		}

		private void openContainingFolder_Click(object sender, EventArgs e)
		{
			Process process = new Process();
			process.StartInfo.FileName = "explorer.exe";
			process.StartInfo.Arguments = $"/select,{_CurClickedFilePath}";
			process.StartInfo.CreateNoWindow = false;
			process.Start();
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
			this.buildOutputText = new BinaryAssetBuilder.Core.NoScrollRichTextBox();
			this.saveOutputToFile = new System.Windows.Forms.CheckBox();
			this.linkContextMenu = new System.Windows.Forms.ContextMenu();
			this.openContainingFolder = new System.Windows.Forms.MenuItem();
			this.openWith = new System.Windows.Forms.MenuItem();
			base.SuspendLayout();
			this.buildOutputText.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.buildOutputText.Font = new System.Drawing.Font("Courier New", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.buildOutputText.Location = new System.Drawing.Point(12, 12);
			this.buildOutputText.Name = "buildOutputText";
			this.buildOutputText.ReadOnly = true;
			this.buildOutputText.Size = new System.Drawing.Size(744, 315);
			this.buildOutputText.TabIndex = 0;
			this.buildOutputText.Text = "";
			this.saveOutputToFile.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			this.saveOutputToFile.AutoSize = true;
			this.saveOutputToFile.Location = new System.Drawing.Point(12, 334);
			this.saveOutputToFile.Name = "saveOutputToFile";
			this.saveOutputToFile.Size = new System.Drawing.Size(117, 17);
			this.saveOutputToFile.TabIndex = 1;
			this.saveOutputToFile.Text = "Save Output to File";
			this.saveOutputToFile.UseVisualStyleBackColor = true;
			this.linkContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[2] { this.openContainingFolder, this.openWith });
			this.openContainingFolder.Index = 0;
			this.openContainingFolder.Text = "Open Containing Folder";
			this.openContainingFolder.Click += new System.EventHandler(openContainingFolder_Click);
			this.openWith.Index = 1;
			this.openWith.Text = "Open With...";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(768, 363);
			base.Controls.Add(this.saveOutputToFile);
			base.Controls.Add(this.buildOutputText);
			base.Name = "GUIBuildOutput";
			this.Text = "Build Output";
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}
}
