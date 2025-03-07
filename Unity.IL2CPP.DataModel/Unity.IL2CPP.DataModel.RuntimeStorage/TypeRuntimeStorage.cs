using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.RuntimeStorage;

internal static class TypeRuntimeStorage
{
	private enum FieldType
	{
		Instance,
		Static,
		ThreadStatic
	}

	internal static RuntimeStorageKind GetTypeDefinitionRuntimeStorageKind(TypeDefinition typeDefinition)
	{
		if (typeDefinition.IsSystemEnum)
		{
			return RuntimeStorageKind.ReferenceType;
		}
		TypeDefinition systemValueType = typeDefinition.Context.GetSystemType(SystemType.ValueType);
		TypeDefinition systemEnumType = typeDefinition.Context.GetSystemType(SystemType.Enum);
		if ((systemValueType != null && typeDefinition.BaseType == systemValueType) || (systemEnumType != null && typeDefinition.BaseType == systemEnumType))
		{
			return RuntimeStorageKind.ValueType;
		}
		return RuntimeStorageKind.ReferenceType;
	}

	internal static (RuntimeStorageKind, RuntimeFieldLayoutKind) RuntimeStorageKindAndFieldLayout(ITypeFactory typeFactory, TypeReference typeReference)
	{
		RuntimeFieldLayoutKind runtimeFieldLayout = RuntimeFieldLayout(typeFactory, typeReference);
		RuntimeStorageKind runtimeStorageKind = RuntimeStorageNoSharing(typeReference);
		if (runtimeStorageKind == RuntimeStorageKind.ValueType && runtimeFieldLayout == RuntimeFieldLayoutKind.Variable)
		{
			runtimeStorageKind = RuntimeStorageKind.VariableSizedValueType;
		}
		return (runtimeStorageKind, runtimeFieldLayout);
	}

	private static RuntimeStorageKind RuntimeStorageNoSharing(TypeReference typeReference)
	{
		if (typeReference.IsIl2CppFullySharedGenericType)
		{
			return RuntimeStorageKind.VariableSizedAny;
		}
		if (typeReference.IsPointer)
		{
			return RuntimeStorageKind.Pointer;
		}
		if (typeReference.IsValueType)
		{
			return RuntimeStorageKind.ValueType;
		}
		return RuntimeStorageKind.ReferenceType;
	}

	internal static RuntimeFieldLayoutKind RuntimeFieldLayout(ITypeFactory typeFactory, TypeReference typeReference)
	{
		if (typeReference.IsIl2CppFullySharedGenericType)
		{
			return RuntimeFieldLayoutKind.Variable;
		}
		return RuntimeFieldLayout(typeFactory, typeReference, FieldType.Instance);
	}

	internal static RuntimeFieldLayoutKind StaticFieldLayout(ITypeFactory typeFactory, TypeReference typeReference)
	{
		if (typeReference.Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric) == null)
		{
			return RuntimeFieldLayoutKind.Fixed;
		}
		return RuntimeFieldLayout(typeFactory, typeReference, FieldType.Static);
	}

	internal static RuntimeFieldLayoutKind ThreadStaticFieldLayout(ITypeFactory typeFactory, TypeReference typeReference)
	{
		if (typeReference.Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppFullySharedGeneric) == null)
		{
			return RuntimeFieldLayoutKind.Fixed;
		}
		return RuntimeFieldLayout(typeFactory, typeReference, FieldType.ThreadStatic);
	}

	private static RuntimeFieldLayoutKind RuntimeFieldLayout(ITypeFactory typeFactory, TypeReference typeReference, FieldType fieldType)
	{
		typeReference = typeReference.GetNonPinnedAndNonByReferenceType();
		if (typeReference.IsPointer)
		{
			typeReference = typeReference.GetElementType();
		}
		return RuntimeFieldLayoutInner(typeFactory, typeReference, fieldType);
	}

	private static RuntimeFieldLayoutKind RuntimeFieldLayoutInner(ITypeFactory typeFactory, TypeReference typeReference, FieldType fieldType)
	{
		TypeDefinition typeDefinition = typeReference.Resolve();
		if (typeReference.IsGenericInstance)
		{
			TypeResolver typeResolver = typeFactory.ResolverFor(typeReference);
			foreach (FieldDefinition field in typeDefinition.Fields)
			{
				if (FieldMatches(field, fieldType))
				{
					TypeReference resolvedFieldType = typeResolver.ResolveFieldType(field);
					if (resolvedFieldType.IsGenericParameter)
					{
						return RuntimeFieldLayoutKind.Variable;
					}
					if (resolvedFieldType.IsIl2CppFullySharedGenericType)
					{
						return RuntimeFieldLayoutKind.Variable;
					}
					if (resolvedFieldType.IsValueType && resolvedFieldType.GetRuntimeFieldLayout(typeFactory) == RuntimeFieldLayoutKind.Variable)
					{
						return RuntimeFieldLayoutKind.Variable;
					}
				}
			}
			if (typeDefinition.BaseType != null && fieldType == FieldType.Instance)
			{
				return typeReference.GetBaseType(typeFactory).GetRuntimeFieldLayout(typeFactory);
			}
		}
		return RuntimeFieldLayoutKind.Fixed;
	}

	private static bool FieldMatches(FieldDefinition field, FieldType fieldType)
	{
		return fieldType switch
		{
			FieldType.Static => field.IsNormalStatic, 
			FieldType.ThreadStatic => field.IsThreadStatic, 
			_ => !field.IsStatic, 
		};
	}
}
