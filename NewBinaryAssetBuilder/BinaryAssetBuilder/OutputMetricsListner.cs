using System.Collections.Generic;
using BinaryAssetBuilder.Core;
using EALA.Metrics;

namespace BinaryAssetBuilder
{
	public class OutputMetricsListner : IMetricsListener
	{
		private List<string> m_ConsoleBuffer = new List<string>();

		private static Tracer _Tracer = Tracer.GetTracer("BinaryAssetBuilder", "Metrics");

		public void Open()
		{
		}

		public void Close()
		{
			foreach (string item in m_ConsoleBuffer)
			{
				_Tracer.Message(item);
			}
			m_ConsoleBuffer.Clear();
		}

		public void SubmitMetrics(Metric m)
		{
			m_ConsoleBuffer.Add(m.ToString());
		}
	}
}
