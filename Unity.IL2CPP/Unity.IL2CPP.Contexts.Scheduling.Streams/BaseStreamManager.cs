using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public abstract class BaseStreamManager<TItem, TWritingItem, TStream> where TStream : IStream
{
	protected class SharedTag
	{
		public readonly ReadOnlyCollection<TItem> Items;

		public readonly IStreamWriterCallbacks<TItem, TWritingItem, TStream> Callbacks;

		public readonly NPath BaseFileName;

		public readonly NPath OutputDirectory;

		public SharedTag(ReadOnlyCollection<TItem> items, IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks, NPath baseFileName, NPath outputDirectory)
		{
			Items = items;
			Callbacks = callbacks;
			BaseFileName = baseFileName;
			OutputDirectory = outputDirectory;
		}
	}

	protected readonly NPath _outputDirectory;

	protected readonly NPath _baseFileName;

	protected readonly IStreamWriterCallbacks<TItem, TWritingItem, TStream> _writerCallbacks;

	public BaseStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> writerCallbacks)
	{
		if (!baseFileName.IsRelative)
		{
			throw new ArgumentException("baseFileName must be a relative path but it was absolute");
		}
		_outputDirectory = outputDirectory;
		_baseFileName = baseFileName;
		_writerCallbacks = writerCallbacks;
	}

	public abstract void Write(GlobalWriteContext context, ICollection<TItem> items);

	protected static List<TWritingItem> FilterAndSort(GlobalWriteContext context, ICollection<TItem> items, IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks)
	{
		ITinyProfilerService tinyProfiler = context.Services.TinyProfiler;
		List<TWritingItem> filtered;
		using (tinyProfiler.Section("FilterAndMap", callbacks.Name))
		{
			filtered = callbacks.CreateAndFilterItemsForWriting(context, items).ToList();
		}
		if (callbacks.RequiresSortingBeforeChunking)
		{
			using (tinyProfiler.Section("Sort", callbacks.Name))
			{
				filtered.Sort(callbacks.CreateComparer());
			}
		}
		return filtered;
	}

	protected static string GetChunkFilePath(NPath outputDirectory, NPath baseFileName, int index)
	{
		if (index == 0)
		{
			return outputDirectory.Combine(baseFileName);
		}
		return outputDirectory.Combine($"{baseFileName.FileNameWithoutExtension}__{index}{baseFileName.ExtensionWithDot}");
	}

	protected static ReadOnlyCollection<ReadOnlyCollection<TWritingItem>> Chunk(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> items, IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks)
	{
		using (context.Services.TinyProfiler.Section("Chunk", callbacks.Name))
		{
			return callbacks.Chunk(items);
		}
	}

	protected static TStream GetAvailableStream(SourceWritingContext context, IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks)
	{
		return callbacks.CreateWriter(context);
	}
}
