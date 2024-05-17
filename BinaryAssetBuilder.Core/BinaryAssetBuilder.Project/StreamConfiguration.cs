using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Project
{
	[Serializable]
	[XmlType(Namespace = "urn:xmlns:ea.com:babproject")]
	[DesignerCategory("code")]
	[GeneratedCode("xsd", "2.0.50727.42")]
	[DebuggerStepThrough]
	public class StreamConfiguration
	{
		private string[] baseStreamSearchPathField;

		private StreamReference[] streamReferenceField;

		private string nameField;

		private bool defaultField;

		private string patchStreamField;

		private string relativeBasePathField;

		[XmlElement("BaseStreamSearchPath")]
		public string[] BaseStreamSearchPath
		{
			get
			{
				return baseStreamSearchPathField;
			}
			set
			{
				baseStreamSearchPathField = value;
			}
		}

		[XmlElement("StreamReference")]
		public StreamReference[] StreamReference
		{
			get
			{
				return streamReferenceField;
			}
			set
			{
				streamReferenceField = value;
			}
		}

		[DefaultValue("")]
		[XmlAttribute]
		public string Name
		{
			get
			{
				return nameField;
			}
			set
			{
				nameField = value;
			}
		}

		[XmlAttribute]
		[DefaultValue(false)]
		public bool Default
		{
			get
			{
				return defaultField;
			}
			set
			{
				defaultField = value;
			}
		}

		[XmlAttribute]
		public string PatchStream
		{
			get
			{
				return patchStreamField;
			}
			set
			{
				patchStreamField = value;
			}
		}

		[XmlAttribute]
		public string RelativeBasePath
		{
			get
			{
				return relativeBasePathField;
			}
			set
			{
				relativeBasePathField = value;
			}
		}

		public StreamConfiguration()
		{
			nameField = "";
			defaultField = false;
		}
	}
}
