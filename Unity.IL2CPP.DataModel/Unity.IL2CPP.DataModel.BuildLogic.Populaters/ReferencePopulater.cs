using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic.Populaters;

internal static class ReferencePopulater
{
	public static void Populate(TypeContext context, ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes)
	{
		foreach (UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference> allNonDefinitionType in allNonDefinitionTypes)
		{
			GenericParameterProviderPopulater.InitializeEmpty(allNonDefinitionType.Ours);
		}
	}

	public static void Populate(TypeContext context, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		foreach (UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference> allNonDefinitionMethod in allNonDefinitionMethods)
		{
			GenericParameterProviderPopulater.InitializeMethodReference(context, allNonDefinitionMethod.Ours);
		}
	}

	public static void PopulateStage2(ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes)
	{
		foreach (UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference> allNonDefinitionType in allNonDefinitionTypes)
		{
			PopulateTypeRefProperties(allNonDefinitionType.Ours);
		}
	}

	public static void PopulateStage2(IEnumerable<MethodDefinition> allDefinitions)
	{
		foreach (MethodDefinition allDefinition in allDefinitions)
		{
			PopulateMethodRefProperties(allDefinition);
		}
	}

	public static void PopulateStage2(ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods)
	{
		foreach (UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference> allNonDefinitionMethod in allNonDefinitionMethods)
		{
			PopulateMethodRefProperties(allNonDefinitionMethod.Ours);
		}
	}

	public static void PopulateStage2(IEnumerable<FieldDefinition> allDefinitions)
	{
		foreach (FieldDefinition allDefinition in allDefinitions)
		{
			PopulateFieldDefinitionProperties(allDefinition);
		}
	}

	public static void PopulateStage2(ReadOnlyCollection<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> allNonDefinitionFields)
	{
		foreach (UnderConstruction<FieldReference, Mono.Cecil.FieldReference> allNonDefinitionField in allNonDefinitionFields)
		{
			PopulateFieldRefProperties(allNonDefinitionField.Ours);
		}
	}

	public static void PopulateFieldDefinitionProperties(FieldDefinition field)
	{
		field.InitializeProperties(IsVolatile(field));
		field.InitializeFieldDefinitionProperties(IsThreadStatic(field), field.IsNormalStatic(), GetFieldIndex(field));
	}

	public static void PopulateTypeRefProperties(TypeReference ours)
	{
		ours.InitializeTypeRefProperties(HasActivationFactories(ours), IsStringBuilder(ours));
	}

	public static void PopulateMethodRefProperties(MethodReference ours)
	{
		ours.InitializeMethodRefProperties(IsFinalizerMethod(ours));
	}

	public static void PopulateFieldRefProperties(FieldReference ours)
	{
		if (ours is FieldInst fieldInst)
		{
			fieldInst.InitializeProperties(IsVolatile(fieldInst));
			return;
		}
		throw new ArgumentException($"Unhandled {typeof(FieldReference)} type {ours.GetType()}");
	}

	internal static bool HasGenericParameterInGenericArguments(IGenericInstance genericInst)
	{
		ReadOnlyCollection<TypeReference> arguments = genericInst.GenericArguments;
		for (int i = 0; i < arguments.Count; i++)
		{
			if (arguments[i].ContainsGenericParameter)
			{
				return true;
			}
		}
		return false;
	}

	internal static bool ContainsFullGenericSharingTypes(IGenericInstance genericInst)
	{
		ReadOnlyCollection<TypeReference> arguments = genericInst.GenericArguments;
		for (int i = 0; i < arguments.Count; i++)
		{
			if (arguments[i].ContainsFullySharedGenericTypes)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsFinalizerMethod(MethodReference method)
	{
		if (method.Name == "Finalize" && method.ReturnType.MetadataType == MetadataType.Void && !method.HasParameters)
		{
			return (method.Attributes & MethodAttributes.Family) != 0;
		}
		return false;
	}

	private static bool IsThreadStatic(FieldDefinition fieldReference)
	{
		if (fieldReference.IsStatic && fieldReference.HasCustomAttributes)
		{
			return fieldReference.CustomAttributes.Any((CustomAttribute ca) => ca.AttributeType.Name == "ThreadStaticAttribute");
		}
		return false;
	}

	private static bool IsNormalStatic(this FieldDefinition field)
	{
		if (field.IsLiteral)
		{
			return false;
		}
		if (!field.IsStatic)
		{
			return false;
		}
		if (!field.HasCustomAttributes)
		{
			return true;
		}
		return field.CustomAttributes.All((CustomAttribute ca) => ca.AttributeType.Name != "ThreadStaticAttribute");
	}

	private static int GetFieldIndex(FieldReference field)
	{
		FieldDefinition def = field.FieldDef;
		ReadOnlyCollection<FieldDefinition> fields = def.DeclaringType.Fields;
		for (int i = 0; i < fields.Count; i++)
		{
			if (def == fields[i])
			{
				return i;
			}
		}
		throw new InvalidOperationException($"Field {field.Name} was not found on its definition {def.DeclaringType}!");
	}

	private static bool IsVolatile(FieldReference fieldReference)
	{
		if (fieldReference.FieldType.IsRequiredModifier && ((RequiredModifierType)fieldReference.FieldType).ModifierType.Name.Contains("IsVolatile"))
		{
			return true;
		}
		return false;
	}

	private static bool HasActivationFactories(TypeReference type)
	{
		if (!(type is TypeDefinition typeDef))
		{
			return false;
		}
		if (!typeDef.IsWindowsRuntime || typeDef.IsValueType)
		{
			return false;
		}
		return typeDef.CustomAttributes.Any((CustomAttribute ca) => (ca.AttributeType.Name == "ActivatableAttribute" && ca.AttributeType.Namespace == "Windows.Foundation.Metadata") || (ca.AttributeType.Name == "StaticAttribute" && ca.AttributeType.Namespace == "Windows.Foundation.Metadata") || (ca.AttributeType.Name == "ComposableAttribute" && ca.AttributeType.Namespace == "Windows.Foundation.Metadata"));
	}

	internal static bool IsStringBuilder(TypeReference type)
	{
		if (type.MetadataType == MetadataType.Class)
		{
			return type == type.Context.GetSystemType(SystemType.StringBuilder);
		}
		return false;
	}
}
