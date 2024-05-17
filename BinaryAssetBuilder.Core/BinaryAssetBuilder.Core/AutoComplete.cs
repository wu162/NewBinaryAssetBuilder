using System;

namespace BinaryAssetBuilder.Core
{
	[Flags]
	public enum AutoComplete : uint
	{
		Default = 0u,
		Filesystem = 1u,
		UrlHistory = 2u,
		UrlMru = 4u,
		UrlAll = 6u,
		UseTab = 8u,
		FileSystemOnly = 0x10u,
		FileSystemDirectories = 0x20u,
		AutoSuggestForceOn = 0x10000000u,
		AutoSuggestForceOff = 0x20000000u,
		AutoAppendForceOn = 0x40000000u,
		AutoAppendForceOff = 0x80000000u
	}
}
