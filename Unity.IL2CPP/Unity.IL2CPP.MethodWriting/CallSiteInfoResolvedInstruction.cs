using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class CallSiteInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedCallSiteInfo CallSiteInfo { get; }

	public CallSiteInfoResolvedInstruction(ResolvedCallSiteInfo callSiteInfo, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		CallSiteInfo = callSiteInfo;
	}
}
