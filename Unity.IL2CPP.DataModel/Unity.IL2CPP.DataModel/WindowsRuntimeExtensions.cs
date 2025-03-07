namespace Unity.IL2CPP.DataModel;

public static class WindowsRuntimeExtensions
{
	public static bool IsWindowsRuntimePrimitiveType(this TypeReference type)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
		case MetadataType.Char:
		case MetadataType.Byte:
		case MetadataType.Int16:
		case MetadataType.UInt16:
		case MetadataType.Int32:
		case MetadataType.UInt32:
		case MetadataType.Int64:
		case MetadataType.UInt64:
		case MetadataType.Single:
		case MetadataType.Double:
		case MetadataType.String:
		case MetadataType.Object:
			return true;
		case MetadataType.ValueType:
			if (type.Namespace == "System")
			{
				return type.Name == "Guid";
			}
			return false;
		default:
			return false;
		}
	}

	public static bool IsExposedToWindowsRuntime(this TypeDefinition type)
	{
		if (type.IsWindowsRuntimeProjection)
		{
			ModuleDefinition module = type.Module;
			if (module != null && module.MetadataKind == MetadataKind.ManagedWindowsMetadata)
			{
				return type.IsPublic;
			}
		}
		return type.IsWindowsRuntime;
	}

	public static bool IsComOrWindowsRuntimeType(this TypeDefinition type)
	{
		if (type.IsValueType)
		{
			return false;
		}
		if (type.IsDelegate)
		{
			return false;
		}
		if (type.IsIl2CppComObject || type.IsIl2CppComDelegate)
		{
			return true;
		}
		if (type.IsImport)
		{
			if (type.IsWindowsRuntimeProjection)
			{
				return type.IsExposedToWindowsRuntime();
			}
			return true;
		}
		return type.IsWindowsRuntime;
	}
}
