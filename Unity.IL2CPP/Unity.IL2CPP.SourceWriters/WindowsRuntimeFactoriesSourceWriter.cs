using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.SourceWriters.Utils;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.SourceWriters;

public class WindowsRuntimeFactoriesSourceWriter : SourceWriterBase<TypeDefinition, TypeDefinition>
{
	public override string Name => "Write Windows Runtime Factory";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.Metadata;

	private WindowsRuntimeFactoriesSourceWriter()
	{
	}

	private static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeDefinition> items)
	{
		ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new WindowsRuntimeFactoriesSourceWriter()).Write(context.Global, items);
	}

	public static void EnqueueWrites(SourceWritingContext context)
	{
		List<WindowsRuntimeFactoryData> typesWithFactories = DictionaryExtensions.ItemsSortedByKey(context.Global.PrimaryCollectionResults.WindowsRuntimeData).SelectMany((KeyValuePair<AssemblyDefinition, CollectedWindowsRuntimeData> pair) => pair.Value.RuntimeFactories).ToList();
		if (typesWithFactories.Count != 0)
		{
			EnqueueWrites(context, "Il2CppWindowsRuntimeFactories", typesWithFactories.Select((WindowsRuntimeFactoryData t) => t.TypeDefinition).ToList());
		}
	}

	public override IEnumerable<TypeDefinition> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TypeDefinition> items)
	{
		return items;
	}

	public override ReadOnlyCollection<ReadOnlyCollection<TypeDefinition>> Chunk(ReadOnlyCollection<TypeDefinition> items)
	{
		return items.ChunkByApproximateGeneratedCodeSize();
	}

	protected override string ProfilerSectionDetailsForItem(TypeDefinition item)
	{
		return item.Name;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeDefinition item, NPath filePath)
	{
		WindowsRuntimeFactoryWriter factoryWriter = new WindowsRuntimeFactoryWriter(context, item);
		factoryWriter.Write(writer);
		string createFactoryMethodName = context.Global.Services.Naming.ForCreateWindowsRuntimeFactoryFunction(item);
		string signature = "Il2CppIActivationFactory* " + createFactoryMethodName + "()";
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			factoryWriter.WriteCreateComCallableWrapperFunctionBody(bodyWriter, metadataAccess);
		}, createFactoryMethodName, null);
	}

	protected override void WriteHeader(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteFooter(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteEnd(IGeneratedMethodCodeStream writer)
	{
		MethodWriter.WriteInlineMethodDefinitions(writer.Context, writer.FileName.FileNameWithoutExtension, writer);
	}
}
