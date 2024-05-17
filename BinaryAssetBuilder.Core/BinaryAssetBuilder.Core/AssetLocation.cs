using System;

namespace BinaryAssetBuilder.Core
{
	[Flags]
	public enum AssetLocation
	{
		None = 0,
		Memory = 1,
		Output = 2,
		Local = 4,
		Cache = 8,
		BasePatchStream = 0x10,
		All = 0xFFFF
	}
}
