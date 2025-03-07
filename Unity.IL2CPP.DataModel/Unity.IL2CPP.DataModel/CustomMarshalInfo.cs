using System;

namespace Unity.IL2CPP.DataModel;

public sealed class CustomMarshalInfo : MarshalInfo
{
	private TypeReference _managedType;

	public Guid Guid { get; }

	public string UnmanagedType { get; }

	public string Cookie { get; }

	public TypeReference ManagedType
	{
		get
		{
			if (_managedType == null)
			{
				throw new UninitializedDataAccessException("ManagedType");
			}
			return _managedType;
		}
	}

	public CustomMarshalInfo(Guid guid, string unmanagedType, string cookie)
		: base(NativeType.CustomMarshaler)
	{
		Guid = guid;
		UnmanagedType = unmanagedType;
		Cookie = cookie;
	}

	internal void InitializeManagedType(TypeReference managedType)
	{
		_managedType = managedType;
	}
}
