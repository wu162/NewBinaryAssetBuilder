using System;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace BinaryAssetBuilder
{
	public class SerializedSectionHandler : IConfigurationSectionHandler
	{
		public object Create(object parent, object configContext, XmlNode section)
		{
			XmlAttribute xmlAttribute = section.Attributes["serializedType"];
			if (xmlAttribute == null)
			{
				throw new ConfigurationErrorsException("Serialized type not specified.", section);
			}
			string value = xmlAttribute.Value;
			Type type = Type.GetType(value, throwOnError: false);
			if (type == null)
			{
				throw new ConfigurationErrorsException("Serialized type not found or can not be instantiated");
			}
			XmlElement xmlElement = section.OwnerDocument.CreateElement(type.Name);
			while (section.FirstChild != null)
			{
				xmlElement.AppendChild(section.FirstChild);
			}
			while (section.Attributes.Count != 0)
			{
				xmlElement.Attributes.Append(section.Attributes[0]);
			}
			section.AppendChild(xmlElement);
			object obj = null;
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(type);
				XmlNodeReader xmlNodeReader = new XmlNodeReader(xmlElement);
				obj = xmlSerializer.Deserialize(xmlNodeReader);
				xmlNodeReader.Close();
				return obj;
			}
			catch (XmlException inner)
			{
				throw new ConfigurationErrorsException("XML parsing error", inner);
			}
		}
	}
}
