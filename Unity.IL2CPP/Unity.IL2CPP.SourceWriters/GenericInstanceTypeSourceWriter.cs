using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.SourceWriters.Utils;

namespace Unity.IL2CPP.SourceWriters;

public class GenericInstanceTypeSourceWriter : SourceWriterBase<GenericInstanceType, TypeWritingInformation>
{
	public override string Name => "WriteGenericInstanceType";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.Generics;

	private GenericInstanceTypeSourceWriter()
	{
	}

	public static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<GenericInstanceType> items)
	{
		ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new GenericInstanceTypeSourceWriter()).Write(context.Global, items);
	}

	public override IEnumerable<TypeWritingInformation> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<GenericInstanceType> items)
	{
		ReadOnlyContext readOnlyContext = context.GetReadOnlyContext();
		foreach (GenericInstanceType type in items)
		{
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
		SourceWriter.WriteTypesMethods(context, writer, in item, filePath, writeMarshalingDefinitions: false);
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
