using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public static class TypeExtensions
{
	public static MethodReference FindMethodByName(this TypeReference type, ITypeFactoryProvider typeFactory, string name)
	{
		foreach (LazilyInflatedMethod method in type.IterateLazilyInflatedMethods(typeFactory.TypeFactory))
		{
			if (method.Name == name)
			{
				return method.InflatedMethod;
			}
		}
		return null;
	}

	public static bool Is(this TypeReference type, SystemType systemType)
	{
		return type == type.Context.GetSystemType(systemType);
	}

	public static bool Is(this TypeReference type, Il2CppCustomType customType)
	{
		return type == type.Context.GetIl2CppCustomType(customType);
	}

	public static bool IsSignedOrUnsignedIntPtr(this TypeReference type)
	{
		if (!type.Is(SystemType.IntPtr))
		{
			return type.Is(SystemType.UIntPtr);
		}
		return true;
	}

	public static IEnumerable<MethodDefinition> GetVirtualMethods(this TypeDefinition typeDefinition)
	{
		return typeDefinition.Methods.Where((MethodDefinition m) => m.IsVirtual);
	}

	public static GenericInstanceType GetSharedType(this TypeReference type, ITypeFactoryProvider provider)
	{
		return type.GetSharedType(provider.TypeFactory);
	}

	public static GenericInstanceType GetCollapsedSignatureType(this GenericInstanceType type, ITypeFactoryProvider provider)
	{
		return type.GetCollapsedSignatureType(provider.TypeFactory);
	}

	public static bool CanShare(this TypeReference type, ITypeFactoryProvider provider)
	{
		return type.CanShare(provider.TypeFactory);
	}

	public static TypeReference GetBaseType(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return typeReference.GetBaseType(provider.TypeFactory);
	}

	public static ReadOnlyCollection<MethodReference> GetMethods(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return typeReference.GetMethods(provider.TypeFactory);
	}

	public static IEnumerable<LazilyInflatedMethod> IterateLazilyInflatedMethods(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return typeReference.IterateLazilyInflatedMethods(provider.TypeFactory);
	}

	public static ReadOnlyCollection<ParameterDefinition> GetResolvedParameters(this MethodReference methodReference, ITypeFactoryProvider provider)
	{
		return methodReference.GetResolvedParameters(provider.TypeFactory);
	}

	public static TypeReference GetResolvedReturnType(this MethodReference methodReference, ITypeFactoryProvider provider)
	{
		return methodReference.GetResolvedReturnType(provider.TypeFactory);
	}

	public static TypeReference GetResolvedThisType(this MethodReference methodReference, ITypeFactoryProvider provider)
	{
		return methodReference.GetResolvedThisType(provider.TypeFactory);
	}

	public static ReadOnlyCollection<InflatedFieldType> GetInflatedFieldTypes(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return typeReference.GetInflatedFieldTypes(provider.TypeFactory);
	}

	public static ReadOnlyCollection<TypeReference> GetInterfaces(this TypeReference type, ITypeFactoryProvider provider)
	{
		return type.GetInterfaceTypes(provider.TypeFactory);
	}

	public static GenericInstanceType CreateGenericInstanceType(this TypeReference typeReference, ITypeFactoryProvider provider, params TypeReference[] genericArguments)
	{
		return provider.TypeFactory.CreateGenericInstanceType(typeReference.Resolve(), typeReference.DeclaringType, genericArguments);
	}

	public static TypeReference CreatePointerType(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return provider.TypeFactory.CreatePointerType(typeReference);
	}

	public static TypeReference CreateByReferenceType(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return provider.TypeFactory.CreateByReferenceType(typeReference);
	}

	public static TypeReference CreateArrayType(this TypeReference typeReference, ITypeFactoryProvider provider)
	{
		return provider.TypeFactory.CreateArrayType(typeReference);
	}

	public static int SumOfMethodCodeSize(this TypeDefinition type)
	{
		return type.Methods.Sum((MethodDefinition m) => m.HasBody ? m.Body.CodeSize : 0);
	}

	public static int SumOfMethodCodeSize(this TypeReference type)
	{
		return type.Resolve()?.SumOfMethodCodeSize() ?? 0;
	}

	public static bool IsReferenceOrContainsReferenceTypeFields(this TypeReference type, ITypeFactoryProvider typeFactoryProvider)
	{
		type = type.WithoutModifiers();
		if (type.IsPrimitive)
		{
			return false;
		}
		if (type.ContainsGenericParameter)
		{
			throw new InvalidOperationException($"Cannot call {"IsReferenceOrContainsReferenceTypeFields"} on a type with generic parameters {type}");
		}
		if (type.IsIntegralType)
		{
			return false;
		}
		if (type.IsPointer || type.IsFunctionPointer)
		{
			return false;
		}
		if (!type.IsValueType)
		{
			return true;
		}
		foreach (InflatedFieldType inflatedField in type.GetInflatedFieldTypes(typeFactoryProvider.TypeFactory))
		{
			if (!inflatedField.Field.IsStatic && inflatedField.InflatedType.IsReferenceOrContainsReferenceTypeFields(typeFactoryProvider))
			{
				return true;
			}
		}
		return false;
	}

	public static FieldDefinition FindFieldDefinition(this TypeDefinition type, string name)
	{
		while (type != null)
		{
			ReadOnlyCollection<FieldDefinition> fields = type.Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				if (fields[i].Name.Equals(name, StringComparison.Ordinal))
				{
					return fields[i];
				}
			}
			type = type.BaseType.Resolve();
		}
		return null;
	}

	public static PropertyDefinition FindPropertyDefinition(this TypeDefinition type, string name)
	{
		while (type != null)
		{
			ReadOnlyCollection<PropertyDefinition> properties = type.Properties;
			for (int i = 0; i < properties.Count; i++)
			{
				if (properties[i].Name.Equals(name, StringComparison.Ordinal))
				{
					return properties[i];
				}
			}
			type = type.BaseType.Resolve();
		}
		return null;
	}
}
