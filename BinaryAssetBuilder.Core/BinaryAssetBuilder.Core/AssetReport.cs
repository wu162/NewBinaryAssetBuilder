using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BinaryAssetBuilder.Core
{
	public static class AssetReport
	{
		public static string FileName = "AssetReport.xml";

		public static string FileNameFullyQualified = null;

		public static readonly string TypeName = "AssetReportTable";

		private static XmlWriter AssetRecordWriter = null;

		private static IDictionary<string, bool> RecordedAssets = null;

		public static void RecordAsset(InstanceDeclaration instance, BinaryAsset asset)
		{
			if (instance.Handle == null)
			{
				throw new BinaryAssetBuilderException(ErrorCode.InternalError, "Asset report failure");
			}
			if (!(instance.Handle.TypeName == TypeName) && !(instance.Handle.TypeName == "StringHashTable"))
			{
				if (AssetRecordWriter == null)
				{
					InitAssetRecordWriter();
				}
				RecordAsset(instance.Handle.InstanceName, instance.Handle.TypeName, asset.InstanceFileSize, instance.ValidatedReferencedInstances);
			}
		}

		private static void RecordAsset(string id, string type, int size, IList<InstanceHandle> references)
		{
			string key = MakeAssetReportId(type, id);
			if (RecordedAssets.ContainsKey(key))
			{
				return;
			}
			RecordedAssets.Add(key, value: true);
			AssetRecordWriter.WriteStartElement("AssetReport", "uri:ea.com:eala:asset");
			AssetRecordWriter.WriteAttributeString("Id", id.ToLower());
			AssetRecordWriter.WriteAttributeString("Type", type);
			AssetRecordWriter.WriteAttributeString("AssetSize", Convert.ToString(size));
			if (references != null)
			{
				foreach (InstanceHandle reference in references)
				{
					AssetRecordWriter.WriteStartElement("Reference");
					AssetRecordWriter.WriteAttributeString("Id", reference.InstanceName.ToLower());
					AssetRecordWriter.WriteAttributeString("Type", reference.TypeName);
					AssetRecordWriter.WriteEndElement();
				}
			}
			AssetRecordWriter.WriteEndElement();
		}

		private static void InitAssetRecordWriter()
		{
			if (!Directory.Exists(Settings.Current.SessionCacheDirectory))
			{
				Directory.CreateDirectory(Settings.Current.SessionCacheDirectory);
			}
			FileNameFullyQualified = Path.Combine(Settings.Current.SessionCacheDirectory, FileName);
			RecordedAssets = new SortedDictionary<string, bool>();
			BufferedStream output = new BufferedStream(new FileStream(FileNameFullyQualified, FileMode.Create));
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.CloseOutput = true;
			xmlWriterSettings.Indent = true;
			xmlWriterSettings.IndentChars = "\t";
			AssetRecordWriter = XmlWriter.Create(output, xmlWriterSettings);
			AssetRecordWriter.WriteStartElement("AssetDeclaration", "uri:ea.com:eala:asset");
			AssetRecordWriter.WriteStartElement(TypeName);
			AssetRecordWriter.WriteAttributeString("id", "TheAssetReportTable");
		}

		public static string MakeAssetReportId(string typeId, string instanceId)
		{
			return (typeId + "." + instanceId + ".AssetReport").ToLower();
		}

		public static void Close()
		{
			if (AssetRecordWriter != null)
			{
				AssetRecordWriter.WriteEndDocument();
				AssetRecordWriter.Flush();
				AssetRecordWriter.Close();
				AssetRecordWriter = null;
				FileNameFullyQualified = null;
				RecordedAssets = null;
			}
		}

		public static void WriteAssetReportInclude(XmlWriter writer)
		{
			writer.WriteStartElement("Includes");
			writer.WriteStartElement("Include");
			writer.WriteAttributeString("type", "all");
			writer.WriteAttributeString("source", FileName);
			writer.WriteEndElement();
			writer.WriteEndElement();
		}
	}
}
