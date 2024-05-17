namespace EALA.Metrics
{
	public class MetricDescriptor
	{
		private string m_Name;

		private MetricType m_Type;

		private string m_Description;

		public string Name => m_Name;

		public MetricType Type => m_Type;

		public string Description => m_Description;

		public MetricDescriptor(string name, MetricType type, string description)
		{
			m_Name = name;
			m_Type = type;
			m_Description = description;
		}
	}
}
