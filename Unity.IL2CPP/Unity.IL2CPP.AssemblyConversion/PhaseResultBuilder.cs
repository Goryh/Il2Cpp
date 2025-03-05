using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion;

internal static class PhaseResultBuilder
{
	public static object[] Complete(ReadOnlyContext context, (Func<object>, string)[] actions)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		if (context.Global.Parameters.EnableSerialConversion)
		{
			return MapSerial(actions, ((Func<object>, string) pair) => ExecuteProfiled(tinyProfiler, pair));
		}
		return Map(actions, ((Func<object>, string) pair) => ExecuteProfiled(tinyProfiler, pair));
	}

	private static object ExecuteProfiled(ITinyProfilerService tinyProfiler, (Func<object>, string) actionSectionNamePair)
	{
		using (tinyProfiler.Section(actionSectionNamePair.Item2))
		{
			return actionSectionNamePair.Item1();
		}
	}

	private static TResult[] MapSerial<TSource, TResult>(TSource[] source, Func<TSource, TResult> func)
	{
		TResult[] results = new TResult[source.Length];
		for (int i = 0; i < source.Length; i++)
		{
			results[i] = func(source[i]);
		}
		return results;
	}

	private static TResult[] Map<TSource, TResult>(TSource[] source, Func<TSource, TResult> func)
	{
		TResult[] results = new TResult[source.Length];
		Parallel.ForEach(ItemAndIndex(source), delegate((TSource, int) item)
		{
			TResult val = func(item.Item1);
			results[item.Item2] = val;
		});
		return results;
	}

	private static IEnumerable<(TSource, int)> ItemAndIndex<TSource>(TSource[] source)
	{
		for (int i = 0; i < source.Length; i++)
		{
			yield return (source[i], i);
		}
	}
}
