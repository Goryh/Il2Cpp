using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;

public class CollectMethodPointerNames : PerAssemblyScheduledStepFunc<GlobalSecondaryCollectionContext, ReadOnlyMethodPointerNameTable>
{
	protected override string Name => "Collect Method Pointer Names Table";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override ReadOnlyMethodPointerNameTable ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item)
	{
		IMethodCollectorResults methodResults = context.Results.PrimaryWrite.Methods;
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		return new ReadOnlyMethodPointerNameTable((from m in item.AllMethods()
			orderby m.MetadataToken.RID
			select CreateEntry(readOnlyContext, methodResults, m)).ToArray().AsReadOnly());
	}

	private static ReadOnlyMethodPointerNameEntryWithIndex CreateEntry(ReadOnlyContext context, IMethodCollectorResults methodResults, MethodReference method)
	{
		bool hasIndex = methodResults.HasIndex(method);
		string name = (hasIndex ? MethodTables.MethodPointerNameFor(context, method) : "NULL");
		return new ReadOnlyMethodPointerNameEntryWithIndex(method, name, hasIndex);
	}
}
