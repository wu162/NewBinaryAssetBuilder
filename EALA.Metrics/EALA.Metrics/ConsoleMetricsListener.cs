using System;
using System.Collections;
using System.IO;

namespace EALA.Metrics
{
	public class ConsoleMetricsListener : IMetricsListener
	{
		private TextWriter m_OutputStream;

		private ArrayList m_ConsoleBuffer = new ArrayList();

		public ConsoleMetricsListener()
		{
			m_OutputStream = Console.Out;
		}

		public ConsoleMetricsListener(TextWriter output)
		{
			m_OutputStream = output;
		}

		public void Open()
		{
		}

		public void Close()
		{
			foreach (string item in m_ConsoleBuffer)
			{
				Console.WriteLine(item);
			}
		}

		public void SubmitMetrics(Metric m)
		{
			m_ConsoleBuffer.Add(m.ToString());
		}
	}
}
