using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class CollectFieldReferences : SimpleScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyFieldReferenceTable>
{
	protected override string Name => "Collect Field References";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyFieldReferenceTable CreateEmptyResult()
	{
		return new ReadOnlyFieldReferenceTable(new List<KeyValuePair<Il2CppRuntimeFieldReference, uint>>().AsReadOnly());
	}

	protected override ReadOnlyFieldReferenceTable Worker(GlobalSecondaryCollectionContext context)
	{
		ReadOnlyCollection<Il2CppRuntimeFieldReference> readOnlyCollection = context.Results.PrimaryWrite.MetadataUsage.GetFieldInfos().ToSortedCollection();
		List<KeyValuePair<Il2CppRuntimeFieldReference, uint>> fieldRefTable = new List<KeyValuePair<Il2CppRuntimeFieldReference, uint>>(readOnlyCollection.Count);
		foreach (Il2CppRuntimeFieldReference field in readOnlyCollection)
		{
			fieldRefTable.Add(new KeyValuePair<Il2CppRuntimeFieldReference, uint>(field, (uint)fieldRefTable.Count));
		}
		return new ReadOnlyFieldReferenceTable(fieldRefTable.AsReadOnly());
	}
}
