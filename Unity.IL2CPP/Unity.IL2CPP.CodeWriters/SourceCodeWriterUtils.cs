using NiceIO;

namespace Unity.IL2CPP.CodeWriters;

internal static class SourceCodeWriterUtils
{
	public static void WriteCommonIncludes(IDirectWriter writer, NPath fileName)
	{
		if (fileName.ExtensionWithDot.Equals(".cpp"))
		{
			writer.WriteLine("#include \"pch-cpp.hpp\"\n");
		}
		if (fileName.ExtensionWithDot.Equals(".c"))
		{
			writer.WriteLine("#include \"pch-c.h\"");
		}
	}
}
