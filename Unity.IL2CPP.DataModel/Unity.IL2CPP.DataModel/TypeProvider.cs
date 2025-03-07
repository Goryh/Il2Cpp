namespace Unity.IL2CPP.DataModel;

public class TypeProvider
{
	private readonly TypeContext _context;

	public AssemblyDefinition Corlib => _context.SystemAssembly;

	public TypeDefinition SystemObject => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Object);

	public TypeDefinition SystemString => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.String);

	public TypeDefinition SystemArray => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Array);

	public TypeDefinition SystemException => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Exception);

	public TypeDefinition SystemDelegate => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Delegate);

	public TypeDefinition SystemMulticastDelegate => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.MulticastDelegate);

	public TypeDefinition SystemByte => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Byte);

	public TypeDefinition SystemUInt16 => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt16);

	public TypeDefinition SystemIntPtr => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.IntPtr);

	public TypeDefinition SystemUIntPtr => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UIntPtr);

	public TypeDefinition SystemVoid => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Void);

	public TypeDefinition SystemNullable => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Nullable);

	public TypeDefinition SystemType => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Type);

	public TypeReference Int32TypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int32);

	public TypeReference Int16TypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int16);

	public TypeReference UInt16TypeReference => SystemUInt16;

	public TypeReference SByteTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.SByte);

	public TypeReference ByteTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Byte);

	public TypeReference BoolTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Boolean);

	public TypeReference CharTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Char);

	public TypeReference IntPtrTypeReference => SystemIntPtr;

	public TypeReference UIntPtrTypeReference => SystemUIntPtr;

	public TypeReference Int64TypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Int64);

	public TypeReference UInt32TypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt32);

	public TypeReference UInt64TypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.UInt64);

	public TypeReference SingleTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Single);

	public TypeReference DoubleTypeReference => _context.GetSystemType(Unity.IL2CPP.DataModel.SystemType.Double);

	public TypeReference ObjectTypeReference => SystemObject;

	public TypeReference StringTypeReference => SystemString;

	public TypeReference Il2CppFullySharedGenericTypeReference => _context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric);

	internal TypeProvider(TypeContext context)
	{
		_context = context;
	}

	public TypeReference GetSharedEnumType(TypeReference enumType)
	{
		return _context.GetSharedEnumType(enumType);
	}

	public TypeDefinition OptionalResolve(SystemType systemType)
	{
		return _context.GetSystemType(systemType);
	}
}
