using System;
using System.Collections;
using System.Collections.Generic;

namespace EALA.Metrics
{
	public class MetricManager
	{
		private static MetricApplicationData m_AppData;

		private static ArrayList m_Listeners;

		private static bool m_SessionOpen;

		private static Dictionary<string, MetricDescriptor> _DescriptorMap;

		private static bool m_Enabled;

		public static bool Enabled
		{
			get
			{
				return m_Enabled;
			}
			set
			{
				m_Enabled = value;
			}
		}

		public static MetricApplicationData AppData => m_AppData;

		static MetricManager()
		{
			m_Listeners = new ArrayList();
			m_SessionOpen = false;
			_DescriptorMap = new Dictionary<string, MetricDescriptor>();
			m_Enabled = true;
			m_AppData = new MetricApplicationData();
		}

		public static void AddListener(IMetricsListener listener)
		{
			if (m_SessionOpen)
			{
				throw new ArgumentException("Cannot add a new listener while session is open.");
			}
			m_Listeners.Add(listener);
		}

		public static void OpenSession()
		{
			if (m_SessionOpen)
			{
				throw new ArgumentException("Session already opened.");
			}
			foreach (IMetricsListener listener in m_Listeners)
			{
				listener.Open();
			}
			m_SessionOpen = true;
			m_Enabled = true;
		}

		public static void CloseSession()
		{
			if (!m_SessionOpen)
			{
				throw new ArgumentException("Session not opened.");
			}
			foreach (IMetricsListener listener in m_Listeners)
			{
				listener.Close();
			}
			m_SessionOpen = false;
		}

		public static void Submit(string descriptorName, params object[] datalist)
		{
			MetricDescriptor value = null;
			if (_DescriptorMap.TryGetValue(descriptorName.ToLower(), out value))
			{
				SubmitInternal(value, datalist);
			}
		}

		public static void Submit(MetricDescriptor descriptor, params object[] datalist)
		{
			if (_DescriptorMap.ContainsKey(descriptor.Name.ToLower()))
			{
				SubmitInternal(descriptor, datalist);
			}
		}

		private static void SubmitInternal(MetricDescriptor descriptor, params object[] samples)
		{
			if (!m_SessionOpen || !m_Enabled || samples.Length == 0)
			{
				return;
			}
			object obj = samples[0];
			object obj2 = null;
			samples[0] = descriptor.Type switch
			{
				MetricType.Count => Convert.ToInt32(obj), 
				MetricType.Size => Convert.ToInt64(obj), 
				MetricType.Duration => (obj.GetType() != typeof(TimeSpan)) ? ((object)Convert.ToDouble(obj)) : ((object)((TimeSpan)obj).TotalSeconds), 
				MetricType.Enabled => Convert.ToBoolean(obj), 
				MetricType.Success => Convert.ToBoolean(obj), 
				MetricType.Name => Convert.ToString(obj), 
				MetricType.Ratio => Convert.ToSingle(obj), 
				_ => obj, 
			};
			foreach (IMetricsListener listener in m_Listeners)
			{
				listener.SubmitMetrics(new Metric(descriptor, samples));
			}
		}

		public static MetricDescriptor GetDescriptor(string name, MetricType type, string description)
		{
			string key = name.ToLower();
			MetricDescriptor value = null;
			if (!_DescriptorMap.TryGetValue(key, out value))
			{
				value = new MetricDescriptor(name, type, description);
				_DescriptorMap.Add(key, value);
			}
			return value;
		}
	}
}
