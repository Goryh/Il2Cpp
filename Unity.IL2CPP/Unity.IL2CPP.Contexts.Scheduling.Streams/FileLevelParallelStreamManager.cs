using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public class FileLevelParallelStreamManager<TItem, TWritingItem, TStream> : BaseParallelStreamManager<TItem, TWritingItem, TStream> where TStream : IStream
{
	private class FileLevelTag
	{
		public readonly ReadOnlyCollection<TWritingItem> Chunk;

		public readonly SharedTag SharedTag;

		public readonly NPath ChunkFilePath;

		public FileLevelTag(ReadOnlyCollection<TWritingItem> chunk, SharedTag sharedTag, NPath chunkFilePath)
		{
			Chunk = chunk;
			SharedTag = sharedTag;
			ChunkFilePath = chunkFilePath;
		}
	}

	public FileLevelParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> writerCallbacks)
		: base(outputDirectory, baseFileName, writerCallbacks)
	{
	}

	protected override void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> chunk, NPath chunkFilePath, SharedTag sharedTag)
	{
		context.Services.Scheduler.Enqueue(context, WorkerWriteItemsToFile, new FileLevelTag(chunk, sharedTag, chunkFilePath));
	}

	private static void WorkerWriteItemsToFile(WorkItemData<GlobalWriteContext, FileLevelTag> data)
	{
		ITinyProfilerService tinyProfiler = data.Context.Services.TinyProfiler;
		using (tinyProfiler.Section(data.Tag.ChunkFilePath.FileName))
		{
			FileLevelTag tag = data.Tag;
			IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks = tag.SharedTag.Callbacks;
			SourceWritingContext context = data.Context.CreateSourceWritingContext();
			using TStream stream = BaseStreamManager<TItem, TWritingItem, TStream>.GetAvailableStream(context, tag.SharedTag.Callbacks);
			foreach (TWritingItem item in tag.Chunk)
			{
				callbacks.WriteItem(new StreamWorkItemData<TWritingItem, TStream>(context, item, stream, tag.ChunkFilePath));
			}
			using (tinyProfiler.Section("Flush Stream"))
			{
				tag.SharedTag.Callbacks.FlushStream(data.Context, stream, tag.ChunkFilePath);
			}
		}
	}
}
