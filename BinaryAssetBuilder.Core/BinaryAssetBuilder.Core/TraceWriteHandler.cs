using System.Diagnostics;

namespace BinaryAssetBuilder.Core
{
	public delegate void TraceWriteHandler(string source, TraceEventType eventType, string message);
}
