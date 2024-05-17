using System;
using System.Reflection;
using System.Xml;

namespace BinaryAssetBuilder.Core
{
	public class XIncludingReaderWrapper
	{
		private static Tracer _Tracer = Tracer.GetTracer("XIncludingReader", "Provides xi:include reading functionality");

		private static Assembly _Lib = null;

		public static void LoadAssembly()
		{
			try
			{
				_Lib = Assembly.Load("Mvp.Xml");
				_Lib.GetType("Mvp.Xml.XInclude.XIncludingReader");
			}
			catch (Exception)
			{
				_Lib = null;
				_Tracer.TraceError("Could not load XIncludingReader from Mvp.Xml.Dll. xi:include is disabled");
			}
		}

		public static XmlReader GetReader(XmlReader reader, FileNameXmlResolver resolver)
		{
			try
			{
				if (_Lib != null)
				{
					Type type = _Lib.GetType("Mvp.Xml.XInclude.XIncludingReader");
					object[] args = new object[1] { reader };
					object obj = _Lib.CreateInstance("Mvp.Xml.XInclude.XIncludingReader", ignoreCase: false, BindingFlags.CreateInstance, null, args, null, null);
					object[] args2 = new object[1] { resolver };
					type.InvokeMember("XmlResolver", BindingFlags.SetProperty, null, obj, args2);
					return (XmlReader)obj;
				}
			}
			catch (Exception)
			{
				_Tracer.TraceError("Could not load XIncludingReader from Mvp.Xml.Dll. xi:include is disabled");
			}
			return null;
		}
	}
}
