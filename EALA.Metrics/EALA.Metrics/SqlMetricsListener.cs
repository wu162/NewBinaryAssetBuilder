using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;

namespace EALA.Metrics
{
	public class SqlMetricsListener : IMetricsListener
	{
		private SqlMetricsWriter m_smw;

		private ListMetric m_Metrics = new ListMetric();

		public void Open()
		{
			try
			{
				Assembly entryAssembly = Assembly.GetEntryAssembly();
				AssemblyName name = entryAssembly.GetName();
				string name2 = name.Name;
				string applicationVersion = name.Version.ToString();
				NameValueCollection nameValueCollection = (NameValueCollection)ConfigurationManager.GetSection("SqlMetricsListener");
				string databaseServer = nameValueCollection["address"];
				string databaseName = nameValueCollection["database"];
				string databaseUser = nameValueCollection["user"];
				string databasePassword = nameValueCollection["password"];
				string projectName = nameValueCollection["projectname"];
				m_smw = new SqlMetricsWriter(name2, applicationVersion, projectName, databaseServer, databaseName, databaseUser, databasePassword);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void Close()
		{
			m_smw.Submit(m_Metrics.Metrics);
			m_smw = null;
		}

		public void SubmitMetrics(Metric m)
		{
			m_Metrics.AddMetric(m);
		}
	}
}
