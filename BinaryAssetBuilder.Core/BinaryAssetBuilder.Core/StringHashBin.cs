using System;
using System.Collections.Generic;
using System.Xml;

namespace BinaryAssetBuilder.Core
{
	internal class StringHashBin
	{
		private IDictionary<uint, string> _stringTable;

		private string _name;

		private bool _caseSensitive;

		private static readonly string StringTableEntryName = "StringAndHash";

		public StringHashBin(string name, bool caseSensitive)
		{
			_name = name;
			_stringTable = new SortedDictionary<uint, string>();
			_caseSensitive = caseSensitive;
		}

		public void RecordStringHash(uint hashCode, string text)
		{
			string value = null;
			if (_stringTable.TryGetValue(hashCode, out value))
			{
				if (!string.Equals(text, value, StringComparison.InvariantCultureIgnoreCase))
				{
					throw new BinaryAssetBuilderException(ErrorCode.HashCollision, "Hash collision detected: '{0}' and '{1}' share the same hash value 0x{2:x}.  If you believe this collision to be an error, please delete the string hash files from {3} and rebuild", text, value, hashCode, Settings.Current.SessionCacheDirectory);
				}
			}
			else
			{
				_stringTable.Add(hashCode, text);
			}
		}

		public void WriteStringHashTable(XmlWriter writer)
		{
			writer.WriteAttributeString("id", "StringHashBin_" + _name);
			writer.WriteAttributeString(HashProvider.StringBinEnumAttribute, _name);
			foreach (KeyValuePair<uint, string> item in _stringTable)
			{
				writer.WriteStartElement(StringTableEntryName);
				writer.WriteAttributeString("Hash", item.Key.ToString());
				writer.WriteAttributeString("Text", item.Value);
				writer.WriteEndElement();
			}
		}

		public void ReadStringHashTable(XmlElement table)
		{
			foreach (XmlNode childNode in table.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Element && childNode.Name.Equals(StringTableEntryName))
				{
					XmlElement xmlElement = (XmlElement)childNode;
					uint hashCode = Convert.ToUInt32(xmlElement.Attributes["Hash"].Value);
					string value = xmlElement.Attributes["Text"].Value;
					RecordStringHash(hashCode, value);
				}
			}
		}

		public bool IsCaseSensitive()
		{
			return _caseSensitive;
		}
	}
}
