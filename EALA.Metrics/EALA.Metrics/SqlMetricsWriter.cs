using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Security.Principal;

namespace EALA.Metrics
{
	public class SqlMetricsWriter
	{
		private int m_SessionId;

		private Hashtable m_MetricDescriptors = new Hashtable();

		private string m_UserName = WindowsIdentity.GetCurrent().Name.ToString();

		private string m_MachineName = Environment.MachineName;

		private string m_BuildInfo = "";

		private static string m_SqlParameters;

		public SqlMetricsWriter(string applicationName, string applicationDescription, string applicationVersion, string projectName, string projectDescription, string databaseServer, string databaseName, string databaseUser, string databasePassword)
		{
			SetUpDatabase(applicationName, applicationDescription, applicationVersion, projectName, projectDescription, databaseServer, databaseName, databaseUser, databasePassword);
		}

		public SqlMetricsWriter(string applicationName, string applicationVersion, string projectName, string databaseServer, string databaseName, string databaseUser, string databasePassword)
		{
			SetUpDatabase(applicationName, "", applicationVersion, projectName, "", databaseServer, databaseName, databaseUser, databasePassword);
		}

		private void SetUpDatabase(string applicationName, string applicationDescription, string applicationVersion, string projectName, string projectDescription, string databaseServer, string databaseName, string databaseUser, string databasePassword)
		{
			m_SqlParameters = $"data source=\"{databaseServer}\";initial catalog={databaseName};user id={databaseUser};pwd={databasePassword}";
			m_SessionId = CreateSession(applicationName, applicationVersion, applicationDescription, projectName, projectDescription, m_UserName, m_MachineName, m_BuildInfo);
		}

