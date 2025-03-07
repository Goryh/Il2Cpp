using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Unity.IL2CPP.DataModel.Stats;

namespace Unity.IL2CPP.DataModel.BuildLogic.Utils;

internal class PerThreadObjectFactory : IDisposable
{
	internal const int DefaultStringBuilderSize = 8000;

	private readonly ThreadLocal<Stack<StringBuilder>> _stringBuilderCache = new ThreadLocal<Stack<StringBuilder>>(() => new Stack<StringBuilder>());

	private readonly Action<StringBuilder> _returnStringBuilder;

	private readonly Statistics _statistics;

	public PerThreadObjectFactory(Statistics statistics)
	{
		_returnStringBuilder = ReturnStringBuilder;
		_statistics = statistics;
	}

	public Returnable<StringBuilder> CheckoutStringBuilder()
	{
		if (_stringBuilderCache.Value.Count > 0)
		{
			StringBuilder stringBuilder = _stringBuilderCache.Value.Pop();
			stringBuilder.Clear();
			return new Returnable<StringBuilder>(stringBuilder, _returnStringBuilder);
		}
		return new Returnable<StringBuilder>(new StringBuilder(8000), _returnStringBuilder);
	}

	private void ReturnStringBuilder(StringBuilder stringBuilder)
	{
		_stringBuilderCache.Value.Push(stringBuilder);
	}

	public void Dispose()
	{
		_stringBuilderCache.Dispose();
	}
}
