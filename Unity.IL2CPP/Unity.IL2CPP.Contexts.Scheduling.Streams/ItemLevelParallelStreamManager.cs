using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public class ItemLevelParallelStreamManager<TItem, TWritingItem, TStream> : BaseParallelStreamManager<TItem, TWritingItem, TStream> where TStream : IStream
{
	private class ItemLevelParallelTag
	{
		public readonly NPath ChunkFilePath;

		public readonly SharedTag SharedTag;

		public ItemLevelParallelTag(SharedTag sharedTag, NPath chunkFilePath)
		{
			SharedTag = sharedTag;
			ChunkFilePath = chunkFilePath;
		}
	}

	private class ResultOrdererByItem : IComparer<ResultData<TWritingItem, TStream>>
	{
		private readonly IComparer<TWritingItem> _comparer;

		public ResultOrdererByItem(IComparer<TWritingItem> comparer)
		{
			_comparer = comparer;
		}

		public int Compare(ResultData<TWritingItem, TStream> x, ResultData<TWritingItem, TStream> y)
		{
			return _comparer.Compare(x.Item, y.Item);
		}
	}

	public ItemLevelParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> writerCallbacks)
		: base(outputDirectory, baseFileName, writerCallbacks)
	{
	}

	protected override void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> chunk, NPath chunkFilePath, SharedTag sharedTag)
	{
		context.Services.Scheduler.EnqueueItemsAndContinueWithResults(context, chunk, WorkerWriteItemToStream, WorkerWriteStreamsToFile, new ItemLevelParallelTag(sharedTag, chunkFilePath));
	}

	private static TStream WorkerWriteItemToStream(WorkItemData<GlobalWriteContext, TWritingItem, ItemLevelParallelTag> data)
	{
		using (data.Context.Services.TinyProfiler.Section(data.Tag.ChunkFilePath.FileName))
		{
			IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks = data.Tag.SharedTag.Callbacks;
			SourceWritingContext context = data.Context.CreateSourceWritingContext();
			TStream stream = BaseStreamManager<TItem, TWritingItem, TStream>.GetAvailableStream(context, data.Tag.SharedTag.Callbacks);
			try
			{
				callbacks.WriteItem(new StreamWorkItemData<TWritingItem, TStream>(context, data.Item, stream, data.Tag.ChunkFilePath));
			}
			catch (Exception)
			{
				stream.Dispose();
				throw;
			}
			return stream;
		}
	}

	private static void WorkerWriteStreamsToFile(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<TWritingItem, TStream>>, ItemLevelParallelTag> data)
	{
		ITinyProfilerService tinyProfiler = data.Context.Services.TinyProfiler;
		using (tinyProfiler.Section(data.Tag.ChunkFilePath.FileName))
		{
			try
			{
				using (tinyProfiler.Section("Merge Streams"))
				{
					data.Tag.SharedTag.Callbacks.MergeAndFlushStreams(data.Context, data.Item, data.Tag.ChunkFilePath);
				}
			}
			finally
			{
				foreach (ResultData<TWritingItem, TStream> item2 in data.Item)
				{
					item2.Result.Dispose();
				}
			}
		}
	}
}