		private int CreateSession(string applicationName, string applicationVersion, string applicationDescription, string projectName, string projectDescription, string username, string machineName, string buildInfo)
		{
			int num = 0;
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(m_SqlParameters);
				sqlConnection.Open();
				string cmdText = "InsertSession";
				SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
				sqlCommand.CommandType = CommandType.StoredProcedure;
				sqlCommand.Parameters.AddWithValue("@ApplicationName", applicationName);
				sqlCommand.Parameters.AddWithValue("@ApplicationVersion", applicationVersion);
				if (applicationDescription.Length > 0)
				{
					sqlCommand.Parameters.AddWithValue("@ApplicationDescription", applicationDescription);
				}
				sqlCommand.Parameters.AddWithValue("@ProjectName", projectName);
				if (projectDescription.Length > 0)
				{
					sqlCommand.Parameters.AddWithValue("@ProjectDescription", projectDescription);
				}
				sqlCommand.Parameters.AddWithValue("@Username", username);
				sqlCommand.Parameters.AddWithValue("@MachineName", machineName);
				sqlCommand.Parameters.AddWithValue("BuildInfo", buildInfo);
				SqlParameter sqlParameter = new SqlParameter("@SessionId", SqlDbType.Int);
				sqlParameter.Direction = ParameterDirection.Output;
				sqlCommand.Parameters.Add(sqlParameter);
				sqlCommand.ExecuteNonQuery();
				return (int)sqlParameter.Value;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void UpdateMetricDescriptors()
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(m_SqlParameters);
				sqlConnection.Open();
				string cmdText = "RetrieveMetricNameId";
				ArrayList arrayList = new ArrayList(m_MetricDescriptors.Keys);
				foreach (MetricDescriptor item in arrayList)
				{
					SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlCommand.CommandType = CommandType.StoredProcedure;
					sqlCommand.Parameters.AddWithValue("@MetricName", item.Name);
					sqlCommand.Parameters.AddWithValue("@MetricType", item.Type.ToString());
					sqlCommand.Parameters.AddWithValue("@MetricTypeEnumId", item.Type.GetHashCode());
					if (item.Description.Length > 0)
					{
						sqlCommand.Parameters.AddWithValue("@MetricDescription", item.Description);
					}
					SqlParameter sqlParameter = new SqlParameter("@MetricNameId", SqlDbType.Int);
					sqlParameter.Direction = ParameterDirection.Output;
					sqlCommand.Parameters.Add(sqlParameter);
					sqlCommand.ExecuteNonQuery();
					m_MetricDescriptors[item] = (int)sqlParameter.Value;
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void PopulateMetricDescriptors(ICollection metrics)
		{
			foreach (Metric metric in metrics)
			{
				m_MetricDescriptors[metric.Descriptor] = null;
			}
		}

		private void LookUpMetricDescriptors(ICollection metrics)
		{
			PopulateMetricDescriptors(metrics);
			UpdateMetricDescriptors();
		}

		private void InsertMetricValueIntoDatabase(Metric m, int metricEntryId)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(m_SqlParameters);
				sqlConnection.Open();
				string cmdText = "InsertMetricValueOnly";
				bool flag = false;
				object[] dataList = m.DataList;
				foreach (object obj in dataList)
				{
					if (!flag)
					{
						flag = true;
						continue;
					}
					SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlCommand.CommandType = CommandType.StoredProcedure;
					Type type = obj.GetType();
					SqlParameter sqlParameter = new SqlParameter();
					SqlParameter sqlParameter2 = new SqlParameter();
					SqlParameter sqlParameter3 = new SqlParameter();
					SqlParameter sqlParameter4 = new SqlParameter();
					SqlParameter sqlParameter5 = new SqlParameter();
					sqlParameter.ParameterName = "@IntValue";
					sqlParameter2.ParameterName = "@StringValue";
					sqlParameter3.ParameterName = "@FloatValue";
					sqlParameter4.ParameterName = "@DtValue";
					sqlParameter5.ParameterName = "@LongValue";
					sqlParameter.Value = "0";
					sqlParameter2.Value = string.Empty;
					sqlParameter3.Value = "0";
					sqlParameter4.Value = DateTime.Parse("1/1/1753 12:00:00 AM");
					sqlParameter5.Value = "0";
					if (type == typeof(int))
					{
						sqlParameter.Value = ((int)obj).ToString();
					}
					else if (type == typeof(bool))
					{
						if ((bool)obj)
						{
							sqlParameter.Value = "1";
						}
						else
						{
							sqlParameter.Value = "0";
						}
					}
					else if (type == typeof(string))
					{
						sqlParameter2.Value = (string)obj;
					}
					else if (type == typeof(float))
					{
						sqlParameter3.Value = ((float)obj).ToString();
					}
					else if (type == typeof(double))
					{
						sqlParameter3.Value = ((double)obj).ToString();
					}
					else if (type == typeof(TimeSpan))
					{
						sqlParameter3.Value = ((TimeSpan)obj).TotalSeconds.ToString();
					}
					else if (type == typeof(long))
					{
						sqlParameter5.Value = ((long)obj).ToString();
					}
					sqlCommand.Parameters.Add(sqlParameter);
					sqlCommand.Parameters.Add(sqlParameter2);
					sqlCommand.Parameters.Add(sqlParameter3);
					sqlCommand.Parameters.Add(sqlParameter4);
					sqlCommand.Parameters.Add(sqlParameter5);
					sqlCommand.Parameters.AddWithValue("MetricEntryId", metricEntryId);
					sqlCommand.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void InsertMetricsIntoDatabase(ICollection metrics)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(m_SqlParameters);
				sqlConnection.Open();
				string cmdText = "InsertMetricValue";
				foreach (Metric metric in metrics)
				{
					SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlCommand.CommandType = CommandType.StoredProcedure;
					Type type = metric.Data.GetType();
					SqlParameter sqlParameter = new SqlParameter();
					SqlParameter sqlParameter2 = new SqlParameter();
					SqlParameter sqlParameter3 = new SqlParameter();
					SqlParameter sqlParameter4 = new SqlParameter();
					SqlParameter sqlParameter5 = new SqlParameter();
					sqlParameter.ParameterName = "@IntValue";
					sqlParameter2.ParameterName = "@StringValue";
					sqlParameter3.ParameterName = "@FloatValue";
					sqlParameter4.ParameterName = "@DtValue";
					sqlParameter5.ParameterName = "@LongValue";
					sqlParameter.Value = "0";
					sqlParameter2.Value = string.Empty;
					sqlParameter3.Value = "0";
					sqlParameter4.Value = DateTime.Parse("1/1/1753 12:00:00 AM");
					sqlParameter5.Value = "0";
					if (type == typeof(int))
					{
						sqlParameter.Value = ((int)metric.Data).ToString();
					}
					else if (type == typeof(bool))
					{
						if ((bool)metric.Data)
						{
							sqlParameter.Value = "1";
						}
						else
						{
							sqlParameter.Value = "0";
						}
					}
					else if (type == typeof(string))
					{
						sqlParameter2.Value = (string)metric.Data;
					}
					else if (type == typeof(float))
					{
						sqlParameter3.Value = ((float)metric.Data).ToString();
					}
					else if (type == typeof(double))
					{
						sqlParameter3.Value = ((double)metric.Data).ToString();
					}
					else if (type == typeof(TimeSpan))
					{
						sqlParameter3.Value = ((TimeSpan)metric.Data).TotalSeconds.ToString();
					}
					else if (type == typeof(long))
					{
						sqlParameter5.Value = ((long)metric.Data).ToString();
					}
					sqlCommand.Parameters.Add(sqlParameter);
					sqlCommand.Parameters.Add(sqlParameter2);
					sqlCommand.Parameters.Add(sqlParameter3);
					sqlCommand.Parameters.Add(sqlParameter4);
					sqlCommand.Parameters.Add(sqlParameter5);
					sqlCommand.Parameters.AddWithValue("@MetricNameId", m_MetricDescriptors[metric.Descriptor]);
					sqlCommand.Parameters.AddWithValue("@SessionId", m_SessionId);
					sqlCommand.Parameters.AddWithValue("@Timestamp", metric.TimeStamp);
					SqlParameter sqlParameter6 = new SqlParameter("@MetricEntryId", SqlDbType.Int);
					sqlParameter6.Direction = ParameterDirection.Output;
					sqlCommand.Parameters.Add(sqlParameter6);
					sqlCommand.ExecuteNonQuery();
					int metricEntryId = (int)sqlParameter6.Value;
					if (metric.DataList.Length > 1)
					{
						InsertMetricValueIntoDatabase(metric, metricEntryId);
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void Submit(ICollection metrics)
		{
			LookUpMetricDescriptors(metrics);
			InsertMetricsIntoDatabase(metrics);
		}
	}
}
