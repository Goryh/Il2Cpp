namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class VariableDefinitionBuilder
{
	private readonly TypeReference _variableType;

	internal VariableDefinitionBuilder(TypeReference variableType)
	{
		_variableType = variableType;
	}

	internal VariableDefinition Complete(int index)
	{
		return new VariableDefinition(_variableType, isPinned: false, index, string.Empty);
	}
}
