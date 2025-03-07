using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Populaters;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.BuildLogic.Inflation;

internal static class TypeInflationHelpers
{
	public static bool ContainsGenericParameters(GenericInstanceType genericInstanceType)
	{
		if (!ReferencePopulater.HasGenericParameterInGenericArguments(genericInstanceType))
		{
			return genericInstanceType.ElementType.ContainsGenericParameter;
		}
		return true;
	}

	public static bool ContainsFullGenericSharedTypes(GenericInstanceType genericInstanceType)
	{
		return ReferencePopulater.ContainsFullGenericSharingTypes(genericInstanceType);
	}

	public static TypeReference UnderlyingType(TypeReference type)
	{
		if (type is TypeSpecification { ElementType: var elementType } typeSpecification)
		{
			if (elementType == null)
			{
				return typeSpecification;
			}
			return elementType.UnderlyingType();
		}
		return type;
	}

	public static TypeReference WithoutModifiers(TypeReference type)
	{
		TypeReference currentReference = type;
		while (currentReference != null)
		{
			if (currentReference is PinnedType pinnedType)
			{
				currentReference = pinnedType.ElementType;
				continue;
			}
			if (currentReference is RequiredModifierType requiredModifierType)
			{
				currentReference = requiredModifierType.ElementType;
				continue;
			}
			if (currentReference is OptionalModifierType optionalModifierType)
			{
				currentReference = optionalModifierType.ElementType;
				continue;
			}
			return currentReference;
		}
		throw new Exception();
	}

	public static TypeReference GetBaseType(TypeReference type, ITypeFactory typeFactory)
	{
		if (type is TypeSpecification)
		{
			if (type.IsArray)
			{
				return type.Context.GetSystemType(SystemType.Array) ?? type.Module.TypeSystem.Object;
			}
			if (type.IsGenericParameter || type.IsByReference || type.IsPointer || type.IsFunctionPointer)
			{
				return null;
			}
			if (type is SentinelType sentinelType)
			{
				return sentinelType.ElementType.GetBaseType(typeFactory);
			}
			if (type is PinnedType pinnedType)
			{
				return pinnedType.ElementType.GetBaseType(typeFactory);
			}
			if (type is RequiredModifierType requiredModifierType)
			{
				return requiredModifierType.ElementType.GetBaseType(typeFactory);
			}
			if (type is OptionalModifierType optionalModifierType)
			{
				return optionalModifierType.ElementType.GetBaseType(typeFactory);
			}
		}
		if (type is GenericParameter)
		{
			throw new NotSupportedException();
		}
		return typeFactory.ResolverFor(type).Resolve(type.Resolve().BaseType);
	}

