using System;

namespace BinaryAssetBuilder.Core
{
	[Flags]
	public enum TraceKind
	{
		None = 0,
		Exception = 1,
		Assert = 2,
		Error = 4,
		Warning = 8,
		Message = 0x10,
		Info = 0x20,
		Note = 0x100,
		Method = 0x200,
		Scope = 0x400,
		Constructor = 0x800,
		Property = 0x1000,
		Data = 0x2000,
		All = 0xFFFF
	}
}
