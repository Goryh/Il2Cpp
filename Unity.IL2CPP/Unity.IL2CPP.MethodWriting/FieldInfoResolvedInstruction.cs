using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class FieldInfoResolvedInstruction : ResolvedInstruction
{
	public override ResolvedFieldInfo FieldInfo { get; }

	public FieldInfoResolvedInstruction(ResolvedFieldInfo fieldInfo, Instruction instruction, ResolvedInstruction next)
		: base(instruction, next)
	{
		FieldInfo = fieldInfo;
	}
}