	public static ReadOnlyCollection<MethodReference> GetMethods(TypeReference type, ITypeFactory typeFactory)
	{
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition == null)
		{
			return ReadOnlyCollectionCache<MethodReference>.Empty;
		}
		ReadOnlyCollection<MethodDefinition> definitionMethods = typeDefinition.Methods;
		if (definitionMethods.Count == 0)
		{
			return ReadOnlyCollectionCache<MethodReference>.Empty;
		}
		TypeResolver typeResolver = typeFactory.ResolverFor(type);
		MethodReference[] methods = new MethodReference[typeDefinition.Methods.Count];
		for (int i = 0; i < definitionMethods.Count; i++)
		{
			methods[i] = typeResolver.Resolve(definitionMethods[i]);
		}
		return methods.AsReadOnly();
	}

	public static ReadOnlyCollection<TypeReference> GetInterfaces(TypeReference type, ITypeFactory typeFactory)
	{
		if (type.IsGenericParameter || type.IsFunctionPointer)
		{
			return ReadOnlyCollectionCache<TypeReference>.Empty;
		}
		if (type.IsArray)
		{
			return ReadOnlyCollectionCache<TypeReference>.Empty;
		}
		HashSet<TypeReference> interfaces = new HashSet<TypeReference>();
		AddInterfacesRecursive(type.Context, type, type.Resolve(), typeFactory, interfaces);
		return interfaces.ToList().AsReadOnly();
	}

	private static void AddInterfacesRecursive(TypeContext context, TypeReference type, TypeDefinition concreteType, ITypeFactory typeFactory, HashSet<TypeReference> interfaces)
	{
		if (type.IsArray || type.IsGenericParameter || type.IsFunctionPointer)
		{
			return;
		}
		TypeResolver typeResolver = typeFactory.ResolverFor(type);
		TypeDefinition definition = type.Resolve();
		if (definition == null)
		{
			return;
		}
		foreach (InterfaceImplementation @interface in definition.Interfaces)
		{
			TypeReference resolvedInterface = @interface.ResolveInterfaceImplementation(concreteType, typeResolver);
			if (!interfaces.Add(resolvedInterface))
			{
				continue;
			}
			if (!(resolvedInterface is GenericInstanceType genericInstanceTypeInterface))
			{
				if (!(resolvedInterface is TypeDefinition typeDefinitionInterface))
				{
					throw new ArgumentException($"Unhandled interface type {resolvedInterface.GetType()}.  {resolvedInterface}");
				}
				if (!context.WindowsRuntimeAssembliesLoaded)
				{
					foreach (TypeReference ifaceType in typeDefinitionInterface.GetInterfaceTypes(typeFactory))
					{
						interfaces.Add(ifaceType);
					}
				}
				else
				{
					AddInterfacesRecursive(context, typeDefinitionInterface, concreteType, typeFactory, interfaces);
				}
			}
			else
			{
				AddInterfacesRecursive(context, genericInstanceTypeInterface, concreteType, typeFactory, interfaces);
			}
		}
	}

	public static ReadOnlyCollection<InflatedFieldType> GetFieldTypes(TypeReference type, ITypeFactory typeFactory)
	{
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition == null)
		{
			return ReadOnlyCollectionCache<InflatedFieldType>.Empty;
		}
		ReadOnlyCollection<FieldDefinition> definitionFields = typeDefinition.Fields;
		if (definitionFields.Count == 0)
		{
			return ReadOnlyCollectionCache<InflatedFieldType>.Empty;
		}
		InflatedFieldType[] fieldTypes = new InflatedFieldType[definitionFields.Count];
		for (int i = 0; i < definitionFields.Count; i++)
		{
			fieldTypes[i] = new InflatedFieldType(definitionFields[i], typeFactory.ResolverFor(type).ResolveFieldType(definitionFields[i]));
		}
		return fieldTypes.AsReadOnly();
	}

	public static bool IsComOrWindowsRuntimeInterface(TypeReference type, ITypeFactory typeFactory)
	{
		return IsComOrWindowsRuntimeType(type, typeFactory, (TypeDefinition typeDef) => typeDef.IsInterface && typeDef.IsComOrWindowsRuntimeType());
	}

	private static bool IsComOrWindowsRuntimeType(TypeReference type, ITypeFactory typeFactory, Func<TypeDefinition, bool> predicate)
	{
		if (IsArrayOrGenericParameter(type))
		{
			return false;
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition == null)
		{
			return false;
		}
		if (!predicate(typeDefinition))
		{
			return false;
		}
		if (type is GenericInstanceType genericInstance)
		{
			return AreGenericArgumentsValidForWindowsRuntimeType(genericInstance, typeFactory);
		}
		return true;
	}

	private static bool IsArrayOrGenericParameter(TypeReference typeReference)
	{
		while (typeReference is TypeSpecification typeSpecification)
		{
			if (typeSpecification.IsArray)
			{
				return true;
			}
			if (typeSpecification.IsGenericParameter)
			{
				return true;
			}
			typeReference = typeSpecification.ElementType;
		}
		return false;
	}

	private static bool AreGenericArgumentsValidForWindowsRuntimeType(GenericInstanceType genericInstance, ITypeFactory typeFactory)
	{
		foreach (TypeReference genericArgument in genericInstance.GenericArguments)
		{
			if (!IsValidForWindowsRuntimeType(genericArgument, typeFactory))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsValidForWindowsRuntimeType(TypeReference type, ITypeFactory typeFactory)
	{
		if (type.IsWindowsRuntimePrimitiveType())
		{
			return true;
		}
		if (type.IsAttribute)
		{
			return false;
		}
		if (type.IsGenericInstance)
		{
			GenericInstanceType genericInstanceType = (GenericInstanceType)type.Context.WindowsRuntimeProjections.ProjectToWindowsRuntime(type, typeFactory);
			if (!IsComOrWindowsRuntimeType(genericInstanceType, typeFactory, (TypeDefinition typeDef) => typeDef.IsExposedToWindowsRuntime() && (typeDef.IsInterface || typeDef.IsDelegate)))
			{
				return false;
			}
			return AreGenericArgumentsValidForWindowsRuntimeType(genericInstanceType, typeFactory);
		}
		if (type.IsGenericParameter || type is TypeSpecification)
		{
			return false;
		}
		return type.Context.WindowsRuntimeProjections.ProjectToWindowsRuntime(type.Resolve()).IsExposedToWindowsRuntime();
	}
}
