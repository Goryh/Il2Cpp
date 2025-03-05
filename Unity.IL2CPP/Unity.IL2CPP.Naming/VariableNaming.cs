using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Naming;

public static class VariableNaming
{
	public static string ForVariableName(this INamingService naming, VariableDefinition variable)
	{
		return "V_" + variable.Index;
	}
}
