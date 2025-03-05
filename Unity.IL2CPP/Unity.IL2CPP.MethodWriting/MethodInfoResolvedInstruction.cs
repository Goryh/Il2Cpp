using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class MethodInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedMethodInfo MethodInfo { get; }

	public MethodInfoResolvedInstruction(ResolvedMethodInfo methodInfo, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		MethodInfo = methodInfo;
	}
}
