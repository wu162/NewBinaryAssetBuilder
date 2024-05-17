using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using EALA.Metrics;

namespace BinaryAssetBuilder
{
	public class FileMetricsListener : IMetricsListener
	{
		private Dictionary<string, Metric> _Metrics = new Dictionary<string, Metric>();

		private string _FilePathStub;

		private MetricDescriptor[] _Descriptors;

		public FileMetricsListener(MetricDescriptor[] descriptors)
		{
			NameValueCollection nameValueCollection = (NameValueCollection)ConfigurationManager.GetSection("BABFileMetricsListener");
			_FilePathStub = nameValueCollection["filepathstub"];
			_Descriptors = descriptors;
		}

		public void Open()
		{
		}

		public void SubmitMetrics(Metric m)
		{
			_Metrics.Add(m.Name, m);
		}

		public void Close()
		{
			FlushMetrics();
			_Metrics.Clear();
		}

		public void FlushMetrics()
		{
			StringBuilder stringBuilder = new StringBuilder("Time,User,Machine,Map");
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendFormat("{0:G},{1},{2}", DateTime.Now, MetricManager.AppData.UserName, MetricManager.AppData.MachineName);
			MetricDescriptor[] descriptors = _Descriptors;
			foreach (MetricDescriptor metricDescriptor in descriptors)
			{
				stringBuilder.AppendFormat(",{0}", metricDescriptor.Name);
				Metric value = null;
				if (_Metrics.TryGetValue(metricDescriptor.Name, out value))
				{
					if (value.Descriptor.Type == MetricType.Duration)
					{
						stringBuilder2.AppendFormat(",{0}", TimeSpan.FromSeconds((double)value.Data));
					}
					else if (value.Descriptor.Type == MetricType.Enabled)
					{
						stringBuilder2.AppendFormat(",{0}", ((bool)value.Data) ? 1 : 0);
					}
					else
					{
						stringBuilder2.AppendFormat(",{0}", value.Data);
					}
				}
				else
				{
					stringBuilder2.Append(",");
				}
			}
			_Metrics.Clear();
			try
			{
				FileInfo fileInfo = new FileInfo(_FilePathStub + MetricManager.AppData.MachineName + ".csv");
				StreamWriter streamWriter = fileInfo.AppendText();
				if (fileInfo.Length == 0)
				{
					streamWriter.WriteLine(stringBuilder.ToString());
				}
				streamWriter.WriteLine(stringBuilder2.ToString());
				streamWriter.Close();
			}
			catch
			{
			}
		}
	}
}
