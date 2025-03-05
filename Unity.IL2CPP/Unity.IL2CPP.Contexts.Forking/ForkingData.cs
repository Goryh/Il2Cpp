namespace Unity.IL2CPP.Contexts.Forking;

public readonly struct ForkingData
{
	public readonly int Index;

	public readonly int Count;

	public ForkingData(int index, int count)
	{
		Index = index;
		Count = count;
	}
}
