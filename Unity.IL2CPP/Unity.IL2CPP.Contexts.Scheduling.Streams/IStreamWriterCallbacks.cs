using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public interface IStreamWriterCallbacks<TItem, TWritingItem, TStream>
{
	string Name { get; }

	bool RequiresSortingBeforeChunking { get; }

	TStream CreateWriter(SourceWritingContext context);

	IComparer<TWritingItem> CreateComparer();

	void MergeAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<ResultData<TWritingItem, TStream>> results, NPath filePath);

	void FlushStream(GlobalWriteContext context, TStream stream, NPath filePath);

	void WriteAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> items, NPath filePath);

	IEnumerable<TWritingItem> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TItem> items);

	ReadOnlyCollection<ReadOnlyCollection<TWritingItem>> Chunk(ReadOnlyCollection<TWritingItem> items);

	void WriteItem(StreamWorkItemData<TWritingItem, TStream> data);
}
