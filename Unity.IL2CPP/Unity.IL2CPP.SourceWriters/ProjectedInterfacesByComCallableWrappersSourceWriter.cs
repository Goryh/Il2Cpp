using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.SourceWriters;

public class ProjectedInterfacesByComCallableWrappersSourceWriter : SourceWriterBase<TypeReference, TypeReference>
{
	public override string Name => "WriteProjectedComCallableWrapperMethods";

	public override bool RequiresSortingBeforeChunking => true;

	public override FileCategory FileCategory => FileCategory.Metadata;

	private ProjectedInterfacesByComCallableWrappersSourceWriter()
	{
	}

	private static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeReference> items)
	{
		ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new ProjectedInterfacesByComCallableWrappersSourceWriter()).Write(context.Global, items);
	}

	public static void EnqueueWrites(SourceWritingContext context)
	{
		ReadOnlyCollection<IIl2CppRuntimeType> typesWithComCallableWrappers = context.Global.PrimaryCollectionResults.CCWMarshalingFunctions;
		HashSet<TypeReference> implementedProjectedInterfaces;
		using (context.Global.Services.TinyProfiler.Section("Collect implemented projected interfaces by COM Callable Wrappers"))
		{
			implementedProjectedInterfaces = CollectImplementedProjectedInterfacesByComCallableWrappersOf(context, typesWithComCallableWrappers);
		}
		if (implementedProjectedInterfaces.Count != 0)
		{
			EnqueueWrites(context, "Il2CppPCCWMethods", implementedProjectedInterfaces);
		}
	}

	private static HashSet<TypeReference> CollectImplementedProjectedInterfacesByComCallableWrappersOf(ReadOnlyContext context, ReadOnlyCollection<IIl2CppRuntimeType> typesWithComCallableWrappers)
	{
		HashSet<TypeReference> implementedProjectedInterfaces = new HashSet<TypeReference>();
		foreach (IIl2CppRuntimeType typesWithComCallableWrapper in typesWithComCallableWrappers)
		{
			foreach (TypeReference interfaceType in typesWithComCallableWrapper.Type.GetInterfacesImplementedByComCallableWrapper(context))
			{
				if (context.Global.Services.WindowsRuntime.ProjectToCLR(interfaceType) != interfaceType)
				{
					implementedProjectedInterfaces.Add(interfaceType);
				}
			}
		}
		return implementedProjectedInterfaces;
	}

	public override IComparer<TypeReference> CreateComparer()
	{
		return new TypeOrderingComparer();
	}

	public override IEnumerable<TypeReference> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TypeReference> items)
	{
		return items;
	}

	public override ReadOnlyCollection<ReadOnlyCollection<TypeReference>> Chunk(ReadOnlyCollection<TypeReference> items)
	{
		return new List<ReadOnlyCollection<TypeReference>> { items }.AsReadOnly();
	}

	protected override string ProfilerSectionDetailsForItem(TypeReference item)
	{
		return item.Name;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference item, NPath filePath)
	{
		ProjectedComCallableWrapperMethodWriterDriver.WriteFor(context, writer, item);
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
