using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.TableWriters;

public class WriteWindowsRuntimeFactoryTable : GeneratedCodeTableWriterBaseSimple<WindowsRuntimeFactoryData>
{
	protected override string TableName => "Il2CppWindowsRuntimeFactoriesTable";

	protected override string CodeTableType => "Il2CppWindowsRuntimeFactoryTableEntry";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, DictionaryExtensions.ItemsSortedByKey(scheduler.SchedulingContext.Results.PrimaryCollection.WindowsRuntimeData).SelectMany((KeyValuePair<AssemblyDefinition, CollectedWindowsRuntimeData> pair) => pair.Value.RuntimeFactories).ToList()
			.AsReadOnly());
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_WindowsRuntimeFactories");
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<WindowsRuntimeFactoryData> allItems)
	{
		INamingService naming = context.Global.Services.Naming;
		foreach (WindowsRuntimeFactoryData factory in allItems)
		{
			writer.WriteExternForIl2CppType(factory.RuntimeType);
			writer.WriteStatement($"Il2CppIActivationFactory* {naming.ForCreateWindowsRuntimeFactoryFunction(factory.TypeDefinition)}()");
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, WindowsRuntimeFactoryData item)
	{
		INamingService naming = context.Global.Services.Naming;
		writer.Write($"{{ &{naming.ForIl2CppType(context, item.RuntimeType)}, reinterpret_cast<Il2CppMethodPointer>({naming.ForCreateWindowsRuntimeFactoryFunction(item.TypeDefinition)}) }}");
	}
}
