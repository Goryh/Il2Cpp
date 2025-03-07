using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP.DataModel.Awesome.CFG;

internal class TryCatchInfoCollector
{
	private readonly LazyDictionary<int, TryCatchInfo> _infos = new LazyDictionary<int, TryCatchInfo>(() => new TryCatchInfo());

	public static LazyDictionary<int, TryCatchInfo> Collect(MethodBody body)
	{
		TryCatchInfoCollector tryCatchInfoCollector = new TryCatchInfoCollector();
		tryCatchInfoCollector.CollectTryCatchInfos(body);
		return tryCatchInfoCollector._infos;
	}

	private void CollectTryCatchInfos(MethodBody body)
	{
		BuildTryCatchScope(body.ExceptionHandlers);
	}

	private void BuildTryCatchScope(IList<ExceptionHandler> handlers)
	{
		foreach (var tryGroup in from h in handlers
			group h by new
			{
				TryStart = h.TryStart.Offset,
				TryEnd = h.TryEnd.Offset
			})
		{
			_infos[tryGroup.Key.TryStart].TryStart++;
			_infos[tryGroup.Key.TryEnd].TryEnd++;
			ExceptionHandler[] tryGroupArray = tryGroup.ToArray();
			for (int i = 0; i < tryGroupArray.Length; i++)
			{
				ExceptionHandler h2 = tryGroupArray[i];
				int startOffset = h2.HandlerStart.Offset;
				int endOffset = h2.HandlerEnd?.Offset ?? (-1);
				if (h2.HandlerType == ExceptionHandlerType.Catch)
				{
					_infos[startOffset].CatchStart++;
					_infos[endOffset].CatchEnd++;
					continue;
				}
				if (h2.HandlerType == ExceptionHandlerType.Finally)
				{
					_infos[startOffset].FinallyStart++;
					_infos[endOffset].FinallyEnd++;
					continue;
				}
				if (h2.HandlerType == ExceptionHandlerType.Fault)
				{
					_infos[startOffset].FaultStart++;
					_infos[endOffset].FaultEnd++;
					continue;
				}
				if (h2.HandlerType == ExceptionHandlerType.Filter)
				{
					InsertNestedTryWorkAroundIfNeeded(tryGroup.Key.TryStart, i, tryGroupArray);
					_infos[startOffset].CatchStart++;
					_infos[endOffset].CatchEnd++;
					_infos[h2.FilterStart.Offset].FilterStart++;
					_infos[startOffset].FilterEnd++;
					continue;
				}
				throw new InvalidOperationException($"Unexpected handler type '{h2.HandlerType}' encountered.");
			}
		}
	}

	private void InsertNestedTryWorkAroundIfNeeded(int tryStartOffset, int currentIndex, ExceptionHandler[] tryGroupArray)
	{
		if (tryGroupArray.Length == 1)
		{
			return;
		}
		if (currentIndex > 0)
		{
			_infos[tryStartOffset].TryStart++;
			_infos[tryGroupArray[currentIndex].FilterStart.Offset].TryEnd++;
		}
		if (currentIndex < tryGroupArray.Length - 1)
		{
			ExceptionHandler nextHandler = tryGroupArray[currentIndex + 1];
			if (nextHandler.HandlerType != ExceptionHandlerType.Filter)
			{
				_infos[tryStartOffset].TryStart++;
				_infos[nextHandler.HandlerStart.Offset].TryEnd++;
			}
		}
	}
}
