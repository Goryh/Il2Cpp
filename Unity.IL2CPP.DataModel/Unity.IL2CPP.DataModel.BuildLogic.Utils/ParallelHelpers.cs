using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Utils;

internal static class ParallelHelpers
{
	public static void ForEachChunkedRoundRobin<TSource>(ICollection<TSource> source, Action<ReadOnlyCollection<TSource>> func, LoadParameters parameters, int overrideChunkDefaultCount = 0)
	{
		ForEach(source.ChunkRoundRobin((overrideChunkDefaultCount != 0) ? overrideChunkDefaultCount : parameters.JobCount), func, parameters.EnableSerial);
	}

	public static void ForEachChunkRoundRobinWithNumber<TSource>(ICollection<TSource> source, Action<(int, ReadOnlyCollection<TSource>)> func, LoadParameters parameters, int overrideChunkDefaultCount = 0)
	{
		ForEach(source.ChunkRoundRobinWithNumber((overrideChunkDefaultCount != 0) ? overrideChunkDefaultCount : parameters.JobCount), func, parameters.EnableSerial);
	}

	public static void ForEachChunked<TSource>(ICollection<TSource> source, Action<ReadOnlyCollection<TSource>> func, LoadParameters parameters, int overrideChunkDefaultCount = 0)
	{
		ForEach(source.Chunk((overrideChunkDefaultCount != 0) ? overrideChunkDefaultCount : parameters.JobCount), func, parameters.EnableSerial);
	}

	public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> func, bool enableSerial)
	{
		if (enableSerial)
		{
			foreach (TSource item in source)
			{
				func(item);
			}
			return;
		}
		Parallel.ForEach(source, func);
	}

	public static TResult[] Map<TSource, TResult>(ReadOnlyCollection<TSource> source, Func<TSource, TResult> func, bool enableSerial)
	{
		if (enableSerial)
		{
			return MapSerial(source, func);
		}
		return MapParallel(source, func);
	}

	private static TResult[] MapSerial<TSource, TResult>(ReadOnlyCollection<TSource> source, Func<TSource, TResult> func)
	{
		TResult[] results = new TResult[source.Count];
		for (int i = 0; i < source.Count; i++)
		{
			results[i] = func(source[i]);
		}
		return results;
	}

	private static TResult[] MapParallel<TSource, TResult>(ReadOnlyCollection<TSource> source, Func<TSource, TResult> func)
	{
		TResult[] results = new TResult[source.Count];
		Parallel.ForEach(ItemAndIndex(source), delegate((TSource, int) item)
		{
			TResult val = func(item.Item1);
			results[item.Item2] = val;
		});
		return results;
	}

	private static IEnumerable<(TSource, int)> ItemAndIndex<TSource>(ReadOnlyCollection<TSource> source)
	{
		for (int i = 0; i < source.Count; i++)
		{
			yield return (source[i], i);
		}
	}
}
