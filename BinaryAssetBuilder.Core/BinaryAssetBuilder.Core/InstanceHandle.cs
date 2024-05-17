using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class InstanceHandle : IXmlSerializable, IComparable<InstanceHandle>
	{
		private uint _InstanceId;

		private uint _InstanceHash;

		private string _InstanceName = string.Empty;

		private uint _TypeId;

		private uint _TypeHash;

		private string _TypeName = string.Empty;

		[NonSerialized]
		private string _Name;

		[NonSerialized]
		private string _FullName;

		[NonSerialized]
		private string _FileBase;

		public uint InstanceId => _InstanceId;

		public uint InstanceHash
		{
			get
			{
				return _InstanceHash;
			}
			set
			{
				_InstanceHash = value;
				_Name = null;
				_FullName = null;
				_FileBase = null;
			}
		}

		public string InstanceName => _InstanceName;

		public uint TypeId => _TypeId;

		public uint TypeHash
		{
			get
			{
				return _TypeHash;
			}
			set
			{
				_TypeHash = value;
				_Name = null;
				_FullName = null;
				_FileBase = null;
			}
		}

		public string TypeName
		{
			get
			{
				return _TypeName;
			}
			set
			{
				_TypeName = value;
				_TypeId = GetTypeId(_TypeName);
			}
		}

		public string Name
		{
			get
			{
				if (string.IsNullOrEmpty(_Name))
				{
					_Name = $"{_TypeName}:{_InstanceName}";
				}
				return _Name;
			}
		}

		public string FullName
		{
			get
			{
				if (string.IsNullOrEmpty(_FullName))
				{
					_FullName = $"{_TypeName}:{_InstanceName} [{_TypeId:x8}:{_InstanceId:x8}]";
				}
				return _FullName;
			}
		}

		public string FileBase
		{
			get
			{
				if (string.IsNullOrEmpty(_FileBase))
				{
					_FileBase = $"{_TypeId:x8}.{_TypeHash:x8}.{_InstanceId:x8}.{_InstanceHash:x8}";
				}
				return _FileBase;
			}
		}

		public InstanceHandle()
		{
		}

		public int CompareTo(InstanceHandle other)
		{
			if (InstanceId == other.InstanceId && TypeId == other.TypeId)
			{
				return 0;
			}
			if (TypeId < other.TypeId)
			{
				return -1;
			}
			if (TypeId > other.TypeId)
			{
				return 1;
			}
			if (InstanceId < other.InstanceId)
			{
				return -1;
			}
			if (InstanceId > other.InstanceId)
			{
				return 1;
			}
			return 0;
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("ih");
			writer.WriteAttributeString("d", $"{_TypeId};{_TypeHash};{_InstanceId};{_InstanceHash};{_TypeName};{_InstanceName}");
			writer.WriteEndElement();
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToAttribute("d");
			string[] array = reader.Value.Split(';');
			_TypeId = Convert.ToUInt32(array[0]);
			_TypeHash = Convert.ToUInt32(array[1]);
			_InstanceId = Convert.ToUInt32(array[2]);
			_InstanceHash = Convert.ToUInt32(array[3]);
			_TypeName = array[4];
			_InstanceName = array[5];
			reader.MoveToElement();
			reader.Read();
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		[Conditional("DEBUG")]
		public void BreakOn(InstanceHandle handle)
		{
			if (this == handle)
			{
				Debugger.Break();
			}
		}

		public static uint GetTypeId(string typeName)
		{
			return HashProvider.GetCaseSensitiveSymbolHash(typeName);
		}

		public static uint GetInstanceId(string instanceName)
		{
			return HashProvider.GetCaseInsensitiveSymbolHash(instanceName);
		}

		public InstanceHandle(InstanceHandle src)
		{
			_InstanceId = src._InstanceId;
			_TypeId = src._TypeId;
			_InstanceHash = src._InstanceHash;
			_TypeHash = src._TypeHash;
			_InstanceName = src._InstanceName;
			_TypeName = src._TypeName;
		}

		public InstanceHandle(string typeName, string instanceName)
		{
			_InstanceId = GetInstanceId(instanceName);
			_TypeId = GetTypeId(typeName);
			_InstanceName = instanceName;
			_TypeName = typeName;
		}

		public InstanceHandle(string instanceName)
		{
			string[] array = instanceName.Split(':');
			if (array.Length > 2)
			{
				throw new ArgumentException("Invalid instance name.");
			}
			_InstanceName = array[array.Length - 1];
			_InstanceId = GetInstanceId(_InstanceName);
			if (array.Length == 2)
			{
				_TypeName = array[0];
				_TypeId = GetTypeId(_TypeName);
			}
		}

		public InstanceHandle(uint typeId, string instanceName)
			: this(instanceName)
		{
			_TypeId = typeId;
		}

		public InstanceHandle(uint typeId, uint instanceId)
		{
			_TypeId = typeId;
			_InstanceId = instanceId;
		}

		public override string ToString()
		{
			return FullName;
		}

		public override int GetHashCode()
		{
			return (int)(InstanceId ^ TypeId);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			InstanceHandle instanceHandle = (InstanceHandle)obj;
			if (_InstanceId == instanceHandle._InstanceId)
			{
				return _TypeId == instanceHandle._TypeId;
			}
			return false;
		}

		public static bool operator ==(InstanceHandle a, InstanceHandle b)
		{
			if ((object)a == null || (object)b == null)
			{
				if ((object)a == null)
				{
					return (object)b == null;
				}
				return false;
			}
			if (a._InstanceId == b._InstanceId)
			{
				return a._TypeId == b._TypeId;
			}
			return false;
		}

		public static bool operator !=(InstanceHandle a, InstanceHandle b)
		{
			if ((object)a == null || (object)b == null)
			{
				if ((object)a == null)
				{
					return (object)b != null;
				}
				return true;
			}
			if (a._InstanceId == b._InstanceId)
			{
				return a._TypeId != b._TypeId;
			}
			return true;
		}
	}
}
