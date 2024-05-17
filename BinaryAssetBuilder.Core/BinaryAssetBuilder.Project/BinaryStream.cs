using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Project
{
	[Serializable]
	[DebuggerStepThrough]
	[XmlType(Namespace = "urn:xmlns:ea.com:babproject")]
	[GeneratedCode("xsd", "2.0.50727.42")]
	[DesignerCategory("code")]
	public class BinaryStream
	{
		private StreamConfiguration[] configurationField;

		private string sourceField;

		[XmlElement("Configuration")]
		public StreamConfiguration[] Configuration
		{
			get
			{
				return configurationField;
			}
			set
			{
				configurationField = value;
			}
		}

		[XmlAttribute]
		public string Source
		{
			get
			{
				return sourceField;
			}
			set
			{
				sourceField = value;
			}
		}
	}
}
