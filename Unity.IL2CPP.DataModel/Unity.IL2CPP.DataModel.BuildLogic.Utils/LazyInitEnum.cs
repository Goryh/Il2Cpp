using System;
using System.Runtime.CompilerServices;

namespace Unity.IL2CPP.DataModel.BuildLogic.Utils;

public struct LazyInitEnum<T> where T : Enum
{
	private T _value;

	public bool IsInitialized
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Unsafe.As<T, uint>(ref _value) != 0;
		}
	}

	public T Value => _value;

	static LazyInitEnum()
	{
		Type underlyingType = Enum.GetUnderlyingType(typeof(T));
		if (underlyingType != typeof(int) && underlyingType != typeof(uint))
		{
			throw new ArgumentOutOfRangeException("LazyInitEnum only supports enum values that are the size of an int/uint");
		}
		if (Enum.IsDefined(typeof(T), 0))
		{
			throw new InvalidOperationException("LazyInitEnum requires that 0 not be a valid enum value");
		}
	}

	public void SetValue(T value)
	{
		_value = value;
	}
}
