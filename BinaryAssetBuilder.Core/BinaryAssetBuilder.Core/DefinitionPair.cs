using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class DefinitionPair : IXmlSerializable
	{
		public string Name;

		public string EvaluatedValue;

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("dp");
			writer.WriteAttributeString("d", $"{Name};{EvaluatedValue}");
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToAttribute("d");
			string[] array = reader.Value.Split(';');
			Name = array[0];
			EvaluatedValue = array[1];
			reader.MoveToElement();
			reader.Read();
		}

		public XmlSchema GetSchema()
		{
			return null;
		}
	}
}
