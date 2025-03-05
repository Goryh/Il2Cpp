using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class ParameterInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedParameter ParameterInfo { get; }

	public ParameterInfoResolvedInstruction(ResolvedParameter parameter, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		ParameterInfo = parameter;
	}
}
