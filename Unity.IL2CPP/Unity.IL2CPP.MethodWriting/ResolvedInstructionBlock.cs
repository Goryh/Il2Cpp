using System.Collections.ObjectModel;
using System.Diagnostics;
using Unity.IL2CPP.DataModel.Awesome.CFG;

namespace Unity.IL2CPP.MethodWriting;

[DebuggerDisplay("{Block}")]
public class ResolvedInstructionBlock
{
	public readonly InstructionBlock Block;

	public readonly ReadOnlyCollection<ResolvedInstruction> Instructions;

	public ResolvedInstructionBlock(InstructionBlock block, ReadOnlyCollection<ResolvedInstruction> instructions)
	{
		Block = block;
		Instructions = instructions;
	}
}
