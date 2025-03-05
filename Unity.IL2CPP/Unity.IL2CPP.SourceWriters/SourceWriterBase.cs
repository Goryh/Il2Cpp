using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.SourceWriters;

public abstract class SourceWriterBase<TItem, TWritingItem> : IStreamWriterCallbacks<TItem, TWritingItem, IGeneratedMethodCodeStream>
{
	public abstract string Name { get; }

	public abstract bool RequiresSortingBeforeChunking { get; }

	public abstract FileCategory FileCategory { get; }

	public IGeneratedMethodCodeStream CreateWriter(SourceWritingContext context)
	{
		return new InMemoryGeneratedMethodCodeWriter(context);
	}

	public virtual IComparer<TWritingItem> CreateComparer()
	{
		throw new NotSupportedException("Must be implemented when RequiresSortingBeforeChunking returns true");
	}

	public void MergeAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<ResultData<TWritingItem, IGeneratedMethodCodeStream>> results, NPath filePath)
	{
		string writtenFileName;
		using (IGeneratedMethodCodeStream sourceWriter = context.CreateSourceWriter(FileCategory, filePath))
		{
			WriteHeader(sourceWriter);
			foreach (ResultData<TWritingItem, IGeneratedMethodCodeStream> result in results)
			{
				sourceWriter.Write(result.Result);
			}
			WriteFooter(sourceWriter);
			WriteEnd(sourceWriter);
			writtenFileName = sourceWriter.FileName;
		}
		context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), writtenFileName);
	}

	public void FlushStream(GlobalWriteContext context, IGeneratedMethodCodeStream stream, NPath filePath)
	{
		string writtenFileName;
		using (IGeneratedMethodCodeStream sourceWriter = context.CreateSourceWriter(FileCategory, filePath))
		{
			WriteHeader(sourceWriter);
			sourceWriter.Write(stream);
			WriteFooter(sourceWriter);
			WriteEnd(sourceWriter);
			writtenFileName = sourceWriter.FileName;
		}
		context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), writtenFileName);
	}

	public void WriteAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<TWritingItem> items, NPath filePath)
	{
		SourceWritingContext sourceWritingContext = context.CreateSourceWritingContext();
		string writtenFileName;
		using (IGeneratedMethodCodeStream fileWriter = sourceWritingContext.CreateProfiledManagedSourceWriter(FileCategory, filePath))
		{
			WriteHeader(fileWriter);
			foreach (TWritingItem item in items)
			{
				WriteItem(new StreamWorkItemData<TWritingItem, IGeneratedMethodCodeStream>(sourceWritingContext, item, fileWriter, filePath));
			}
			WriteFooter(fileWriter);
			WriteEnd(fileWriter);
			writtenFileName = fileWriter.FileName;
		}
		context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), writtenFileName);
	}

	public abstract IEnumerable<TWritingItem> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<TItem> items);

	public abstract ReadOnlyCollection<ReadOnlyCollection<TWritingItem>> Chunk(ReadOnlyCollection<TWritingItem> items);

	public void WriteItem(StreamWorkItemData<TWritingItem, IGeneratedMethodCodeStream> data)
	{
		using (data.Context.Global.Services.TinyProfiler.Section(Name, ProfilerSectionDetailsForItem(data.Item)))
		{
			WriteItem(data.Context, data.Stream, data.Item, data.FilePath);
		}
	}

	protected abstract string ProfilerSectionDetailsForItem(TWritingItem item);

	protected abstract void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TWritingItem item, NPath filePath);

	protected abstract void WriteHeader(IGeneratedMethodCodeWriter writer);

	protected abstract void WriteFooter(IGeneratedMethodCodeWriter writer);

	protected abstract void WriteEnd(IGeneratedMethodCodeStream writer);
}
