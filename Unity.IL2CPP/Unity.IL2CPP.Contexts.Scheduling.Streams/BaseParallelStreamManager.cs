using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public abstract class BaseParallelStreamManager<TItem, TWritingItem, TStream> : BaseStreamManager<TItem, TWritingItem, TStream> where TStream : IStream
{
	protected BaseParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> writerCallbacks)
		: base(outputDirectory, baseFileName, writerCallbacks)
	{
	}

	public override void Write(GlobalWriteContext context, ICollection<TItem> items)
	{
		context.Services.Scheduler.Enqueue(context, DoFilterSortingAndChunking, new SharedTag(items.ToList().AsReadOnly(), _writerCallbacks, _baseFileName, _outputDirectory));
	}

	private void DoFilterSortingAndChunking(WorkItemData<GlobalWriteContext, SharedTag> data)
	{
		SharedTag sharedTag = data.Tag;
		List<TWritingItem> filteredAndSorted = BaseStreamManager<TItem, TWritingItem, TStream>.FilterAndSort(data.Context, sharedTag.Items, sharedTag.Callbacks);
		if (filteredAndSorted.Count != 0)
		{
			ReadOnlyCollection<ReadOnlyCollection<TWritingItem>> chunks = BaseStreamManager<TItem, TWritingItem, TStream>.Chunk(data.Context, filteredAndSorted.AsReadOnly(), sharedTag.Callbacks);
			for (int i = 0; i < chunks.Count; i++)
			{
				ProcessChunk(data.Context, chunks[i], BaseStreamManager<TItem, TWritingItem, TStream>.GetChunkFilePath(sharedTag.OutputDirectory, sharedTag.BaseFileName, i), sharedTag);
			}
		}
	}

	protected abstract void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> chunk, NPath chunkFilePath, SharedTag sharedTag);
}
