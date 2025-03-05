using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class TypeInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedTypeInfo TypeInfo { get; }

	public TypeInfoResolvedInstruction(ResolvedTypeInfo methodInfo, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		TypeInfo = methodInfo;
	}
}
