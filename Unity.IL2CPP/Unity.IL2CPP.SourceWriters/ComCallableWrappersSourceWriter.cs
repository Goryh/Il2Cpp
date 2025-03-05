using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Com;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.SourceWriters.Utils;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.SourceWriters;

public class ComCallableWrappersSourceWriter : SourceWriterBase<TypeReference, TypeReference>
{
	public override string Name => "WriteComCallableWrapperMethods";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.Metadata;

	private ComCallableWrappersSourceWriter()
	{
	}

	private static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeReference> items)
	{
		ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new ComCallableWrappersSourceWriter()).Write(context.Global, items);
	}

	public static void EnqueueWrites(SourceWritingContext context)
	{
		ReadOnlyCollection<IIl2CppRuntimeType> typesWithComCallableWrappers = context.Global.PrimaryCollectionResults.CCWMarshalingFunctions;
		if (typesWithComCallableWrappers.Count != 0)
		{
			EnqueueWrites(context, "Il2CppCCWs", typesWithComCallableWrappers.Select((IIl2CppRuntimeType t) => t.Type).ToList());
		}
	}

	public override IEnumerable<TypeReference> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TypeReference> items)
	{
		return items;
	}

	public override ReadOnlyCollection<ReadOnlyCollection<TypeReference>> Chunk(ReadOnlyCollection<TypeReference> items)
	{
		return items.ChunkByApproximateGeneratedCodeSize();
	}

	protected override string ProfilerSectionDetailsForItem(TypeReference item)
	{
		return item.Name;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference item, NPath filePath)
	{
		ICCWWriter ccwWriter = GetCCWWriterForType(context, item);
		ccwWriter.Write(writer);
		writer.WriteLine();
		string createCCWMethodName = context.Global.Services.Naming.ForCreateComCallableWrapperFunction(item);
		string createCCWMethodSignature = "IL2CPP_EXTERN_C Il2CppIUnknown* " + createCCWMethodName + "(RuntimeObject* obj)";
		writer.WriteMethodWithMetadataInitialization(createCCWMethodSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			ccwWriter.WriteCreateComCallableWrapperFunctionBody(bodyWriter, metadataAccess);
		}, createCCWMethodName, null);
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

	private static ICCWWriter GetCCWWriterForType(SourceWritingContext context, TypeReference type)
	{
		if (type.IsArray)
		{
			return new CCWWriter(context, type);
		}
		TypeDefinition typeDef = type.Resolve();
		if (typeDef.IsDelegate)
		{
			return new DelegateCCWWriter(context, type);
		}
		TypeDefinition windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDef);
		if (windowsRuntimeType != typeDef && !windowsRuntimeType.IsInterface && !windowsRuntimeType.IsValueType)
		{
			return new ProjectedClassCCWWriter(type);
		}
		return new CCWWriter(context, type);
	}
}
