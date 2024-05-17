using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class FileItem : IXmlSerializable
	{
		private FileHashItem _hashItem;

		private AssetDeclarationDocument _document;

		public FileHashItem HashItem
		{
			get
			{
				return _hashItem;
			}
			set
			{
				_hashItem = value;
			}
		}

		public AssetDeclarationDocument Document
		{
			get
			{
				return _document;
			}
			set
			{
				_document = value;
			}
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("fi");
			HashItem.WriteXml(writer);
			if (Document != null)
			{
				Document.WriteXml(writer);
			}
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.Read();
			HashItem = new FileHashItem();
			HashItem.ReadXml(reader);
			if (reader.Name == "ad")
			{
				Document = new AssetDeclarationDocument();
				Document.ReadXml(reader);
			}
			reader.Read();
		}
	}
}
