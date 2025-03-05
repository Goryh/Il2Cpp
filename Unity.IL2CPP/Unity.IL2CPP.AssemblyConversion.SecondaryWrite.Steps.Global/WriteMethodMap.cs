using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;

public class WriteMethodMap : ChunkedItemsWithPostProcessingAction<GlobalWriteContext, ReadOnlyMethodPointerNameEntry, MemoryStream>
{
	private readonly ReadOnlyCollection<AssemblyDefinition> _assemblies;

	protected override string Name => "Write Method Map";

	protected override string PostProcessingSectionName => "Merge Method Map";

	public WriteMethodMap(ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		_assemblies = assemblies;
	}

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return !context.Parameters.EmitMethodMap;
	}

	protected override string ProfilerDetailsForItem(ReadOnlyCollection<ReadOnlyMethodPointerNameEntry> workerItem)
	{
		return "Write Method Map (Chunked)";
	}

	protected override MemoryStream ProcessItem(GlobalWriteContext context, ReadOnlyCollection<ReadOnlyMethodPointerNameEntry> items)
	{
		MemoryStream stream = new MemoryStream();
		using StreamWriter streamWriter = new StreamWriter(stream, null, -1, leaveOpen: true);
		foreach (ReadOnlyMethodPointerNameEntry item in items)
		{
			WriteEntry(streamWriter, item.Name, item.Method, item.Method.DeclaringType.Module.Assembly.Name.Name);
		}
		return stream;
	}

	protected override ReadOnlyCollection<ReadOnlyCollection<ReadOnlyMethodPointerNameEntry>> Chunk(GlobalSchedulingContext context, ReadOnlyCollection<ReadOnlyMethodPointerNameEntry> items)
	{
		List<ReadOnlyCollection<ReadOnlyMethodPointerNameEntry>> chunks = new List<ReadOnlyCollection<ReadOnlyMethodPointerNameEntry>>();
		if (items != null && items.Count > 0)
		{
			chunks.AddRange(items.Chunk(context.InputData.JobCount * 2));
		}
		if (_assemblies != null && _assemblies.Count > 0)
		{
			chunks.AddRange(_assemblies.Select((AssemblyDefinition asm) => context.Results.SecondaryCollection.MethodPointerNameTable[asm].Items.Cast<ReadOnlyMethodPointerNameEntry>().ToList().AsReadOnly()));
		}
		return chunks.AsReadOnly();
	}

	protected override void PostProcess(GlobalWriteContext context, ReadOnlyCollection<ResultData<ReadOnlyCollection<ReadOnlyMethodPointerNameEntry>, MemoryStream>> data)
	{
		NPath outputPath = context.InputData.SymbolsFolder.EnsureDirectoryExists();
		using StreamWriter methodsOutput = new StreamWriter(context.Services.PathFactory.GetFilePath(FileCategory.Other, outputPath.Combine("MethodMap.tsv")).ToString());
		foreach (ResultData<ReadOnlyCollection<ReadOnlyMethodPointerNameEntry>, MemoryStream> datum in data)
		{
			datum.Result.Seek(0L, SeekOrigin.Begin);
			datum.Result.CopyTo(methodsOutput.BaseStream);
			datum.Result.Dispose();
		}
	}

	private static void WriteEntry(StreamWriter writer, string methodPointerName, MethodReference method, string assemblyName)
	{
		writer.Write(methodPointerName);
		writer.Write('\t');
		writer.Write(method.FullName);
		writer.Write('\t');
		writer.Write(assemblyName);
		writer.WriteLine();
	}
}
