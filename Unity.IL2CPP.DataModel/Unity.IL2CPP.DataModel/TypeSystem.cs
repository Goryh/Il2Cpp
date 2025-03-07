namespace Unity.IL2CPP.DataModel;

public struct TypeSystem
{
	private readonly TypeContext _typeContext;

	public TypeReference Object => _typeContext.GetSystemType(SystemType.Object);

	public TypeReference Void => _typeContext.GetSystemType(SystemType.Void);

	public TypeReference Boolean => _typeContext.GetSystemType(SystemType.Boolean);

	public TypeReference Char => _typeContext.GetSystemType(SystemType.Char);

	public TypeReference SByte => _typeContext.GetSystemType(SystemType.SByte);

	public TypeReference Byte => _typeContext.GetSystemType(SystemType.Byte);

	public TypeReference Int16 => _typeContext.GetSystemType(SystemType.Int16);

	public TypeReference UInt16 => _typeContext.GetSystemType(SystemType.UInt16);

	public TypeReference Int32 => _typeContext.GetSystemType(SystemType.Int32);

	public TypeReference UInt32 => _typeContext.GetSystemType(SystemType.UInt32);

	public TypeReference Int64 => _typeContext.GetSystemType(SystemType.Int64);

	public TypeReference UInt64 => _typeContext.GetSystemType(SystemType.UInt64);

	public TypeReference Single => _typeContext.GetSystemType(SystemType.Single);

	public TypeReference Double => _typeContext.GetSystemType(SystemType.Double);

	public TypeReference IntPtr => _typeContext.GetSystemType(SystemType.IntPtr);

	public TypeReference UIntPtr => _typeContext.GetSystemType(SystemType.UIntPtr);

	public TypeReference String => _typeContext.GetSystemType(SystemType.String);

	public TypeReference TypedReference => _typeContext.GetSystemType(SystemType.TypedReference);

	internal TypeSystem(TypeContext typeContext)
	{
		_typeContext = typeContext;
	}
}
