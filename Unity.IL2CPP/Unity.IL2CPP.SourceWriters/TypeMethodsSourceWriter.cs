using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.SourceWriters.Utils;

namespace Unity.IL2CPP.SourceWriters;

public class TypeMethodsSourceWriter : SourceWriterBase<TypeDefinition, TypeWritingInformation>
{
	public override string Name => "WriteMethods";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.PerAssembly;

	private TypeMethodsSourceWriter()
	{
	}

	public static void EnqueueWrites(SourceWritingContext context, AssemblyDefinition assembly, ICollection<TypeDefinition> items)
	{
		ScheduledStreamFactory.Create(context, context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, ".cpp"), new TypeMethodsSourceWriter()).Write(context.Global, items);
	}

	public static void EnqueueWrites(AssemblyWriteContext context)
	{
		AssemblyDefinition assemblyDefinition = context.AssemblyDefinition;
		EnqueueWrites(context.SourceWritingContext, assemblyDefinition, assemblyDefinition.GetAllTypes());
	}

	public override IEnumerable<TypeWritingInformation> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TypeDefinition> items)
	{
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		foreach (TypeDefinition type in items)
		{
			if (!MethodWriter.TypeMethodsCanBeDirectlyCalled(context.GetReadOnlyContext(), type))
			{
				continue;
			}
			foreach (TypeWritingInformation item in WritingUtils.TypeToWritingInformation(readOnlyContext, type))
			{
				yield return item;
			}
		}
	}

	public override ReadOnlyCollection<ReadOnlyCollection<TypeWritingInformation>> Chunk(ReadOnlyCollection<TypeWritingInformation> items)
	{
		return items.ChunkByApproximateGeneratedCodeSize();
	}

	protected override string ProfilerSectionDetailsForItem(TypeWritingInformation item)
	{
		return item.ProfilerSectionName;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeWritingInformation item, NPath filePath)
	{
		SourceWriter.WriteTypesMethods(context, writer, in item, filePath, writeMarshalingDefinitions: true);
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
