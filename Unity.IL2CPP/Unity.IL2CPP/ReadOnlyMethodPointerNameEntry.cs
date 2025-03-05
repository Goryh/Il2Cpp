using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class ReadOnlyMethodPointerNameEntry
{
	public readonly MethodReference Method;

	public readonly string Name;

	public ReadOnlyMethodPointerNameEntry(MethodReference method, string name)
	{
		Method = method;
		Name = name;
	}
}
