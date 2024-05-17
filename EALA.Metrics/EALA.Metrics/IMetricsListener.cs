namespace EALA.Metrics
{
	public interface IMetricsListener
	{
		void Open();

		void Close();

		void SubmitMetrics(Metric m_);
	}
}
