using Microsoft.IO;

namespace Ulearn.Common;

public static class StaticRecyclableMemoryStreamManager
{
	public static readonly RecyclableMemoryStreamManager Manager = new();
}