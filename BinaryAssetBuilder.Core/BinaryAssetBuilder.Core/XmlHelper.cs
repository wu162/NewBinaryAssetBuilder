using System;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	public class XmlHelper
	{
		public static object ReadCollection(XmlReader reader, string name, Type type)
		{
			if (reader.Name != name)
			{
				return null;
			}
			reader.MoveToFirstAttribute();
			int num = reader.ReadContentAsInt();
			reader.MoveToElement();
			reader.Read();
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < num; i++)
			{
				if (Activator.CreateInstance(type) is IXmlSerializable xmlSerializable)
				{
					xmlSerializable.ReadXml(reader);
					arrayList.Add(xmlSerializable);
				}
			}
			reader.Read();
			return arrayList.ToArray(type);
		}

		public static string[] ReadStringArrayElement(XmlReader reader, string name)
		{
			if (reader.Name != name)
			{
				return null;
			}
			return reader.ReadElementString().Split(';');
		}

		public static void WriteCollection(XmlWriter writer, string name, ICollection collection)
		{
			if (collection == null)
			{
				return;
			}
			writer.WriteStartElement(name);
			writer.WriteAttributeString("c", collection.Count.ToString());
			foreach (object item in collection)
			{
				if (item is IXmlSerializable xmlSerializable)
				{
					xmlSerializable.WriteXml(writer);
				}
			}
			writer.WriteEndElement();
		}

		public static void WriteStringArrayElement(XmlWriter writer, string name, string[] strings)
		{
			if (strings != null)
			{
				writer.WriteElementString(name, string.Join(";", strings));
			}
		}
	}
}
