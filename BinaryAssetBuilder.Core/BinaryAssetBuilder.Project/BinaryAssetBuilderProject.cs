using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Project
{
	[Serializable]
	[DebuggerStepThrough]
	[GeneratedCode("xsd", "2.0.50727.42")]
	[XmlRoot(Namespace = "urn:xmlns:ea.com:babproject", IsNullable = false)]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true, Namespace = "urn:xmlns:ea.com:babproject")]
	public class BinaryAssetBuilderProject
	{
		private BinaryStream[] streamField;

		[XmlElement("Stream")]
		public BinaryStream[] Stream
		{
			get
			{
				return streamField;
			}
			set
			{
				streamField = value;
			}
		}
	}
}
