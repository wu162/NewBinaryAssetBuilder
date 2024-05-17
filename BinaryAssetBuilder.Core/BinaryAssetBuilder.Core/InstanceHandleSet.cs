using System;

namespace BinaryAssetBuilder.Core
{
	[Serializable]
	public class InstanceHandleSet : KeyedCollection2<int, InstanceHandle>
	{
		protected override int GetKeyForItem(InstanceHandle item)
		{
			return item.GetHashCode();
		}

		public bool TryAdd(InstanceHandle handle)
		{
			if (Contains(handle))
			{
				return false;
			}
			Add(handle);
			return true;
		}
	}
}
