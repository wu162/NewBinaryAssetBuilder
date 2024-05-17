using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class InclusionItem : IXmlSerializable
	{
		private string _LogicalPath;

		private string _PhysicalPath;

		private InclusionType _Type;

		[NonSerialized]
		private AssetDeclarationDocument _Document;

		public string LogicalPath => _LogicalPath;

		public string PhysicalPath
		{
			get
			{
				return _PhysicalPath;
			}
			set
			{
				_PhysicalPath = value;
			}
		}

		public InclusionType Type => _Type;

		public AssetDeclarationDocument Document
		{
			get
			{
				return _Document;
			}
			set
			{
				_Document = value;
			}
		}

		public InclusionItem()
		{
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("ii");
			writer.WriteAttributeString("d", $"{_LogicalPath};{_PhysicalPath};{(int)_Type}");
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToAttribute("d");
			string[] array = reader.Value.Split(';');
			_LogicalPath = array[0];
			_PhysicalPath = array[1];
			_Type = (InclusionType)Convert.ToInt32(array[2]);
			reader.Read();
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public InclusionItem(string logicalPath, string physicalPath, InclusionType type)
		{
			_LogicalPath = logicalPath;
			_PhysicalPath = physicalPath;
			_Type = type;
		}
	}
}
