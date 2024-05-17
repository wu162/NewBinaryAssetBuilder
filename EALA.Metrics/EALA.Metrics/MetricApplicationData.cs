using System;
using System.Diagnostics;
using System.Security.Principal;

namespace EALA.Metrics
{
	public class MetricApplicationData
	{
		private string m_ApplicationName;

		private string m_Username;

		private string m_MachineName;

		public string ApplicationName
		{
			get
			{
				return m_ApplicationName;
			}
			set
			{
				m_ApplicationName = value;
			}
		}

		public string UserName
		{
			get
			{
				return m_Username;
			}
			set
			{
				m_Username = value;
			}
		}

		public string MachineName
		{
			get
			{
				return m_MachineName;
			}
			set
			{
				m_MachineName = value;
			}
		}

		public MetricApplicationData()
		{
			m_Username = WindowsIdentity.GetCurrent().Name.ToString();
			m_MachineName = Environment.MachineName.ToString();
			m_ApplicationName = Process.GetCurrentProcess().MainModule.FileName.ToString();
		}
	}
}
