using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.Dat;
using Unity.IL2CPP.Metadata.Dat.SectionWriters;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;

public class WriteGlobalMetadataDat : ScheduledStep
{
	protected override string Name => "Write Metadata Dat";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	public void Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		using (CreateProfilerSectionAroundScheduling(scheduler.SchedulingContext, scheduler.WorkIsDoneOnDifferentThread))
		{
			if (!Skip(scheduler.SchedulingContext))
			{
				ReadOnlyCollection<BaseSectionWriter> sections = new BaseSectionWriter[17]
				{
					new Unity.IL2CPP.Metadata.Dat.SectionWriters.StringWriter(),
					new MetadataStringWriter(),
					new EventWriter(),
					new PropertyWriter(),
					new Unity.IL2CPP.Metadata.Dat.SectionWriters.MethodWriter(),
					new FieldAndParameterDataWriter(),
					new ParameterWriter(),
					new FieldWriter(),
					new GenericsDataWriter(),
					new NestedTypesAndInterfacesWriter(),
					new VTableWriter(),
					new InterfaceOffsetsWriter(),
					new TypeDefinitionsWriter(),
					new AssemblyAndAttributeDataWriter(),
					new UnresolvedVirtualCallWriter(),
					new WindowsRuntimeWriter(),
					new ExportedTypeWriter()
				}.AsReadOnly();
				scheduler.EnqueueItemsAndContinueWithResults<GlobalWriteContext, BaseSectionWriter, ReadOnlyCollection<DatSection>, object>(scheduler.QueuingContext, sections, ProcessWriter, WriteFinalDat, null);
			}
		}
	}

	private static void WriteFinalDat(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<BaseSectionWriter, ReadOnlyCollection<DatSection>>>, object> data)
	{
		using (data.Context.Services.TinyProfiler.Section("Write Metadata Dat"))
		{
			SourceWritingContext context = data.Context.CreateSourceWritingContext();
			DatSection[] allSections = data.Item.SelectMany((ResultData<BaseSectionWriter, ReadOnlyCollection<DatSection>> r) => r.Result).ToArray();
			using FileStream binary = new FileStream(context.Global.InputData.MetadataFolder.MakeAbsolute().CreateDirectory().Combine("global-metadata.dat")
				.ToString(), FileMode.Create, FileAccess.Write);
			int headerCount = allSections.Length * 2 + 2;
			List<uint> header = new List<uint>(headerCount);
			header.Add(4205910959u);
			header.Add(31u);
			binary.Seek(headerCount * 4, SeekOrigin.Begin);
			DatSection[] array = allSections;
			foreach (DatSection section in array)
			{
				WriteStreamAndRecordHeader(context, section.Name, binary, section.Stream, header, section.SectionAlignment);
				section.Dispose();
			}
			binary.Seek(0L, SeekOrigin.Begin);
			foreach (uint headerValue in header)
			{
				binary.WriteUInt(headerValue);
			}
		}
	}

	private static ReadOnlyCollection<DatSection> ProcessWriter(WorkItemData<GlobalWriteContext, BaseSectionWriter, object> data)
	{
		using (data.Context.Services.TinyProfiler.Section(data.Item.Name))
		{
			return data.Item.Write(data.Context.CreateSourceWritingContext());
		}
	}

	private static void WriteStreamAndRecordHeader(SourceWritingContext context, string name, Stream outputStream, Stream dataStream, List<uint> headerData, int alignment)
	{
		if (dataStream.Position % 4 != 0L)
		{
			throw new ArgumentException($"Data stream is not aligned to minimum alignment of {4}", "dataStream");
		}
		if (outputStream.Position % 4 != 0L)
		{
			throw new ArgumentException($"Stream is not aligned to minimum alignment of {4}", "outputStream");
		}
		if (alignment != 0)
		{
			outputStream.AlignTo(alignment);
		}
		context.Global.Collectors.Stats.RecordMetadataStream(name, outputStream.Position);
		checked
		{
			uint startPos = (uint)outputStream.Position;
			headerData.Add(startPos);
			dataStream.Seek(0L, SeekOrigin.Begin);
			dataStream.CopyTo(outputStream);
			headerData.Add((uint)outputStream.Position - startPos);
		}
	}
}
