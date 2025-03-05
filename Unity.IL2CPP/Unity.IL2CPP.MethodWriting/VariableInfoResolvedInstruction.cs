using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class VariableInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedVariable VariableInfo { get; }

	public VariableInfoResolvedInstruction(ResolvedVariable variable, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		VariableInfo = variable;
	}
}
