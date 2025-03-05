using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.CodeWriters;

public interface IDirectWriter
{
	void WriteLine();

	void WriteLine(string text);

	void Write(string text);

	void Write(char text);

	void WriteLine([InterpolatedStringHandlerArgument("")] ref IDirectWriterInterpolatedStringHandler handler);

	void Write([InterpolatedStringHandlerArgument("")] ref IDirectWriterInterpolatedStringHandler handler);
}
