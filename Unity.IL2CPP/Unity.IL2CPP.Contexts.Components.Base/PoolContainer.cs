namespace Unity.IL2CPP.Contexts.Components.Base;

internal class PoolContainer<T>
{
	public readonly T[] Items;

	public PoolContainer(int count)
	{
		Items = new T[count];
	}
}
