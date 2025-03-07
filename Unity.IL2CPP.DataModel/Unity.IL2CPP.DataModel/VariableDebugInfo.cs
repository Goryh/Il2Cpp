namespace Unity.IL2CPP.DataModel;

public class VariableDebugInfo
{
	public string Name { get; }

	public int Index { get; }

	public VariableDebugInfo(string name, int index)
	{
		Name = name;
		Index = index;
	}
}
