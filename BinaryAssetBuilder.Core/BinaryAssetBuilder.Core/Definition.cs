using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class Definition : IXmlSerializable
	{
		public string Name;

		public string EvaluatedValue;

		public bool IsOverride;

		[NonSerialized]
		public string OriginalValue;

		[NonSerialized]
		public AssetDeclarationDocument Document;

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("ud");
			writer.WriteAttributeString("d", $"{Name};{EvaluatedValue};{IsOverride}");
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToAttribute("d");
			string[] array = reader.Value.Split(';');
			Name = array[0];
			EvaluatedValue = array[1];
			IsOverride = Convert.ToBoolean(array[2]);
			reader.MoveToElement();
			reader.Read();
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public override string ToString()
		{
			return $"{Name} = {((EvaluatedValue != null) ? EvaluatedValue : OriginalValue)}";
		}
	}
}
