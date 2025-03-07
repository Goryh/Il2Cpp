using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public class ScopeDebugInfo
{
	public InstructionOffset Start { get; }

	public InstructionOffset End { get; }

	public bool HasVariables => Variables.Count > 0;

	public ReadOnlyCollection<VariableDebugInfo> Variables { get; }

	public ScopeDebugInfo(ReadOnlyCollection<VariableDebugInfo> variables, InstructionOffset start, InstructionOffset end)
	{
		Variables = variables;
		Start = start;
		End = end;
	}
}
