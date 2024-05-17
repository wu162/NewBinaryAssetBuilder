using System;

namespace BinaryAssetBuilder.Core
{
	[Flags]
	public enum AssetLocationOption
	{
		None = 0,
		ForceUpdate = 1,
		ReturnAll = 2
	}
}
