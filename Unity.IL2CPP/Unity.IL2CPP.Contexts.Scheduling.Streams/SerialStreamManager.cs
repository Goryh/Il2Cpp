using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public class SerialStreamManager<TItem, TWritingItem, TStream> : BaseStreamManager<TItem, TWritingItem, TStream> where TStream : IStream
{
	public SerialStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> writerCallbacks)
		: base(outputDirectory, baseFileName, writerCallbacks)
	{
	}

	public override void Write(GlobalWriteContext context, ICollection<TItem> items)
	{
		List<TWritingItem> filtered = BaseStreamManager<TItem, TWritingItem, TStream>.FilterAndSort(context, items, _writerCallbacks);
		if (filtered.Count != 0)
		{
			ReadOnlyCollection<ReadOnlyCollection<TWritingItem>> batches = BaseStreamManager<TItem, TWritingItem, TStream>.Chunk(context, filtered.AsReadOnly(), _writerCallbacks);
			for (int i = 0; i < batches.Count; i++)
			{
				_writerCallbacks.WriteAndFlushStreams(context, batches[i], BaseStreamManager<TItem, TWritingItem, TStream>.GetChunkFilePath(_outputDirectory, _baseFileName, i));
			}
		}
	}
}
