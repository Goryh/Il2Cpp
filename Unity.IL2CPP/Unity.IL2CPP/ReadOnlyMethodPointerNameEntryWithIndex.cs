using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class ReadOnlyMethodPointerNameEntryWithIndex : ReadOnlyMethodPointerNameEntry
{
	public readonly bool HasIndex;

	public ReadOnlyMethodPointerNameEntryWithIndex(MethodReference method, string name, bool hasIndex)
		: base(method, name)
	{
		HasIndex = hasIndex;
	}
}
