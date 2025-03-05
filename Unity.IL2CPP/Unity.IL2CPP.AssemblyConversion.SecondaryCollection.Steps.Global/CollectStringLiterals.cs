using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;

public class CollectStringLiterals : SimpleScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyStringLiteralTable>
{
	protected override string Name => "Collect String Literals";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyStringLiteralTable CreateEmptyResult()
	{
		return new ReadOnlyStringLiteralTable(new List<KeyValuePair<string, uint>>().AsReadOnly());
	}

	protected override ReadOnlyStringLiteralTable Worker(GlobalSecondaryCollectionContext context)
	{
		Dictionary<StringMetadataToken, uint> table = new Dictionary<StringMetadataToken, uint>(StringMetadataTokenComparer.Default);
		foreach (StringMetadataToken stringMetadataToken in context.Results.PrimaryWrite.MetadataUsage.GetStringLiterals().ToSortedCollection())
		{
			table.Add(stringMetadataToken, (uint)table.Count);
		}
		return new ReadOnlyStringLiteralTable((from pair in table.ItemsSortedByValue()
			select new KeyValuePair<string, uint>(pair.Key.Literal, pair.Value)).ToArray().AsReadOnly());
	}
}
