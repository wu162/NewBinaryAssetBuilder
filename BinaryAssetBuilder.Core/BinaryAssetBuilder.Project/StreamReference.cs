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
	public class StreamReference
	{
		private string referenceNameField;

		private string referenceConfigurationField;

		private string referenceManifestField;

		[XmlAttribute]
		public string ReferenceName
		{
			get
			{
				return referenceNameField;
			}
			set
			{
				referenceNameField = value;
			}
		}

		[XmlAttribute]
		public string ReferenceConfiguration
		{
			get
			{
				return referenceConfigurationField;
			}
			set
			{
				referenceConfigurationField = value;
			}
		}

		[XmlAttribute]
		public string ReferenceManifest
		{
			get
			{
				return referenceManifestField;
			}
			set
			{
				referenceManifestField = value;
			}
		}
	}
}
