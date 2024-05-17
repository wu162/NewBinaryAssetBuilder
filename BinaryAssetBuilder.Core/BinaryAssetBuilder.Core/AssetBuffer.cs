namespace BinaryAssetBuilder.Core
{
	public class AssetBuffer
	{
		private byte[] _InstanceData;

		private byte[] _RelocationData;

		private byte[] _ImportsData;

		public byte[] InstanceData
		{
			get
			{
				return _InstanceData;
			}
			set
			{
				_InstanceData = value;
			}
		}

		public byte[] RelocationData
		{
			get
			{
				return _RelocationData;
			}
			set
			{
				_RelocationData = value;
			}
		}

		public byte[] ImportsData
		{
			get
			{
				return _ImportsData;
			}
			set
			{
				_ImportsData = value;
			}
		}
	}
}
