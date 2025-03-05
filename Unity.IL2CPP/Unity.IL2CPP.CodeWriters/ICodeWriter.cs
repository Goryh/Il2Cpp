using System.Runtime.CompilerServices;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public interface ICodeWriter : IDirectWriter
{
	ReadOnlyContext Context { get; }

	int IndentationLevel { get; }

	bool Empty { get; }

	new void WriteLine();

	new void WriteLine(string block);

	void WriteLine([InterpolatedStringHandlerArgument("")] ref CodeWriterInterpolatedStringHandler handler);

	void WriteCommentedLine(string block);

	void WriteComment(string block);

	void WriteStatement(string block);

	void WriteStatement([InterpolatedStringHandlerArgument("")] ref CodeWriterInterpolatedStringHandler handler);

	new void Write(string block);

	new void Write(char block);

	void Write([InterpolatedStringHandlerArgument("")] ref CodeWriterInterpolatedStringHandler handler);

	void WriteUnindented(string block);

	void WriteUnindented([InterpolatedStringHandlerArgument("")] ref CodeWriterUnindentedInterpolatedStringHandler handler);

	void Indent(int count = 1);

	void Dedent(int count = 1);

	void BeginBlock();

	void BeginBlock(string comment);

	void EndBlock(bool semicolon = false);

	void EndBlock(string comment, bool semicolon = false);

	void WriteAssignStatement([InterpolatedStringHandlerArgument("")] ref CodeWriterAssignInterpolatedStringHandler left, [InterpolatedStringHandlerArgument("")] ref CodeWriterInterpolatedStringHandler right);

	void WriteAssignStatement([InterpolatedStringHandlerArgument("")] ref CodeWriterAssignInterpolatedStringHandler left, string right);

	void WriteAssignStatement(string left, [InterpolatedStringHandlerArgument(new string[] { "", "left" })] ref CodeWriterRightAssignInterpolatedStringHandler right);

	void Flush();

	void Write(ICodeBuilder other);
}
