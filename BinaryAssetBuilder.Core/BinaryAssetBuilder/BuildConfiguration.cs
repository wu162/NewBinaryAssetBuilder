using System;
using System.Xml.Serialization;

namespace BinaryAssetBuilder
{
	[XmlType("buildConfiguration")]
	public class BuildConfiguration : ICloneable
	{
		private string _Name;

		private string _Postfix;

		private string _StreamPostfix;

		private bool _AppendPostfixToStream = true;

		private string _ArtPaths;

		private string _AudioPaths;

		private string _DataPaths;

		[XmlAttribute("name")]
		public string Name
		{
			get
			{
				return _Name;
			}
			set
			{
				_Name = value;
			}
		}

		[XmlAttribute("postfix")]
		public string Postfix
		{
			get
			{
				return _Postfix;
			}
			set
			{
				_Postfix = value;
			}
		}

		[XmlAttribute("streamPostfix")]
		public string StreamPostfix
		{
			get
			{
				return _StreamPostfix;
			}
			set
			{
				_StreamPostfix = value;
			}
		}

		[XmlAttribute("appendPostfixToStream")]
		public bool AppendPostfixToStream
		{
			get
			{
				return _AppendPostfixToStream;
			}
			set
			{
				_AppendPostfixToStream = value;
			}
		}

		[XmlAttribute("artPaths")]
		public string ArtPaths
		{
			get
			{
				return _ArtPaths;
			}
			set
			{
				_ArtPaths = value;
			}
		}

		[XmlAttribute("audioPaths")]
		public string AudioPaths
		{
			get
			{
				return _AudioPaths;
			}
			set
			{
				_AudioPaths = value;
			}
		}

		[XmlAttribute("dataPaths")]
		public string DataPaths
		{
			get
			{
				return _DataPaths;
			}
			set
			{
				_DataPaths = value;
			}
		}

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
