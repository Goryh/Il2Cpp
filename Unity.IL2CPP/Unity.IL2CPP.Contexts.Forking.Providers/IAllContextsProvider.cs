namespace Unity.IL2CPP.Contexts.Forking.Providers;

public interface IAllContextsProvider
{
	GlobalWriteContext GlobalWriteContext { get; }

	GlobalPrimaryCollectionContext GlobalPrimaryCollectionContext { get; }

	GlobalSecondaryCollectionContext GlobalSecondaryCollectionContext { get; }

	GlobalReadOnlyContext GlobalReadOnlyContext { get; }

	GlobalMinimalContext GlobalMinimalContext { get; }
}
