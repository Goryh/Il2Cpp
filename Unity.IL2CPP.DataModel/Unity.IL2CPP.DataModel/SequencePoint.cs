using Mono.Cecil.Cil;

namespace Unity.IL2CPP.DataModel;

public class SequencePoint
{
	public int Offset { get; }

	public int StartLine { get; }

	public int StartColumn { get; }

	public int EndLine { get; }

	public int EndColumn { get; }

	public Document Document { get; }

	internal SequencePoint(Mono.Cecil.Cil.SequencePoint source, Document document)
	{
		Offset = source.Offset;
		StartLine = source.StartLine;
		StartColumn = source.StartColumn;
		EndColumn = source.EndColumn;
		EndLine = source.EndLine;
		Document = document;
	}

	public SequencePoint(Instruction instruction, Document document, int startLine = -1, int startColumn = -1, int endLine = -1, int endColumn = -1)
	{
		Offset = instruction.Offset;
		StartLine = startLine;
		StartColumn = startColumn;
		EndColumn = endColumn;
		EndLine = endLine;
		Document = document;
	}
}
