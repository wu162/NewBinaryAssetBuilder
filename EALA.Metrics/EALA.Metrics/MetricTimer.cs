using System;

namespace EALA.Metrics
{
	public class MetricTimer : IDisposable
	{
		private DateTime m_StartTime = DateTime.Now;

		private string m_UserData;

		private string m_DescriptorName;

		public MetricTimer(string descriptorName, string userData)
		{
			m_DescriptorName = descriptorName;
			m_UserData = userData;
		}

		public MetricTimer(MetricDescriptor descriptor, string userData)
			: this(descriptor.Name, userData)
		{
		}

		public MetricTimer(string descriptorName)
			: this(descriptorName, null)
		{
		}

		public MetricTimer(MetricDescriptor descriptor)
			: this(descriptor.Name, null)
		{
		}

		public void Dispose()
		{
			TimeSpan timeSpan = DateTime.Now - m_StartTime;
			if (!string.IsNullOrEmpty(m_UserData))
			{
				MetricManager.Submit(m_DescriptorName, timeSpan, m_UserData);
			}
			else
			{
				MetricManager.Submit(m_DescriptorName, timeSpan);
			}
		}
	}
}
