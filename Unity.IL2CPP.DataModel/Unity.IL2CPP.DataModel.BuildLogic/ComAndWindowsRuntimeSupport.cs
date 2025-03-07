using System;
using System.Linq;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class ComAndWindowsRuntimeSupport
{
	public static void ProcessType(EditContext editContext, TypeDefinition type, ITypeFactory typeFactory)
	{
		if (!type.IsInterface && !type.IsValueType && !type.IsDelegate)
		{
			InjectBaseType(editContext, type);
			if (ShouldInjectFinalizeMethod(type))
			{
				InjectFinalizer(editContext, type, typeFactory);
			}
			if (ShouldInjectToStringMethod(type))
			{
				InjectToStringMethod(editContext, type, typeFactory);
			}
		}
	}

	private static void InjectBaseType(EditContext editContext, TypeDefinition type)
	{
		if (type.IsImport)
		{
			if (type.BaseType == null)
			{
				throw new InvalidOperationException($"COM import type '{type}' has no base type.");
			}
			if (type.BaseType.IsSystemObject)
			{
				((ITypeDefinitionUpdater)type).UpdateBaseType(editContext.Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppComObject));
			}
		}
	}

	private static bool ShouldInjectFinalizeMethod(TypeDefinition type)
	{
		if (!type.IsAttribute)
		{
			if (!type.IsIl2CppComObject && type != type.Context.GetIl2CppCustomType(Il2CppCustomType.Il2CppComDelegate))
			{
				return type.IsImport;
			}
			return true;
		}
		return false;
	}

	private static bool ShouldInjectToStringMethod(TypeDefinition type)
	{
		if (type.IsIl2CppComObject)
		{
			TypeDefinition stringableType = type.Context.GetSystemType(SystemType.IStringable);
			if (stringableType != null && stringableType.Methods.Any((MethodDefinition m) => m.Name == "ToString"))
			{
				return true;
			}
		}
		return false;
	}

	private static void InjectToStringMethod(EditContext editContext, TypeDefinition type, ITypeFactory typeFactory)
	{
		editContext.BuildMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual, editContext.Context.GetSystemType(SystemType.String)).WithMethodImplAttributes(MethodImplAttributes.CodeTypeMask).CompleteBuildStage(type, typeFactory);
	}

	private static void InjectFinalizer(EditContext editContext, TypeDefinition type, ITypeFactory typeFactory)
	{
		editContext.BuildMethod("Finalize", MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig).WithMethodImplAttributes(MethodImplAttributes.CodeTypeMask).CompleteBuildStage(type, typeFactory);
	}
}
