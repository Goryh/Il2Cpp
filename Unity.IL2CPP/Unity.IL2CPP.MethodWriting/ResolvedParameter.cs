namespace Unity.IL2CPP.MethodWriting;

public class ResolvedParameter
{
	public ResolvedTypeInfo ParameterType { get; }

	public int Index { get; }

	public bool IsThisArg => Index < 0;

	public string Name { get; }

	public string CppName { get; }

	public ResolvedParameter(int index, string name, string cppName, ResolvedTypeInfo parameterType)
	{
		Index = index;
		Name = name;
		CppName = cppName;
		ParameterType = parameterType;
	}

	public override string ToString()
	{
		return $"{Name} - {ParameterType}";
	}
}
