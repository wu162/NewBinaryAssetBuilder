using System;

namespace BinaryAssetBuilder.Core
{
	[Flags]
	public enum PathCharType
	{
		Invalid = 0,
		ValidLongFileNameChar = 1,
		ValidShortFileNameChar = 2,
		Wildcard = 4,
		Separator = 8
	}
}
