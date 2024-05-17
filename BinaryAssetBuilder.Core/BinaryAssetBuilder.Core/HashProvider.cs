using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using EALAHash;

namespace BinaryAssetBuilder.Core
{
	public static class HashProvider
	{
		private static Tracer _Tracer;

		private static readonly string StringHashesFileDefault;

		public static string StringHashesFile;

		private static IDictionary<string, string> _SchemaNameToBinName;

		private static IDictionary<string, StringHashBin> _StringHashBins;

		private static readonly string InstanceIdTableName;

		private static readonly string TypeIdTableName;

		public static readonly string StringTableAssetName;

		public static readonly string StringBinEnumAttribute;

		private static readonly uint HashProviderVersion;

		private static string _outputDirectory;

		public static string GetOutputDirectory()
		{
			return _outputDirectory;
		}

		static HashProvider()
		{
			_Tracer = Tracer.GetTracer("HashProvider", "Centralized facility to produce hashes from strings");
			StringHashesFileDefault = "StringHashes.xml";
			StringHashesFile = StringHashesFileDefault;
			InstanceIdTableName = "INSTANCEID";
			TypeIdTableName = "TYPEID";
			StringTableAssetName = "StringHashTable";
			StringBinEnumAttribute = "StringHashBin";
			HashProviderVersion = GetTextHash(StringTableAssetName) ^ 2u;
		}

		public static void RecordHash(XmlSchemaType type, string str)
		{
			string value = null;
			if (_SchemaNameToBinName.TryGetValue(type.Name, out value))
			{
				RecordHash(value, str);
				return;
			}
			throw new BinaryAssetBuilderException(ErrorCode.InternalError, "StringHashBin does not exist for Schema type {0} but hashing was requested", type.Name);
		}

		public static void RecordHash(InstanceHandle handle)
		{
			_StringHashBins[InstanceIdTableName].RecordStringHash(handle.InstanceId, handle.InstanceName);
			_StringHashBins[TypeIdTableName].RecordStringHash(handle.TypeId, handle.TypeName);
		}

		public static void RecordHash(string binName, string str)
		{
			StringHashBin stringHashBin = _StringHashBins[binName];
			stringHashBin.RecordStringHash(GetTextHash(stringHashBin.IsCaseSensitive() ? str : str.ToLower()), str);
		}

		public static void InitializeStringHashes(string outputDir)
		{
			_outputDirectory = outputDir;
			_SchemaNameToBinName = new Dictionary<string, string>();
			_StringHashBins = new Dictionary<string, StringHashBin>();
			StringHashesFile = StringHashesFileDefault;
			_StringHashBins.Add(InstanceIdTableName, new StringHashBin(InstanceIdTableName, caseSensitive: false));
			_StringHashBins.Add(TypeIdTableName, new StringHashBin(TypeIdTableName, caseSensitive: true));
			StringHashBinDescriptor[] stringHashBinDescriptors = Settings.Current.StringHashBinDescriptors;
			foreach (StringHashBinDescriptor stringHashBinDescriptor in stringHashBinDescriptors)
			{
				_SchemaNameToBinName.Add(stringHashBinDescriptor.SchemaTypeName, stringHashBinDescriptor.BinName);
				_StringHashBins.Add(stringHashBinDescriptor.BinName, new StringHashBin(stringHashBinDescriptor.BinName, stringHashBinDescriptor.CaseSensitivity));
			}
			LoadPreviousStringHashes();
		}

		private static void LoadPreviousStringHashes()
		{
			string path = Path.Combine(_outputDirectory, StringHashesFile);
			if (!File.Exists(path))
			{
				path = Path.Combine(_outputDirectory, StringHashesFileDefault);
				if (!File.Exists(path))
				{
					return;
				}
			}
			LoadPreviousStringHashes(path);
		}

		private static void LoadPreviousStringHashes(string path)
		{
			_Tracer.TraceInfo("Loading previous string hash file from {0}", path);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(path);
			try
			{
				foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
				{
					if (childNode.NodeType != XmlNodeType.Element || !childNode.Name.Equals(StringTableAssetName))
					{
						continue;
					}
					XmlElement xmlElement = (XmlElement)childNode;
					string value = xmlElement.Attributes[StringBinEnumAttribute].Value;
					StringHashBin value2 = null;
					if (_StringHashBins.TryGetValue(value, out value2))
					{
						if (Convert.ToUInt32(xmlElement.Attributes["Version"].Value) == HashProviderVersion)
						{
							value2.ReadStringHashTable(xmlElement);
							continue;
						}
						_Tracer.TraceInfo("Old string hash file is out of date.  If you encounter missing strings in the game (such as missing id's), please delete the session cache file in {0} and rebuild", Settings.Current.SessionCacheDirectory);
					}
				}
			}
			catch (Exception ex)
			{
				_Tracer.TraceWarning("Did not successfully initialize previous string hashes, exception message: {0}", ex.ToString());
			}
		}

		public static void FinalizeStringHashes()
		{
			if (!Directory.Exists(_outputDirectory))
			{
				Directory.CreateDirectory(_outputDirectory);
			}
			string outputFileName = Path.Combine(_outputDirectory, StringHashesFileDefault);
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.CloseOutput = true;
			xmlWriterSettings.Indent = true;
			XmlWriter xmlWriter = XmlWriter.Create(outputFileName, xmlWriterSettings);
			xmlWriter.WriteStartElement("AssetDeclaration", "uri:ea.com:eala:asset");
			if (Settings.Current.OutputAssetReport)
			{
				AssetReport.WriteAssetReportInclude(xmlWriter);
			}
			foreach (KeyValuePair<string, StringHashBin> stringHashBin in _StringHashBins)
			{
				xmlWriter.WriteStartElement(StringTableAssetName);
				xmlWriter.WriteAttributeString("Version", HashProviderVersion.ToString());
				stringHashBin.Value.WriteStringHashTable(xmlWriter);
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.Flush();
			xmlWriter.Close();
		}

		public static uint GetTypeHash(Type type)
		{
			return GetTypeHash((uint)type.FullName.Length, type);
		}

		public static uint GetTypeHash(uint combine, Type type)
		{
			return FastHash.GetHashCode(FastHash.GetHashCode(combine, type.FullName), type.Module.ModuleVersionId.ToByteArray());
		}

		public static uint GetCaseInsensitiveSymbolHash(string symbol)
		{
			return FastHash.GetHashCode(symbol.ToLower());
		}

		public static uint GetCaseSensitiveSymbolHash(string symbol)
		{
			return FastHash.GetHashCode(symbol);
		}

		public static uint GetTextHash(string text)
		{
			return FastHash.GetHashCode(text);
		}

		public static uint GetTextHash(uint hash, string text)
		{
			return FastHash.GetHashCode(hash, text);
		}

		public static uint GetDataHash(byte[] data)
		{
			return FastHash.GetHashCode(data);
		}

		public static uint GetDataHash(uint hash, byte[] data)
		{
			return FastHash.GetHashCode(hash, data);
		}

		public static uint GetXmlHash(uint hash, ref XmlNode node)
		{
			HashingWriter hashingWriter = new HashingWriter(hash);
			XmlWriter xmlWriter = XmlWriter.Create(hashingWriter);
			node.WriteTo(xmlWriter);
			xmlWriter.Flush();
			hashingWriter.Flush();
			return hashingWriter.GetFinalHash();
		}

		public static uint GetXmlHash(ref XmlNode node)
		{
			return GetXmlHash(0u, ref node);
		}
	}
}
