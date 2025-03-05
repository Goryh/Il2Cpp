using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public class ResolvedVariable
{
	public readonly VariableDefinition VariableReference;

	public readonly ResolvedTypeInfo VariableType;

	public int Index => VariableReference.Index;

	public ResolvedVariable(VariableDefinition variableReference, ResolvedTypeInfo variableType)
	{
		VariableReference = variableReference;
		VariableType = variableType;
	}

	public override string ToString()
	{
		return VariableReference.ToString();
	}
}
