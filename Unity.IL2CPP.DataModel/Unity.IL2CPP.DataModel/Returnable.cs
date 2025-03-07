using System;

namespace Unity.IL2CPP.DataModel;

public struct Returnable<T> : IDisposable where T : class
{
	private readonly Action<T> _returnItem;

	public T Value { get; private set; }

	public Returnable(T value, Action<T> returnItem)
	{
		Value = value;
		_returnItem = returnItem;
	}

	public void Dispose()
	{
		if (Value != null)
		{
			_returnItem(Value);
			Value = null;
		}
	}
}
