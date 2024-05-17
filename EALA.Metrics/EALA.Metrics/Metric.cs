using System;
using System.Text;

namespace EALA.Metrics
{
	public class Metric
	{
		private MetricDescriptor m_descriptor;

		private object[] m_datalist;

		private DateTime m_timestamp;

		private static string[] _BytePostfix = new string[9] { "Bytes", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };

		public string Name => m_descriptor.Name;

		public MetricDescriptor Descriptor => m_descriptor;

		public DateTime TimeStamp => m_timestamp;

		public object Data => m_datalist[0];

		public object[] DataList => m_datalist;

		public Metric(MetricDescriptor md, params object[] datalist)
		{
			m_timestamp = DateTime.Now;
			m_datalist = datalist;
			m_descriptor = md;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("[{0:G}] {1}: ", m_timestamp, m_descriptor.Description);
			object obj = m_datalist[0];
			switch (m_descriptor.Type)
			{
			case MetricType.Duration:
				stringBuilder.Append(TimeSpan.FromSeconds((double)obj).ToString());
				break;
			case MetricType.Size:
			{
				double num = (long)obj;
				string[] bytePostfix = _BytePostfix;
				foreach (string arg in bytePostfix)
				{
					if (num < 1024.0)
					{
						stringBuilder.AppendFormat("{0:n} {1}", num, arg);
						break;
					}
					num /= 1024.0;
				}
				break;
			}
			default:
				stringBuilder.Append(obj);
				break;
			}
			if (m_datalist.Length > 1)
			{
				stringBuilder.AppendFormat(" [ {0}", m_datalist[1]);
				_ = string.Empty;
				for (int j = 2; j < m_datalist.Length; j++)
				{
					stringBuilder.AppendFormat(", {0}", m_datalist[j]);
				}
				stringBuilder.Append("]");
			}
			return stringBuilder.ToString();
		}
	}
}
