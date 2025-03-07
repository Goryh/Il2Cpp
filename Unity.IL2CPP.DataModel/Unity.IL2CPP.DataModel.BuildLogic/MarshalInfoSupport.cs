using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public static class MarshalInfoSupport
{
	public static void ProcessType(TypeDefinition type)
	{
		if (!type.IsWindowsRuntime)
		{
			ProcessFields(type);
			ProcessMethods(type);
		}
	}

	private static void ProcessFields(TypeDefinition type)
	{
		if (type.IsPrimitive || type.IsEnum)
		{
			return;
		}
		foreach (FieldDefinition field in type.Fields)
		{
			ProcessObject(field.FieldType, field, NativeType.IUnknown);
			ProcessBoolean(field.FieldType, field, NativeType.Boolean);
		}
	}

	private static void ProcessMethods(TypeDefinition type)
	{
		foreach (MethodDefinition method in type.Methods)
		{
			MethodReturnType methodReturnType = method.MethodReturnType;
			if (type.IsComInterface)
			{
				ProcessObject(methodReturnType.ReturnType, methodReturnType, NativeType.Struct);
			}
			ProcessBoolean(methodReturnType.ReturnType, methodReturnType, type.IsComInterface ? NativeType.VariantBool : NativeType.Boolean);
			if (!type.IsComInterface && !method.IsPInvokeImpl)
			{
				continue;
			}
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				ProcessObject(parameter.ParameterType, parameter, NativeType.Struct);
				ProcessBoolean(parameter.ParameterType, parameter, type.IsComInterface ? NativeType.VariantBool : NativeType.Boolean);
			}
		}
	}

	private static void ProcessObject(TypeReference type, IMarshalInfoUpdater provider, NativeType nativeType)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Object:
			if (!provider.HasMarshalInfo)
			{
				provider.UpdateMarshalInfo(new MarshalInfo(nativeType));
			}
			break;
		case MetadataType.ByReference:
			ProcessObject(((ByReferenceType)type).ElementType, provider, nativeType);
			break;
		case MetadataType.Array:
			if (((ArrayType)type).ElementType.MetadataType == MetadataType.Object && provider.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo && (arrayMarshalInfo.ElementType == NativeType.None || arrayMarshalInfo.ElementType == NativeType.Max))
			{
				provider.UpdateMarshalInfo(new ArrayMarshalInfo(nativeType, arrayMarshalInfo.SizeParameterIndex, arrayMarshalInfo.Size, arrayMarshalInfo.SizeParameterMultiplier));
			}
			break;
		}
	}

	private static void ProcessBoolean(TypeReference type, IMarshalInfoUpdater provider, NativeType nativeType)
	{
		switch (type.MetadataType)
		{
		case MetadataType.Boolean:
			if (!provider.HasMarshalInfo)
			{
				provider.UpdateMarshalInfo(new MarshalInfo(nativeType));
			}
			break;
		case MetadataType.ByReference:
			ProcessBoolean(((ByReferenceType)type).ElementType, provider, nativeType);
			break;
		case MetadataType.Array:
			if (((ArrayType)type).ElementType.MetadataType == MetadataType.Boolean && provider.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo && (arrayMarshalInfo.ElementType == NativeType.None || arrayMarshalInfo.ElementType == NativeType.Max))
			{
				provider.UpdateMarshalInfo(new ArrayMarshalInfo(nativeType, arrayMarshalInfo.SizeParameterIndex, arrayMarshalInfo.Size, arrayMarshalInfo.SizeParameterMultiplier));
			}
			break;
		}
	}
}
