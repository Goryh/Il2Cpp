using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Results;

public interface IStatsResults
{
	int FilesWritten { get; }

	int TypesConverted { get; }

	int StringLiterals { get; }

	int Methods { get; }

	int GenericTypeMethods { get; }

	int GenericMethods { get; }

	int ShareableMethods { get; }

	int TailCallsEncountered { get; }

	int WindowsRuntimeBoxedTypes { get; }

	int WindowsRuntimeTypesWithNames { get; }

	int NativeToManagedInterfaceAdapters { get; }

	int ComCallableWrappers { get; }

	int ArrayComCallableWrappers { get; }

	int ImplementedComCallableWrapperMethods { get; }

	int StrippedComCallableWrapperMethods { get; }

	int ForwardedToBaseClassComCallableWrapperMethods { get; }

	long MetadataTotal { get; }

	int TotalNullChecks { get; }

	ReadOnlyDictionary<string, long> MetadataStreams { get; }

	Dictionary<MethodDefinition, int> NullCheckMethodsCount { get; }

	HashSet<MethodDefinition> NullChecksMethods { get; }

	HashSet<MethodDefinition> ArrayBoundsChecksMethods { get; }

	HashSet<MethodDefinition> DivideByZeroChecksMethods { get; }

	HashSet<MethodDefinition> MemoryBarrierMethods { get; }

	HashSet<MethodReference> SharableMethods { get; }
}
