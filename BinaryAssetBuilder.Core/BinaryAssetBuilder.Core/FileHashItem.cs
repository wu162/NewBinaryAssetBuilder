using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using EALAHash;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class FileHashItem : IXmlSerializable
	{
		public delegate void KeepAliveDelegate();

		public static KeepAliveDelegate _KeepAliveDelegate;

		private string _Path;

		private string _BuildConfiguration;

		private TargetPlatform _TargetPlatform;

		[NonSerialized]
		private FileState _State;

		private uint _Hash;

		private DateTime _LastDate = DateTime.MinValue;

		[NonSerialized]
		public DateTime _CurrentDate = DateTime.MaxValue;

		[NonSerialized]
		private bool _Exists;

		public string Path => _Path;

		public string BuildConfiguration => _BuildConfiguration;

		public TargetPlatform TargetPlatform => _TargetPlatform;

		public uint Hash
		{
			get
			{
				if (_State < FileState.HashValid)
				{
					if (IsDirty)
					{
						if (Exists)
						{
							UpdateHash();
						}
						else
						{
							_Hash = 0u;
						}
					}
					_State |= FileState.HashValid;
				}
				return _Hash;
			}
		}

		public bool IsDirty
		{
			get
			{
				if (_State < FileState.DateValid)
				{
					if (Exists)
					{
						_CurrentDate = File.GetLastWriteTime(_Path);
					}
					_State = FileState.DateValid;
				}
				return _LastDate != _CurrentDate;
			}
		}

		public bool Exists
		{
			get
			{
				if (_State < FileState.ExistsValid)
				{
					_Exists = File.Exists(_Path);
					_State = FileState.ExistsValid;
				}
				return _Exists;
			}
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("hi");
			writer.WriteAttributeString("d", $"{_Path};{_Hash};{_LastDate.ToBinary()};{_BuildConfiguration};{(int)_TargetPlatform}");
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToAttribute("d");
			string[] array = reader.Value.Split(';');
			_Path = array[0];
			_Hash = Convert.ToUInt32(array[1]);
			_LastDate = DateTime.FromBinary(Convert.ToInt64(array[2]));
			if (array.Length > 3)
			{
				if (array.Length > 4)
				{
					_TargetPlatform = (TargetPlatform)int.Parse(array[4]);
				}
				else
				{
					_TargetPlatform = TargetPlatform.Win32;
				}
				_BuildConfiguration = array[3];
			}
			else
			{
				_BuildConfiguration = "";
				_TargetPlatform = TargetPlatform.Win32;
			}
			_CurrentDate = DateTime.MaxValue;
			reader.Read();
		}

		public FileHashItem()
		{
		}

		public FileHashItem(string path, string configuration, TargetPlatform platform)
		{
			_Path = path;
			_BuildConfiguration = configuration;
			_TargetPlatform = platform;
		}

		public void Reset()
		{
			_State = FileState.AllInvalid;
			_CurrentDate = DateTime.MaxValue;
		}

		private void UpdateHash()
		{
			AsynchronousFileReader asynchronousFileReader = new AsynchronousFileReader(_Path);
			uint hash = asynchronousFileReader.FileSize;
			while (asynchronousFileReader.BeginRead())
			{
				hash = FastHash.GetHashCode(hash, asynchronousFileReader.CurrentChunk.Data, asynchronousFileReader.CurrentChunk.BytesRead);
				asynchronousFileReader.EndRead();
			}
			_Hash = hash;
			_LastDate = _CurrentDate;
		}
	}
}
