using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[XmlType("Type")]
	public class StringHashBinDescriptor
	{
		private string _SchemaTypeName;

		private string _BinName;

		private bool _CaseSensitive;

		[XmlAttribute("SchemaType")]
		public string SchemaTypeName
		{
			get
			{
				return _SchemaTypeName;
			}
			set
			{
				_SchemaTypeName = value;
			}
		}

		[XmlAttribute("Bin")]
		public string BinName
		{
			get
			{
				return _BinName;
			}
			set
			{
				_BinName = value;
			}
		}

		[XmlAttribute("CaseSensitivity")]
		public bool CaseSensitivity
		{
			get
			{
				return _CaseSensitive;
			}
			set
			{
				_CaseSensitive = value;
			}
		}
	}
}
