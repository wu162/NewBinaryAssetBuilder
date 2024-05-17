using System.Collections;

namespace EALA.Metrics
{
	public class ListMetric
	{
		private ArrayList m_Metrics = new ArrayList();

		public ArrayList Metrics => m_Metrics;

		public void AddMetric(Metric m)
		{
			m_Metrics.Add(m);
		}
	}
}
