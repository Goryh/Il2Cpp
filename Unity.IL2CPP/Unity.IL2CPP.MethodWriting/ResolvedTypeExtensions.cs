using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.MethodWriting;

public static class ResolvedTypeExtensions
{
	public static string ForField(this INamingService namingService, ResolvedFieldInfo fieldInfo)
	{
		return fieldInfo.FieldReference.CppName;
	}

	public static string ForVariable(this INamingService namingService, ResolvedTypeInfo type)
	{
		return type.ResolvedType.CppNameForVariable;
	}

	public static string ForPointerToVariable(this INamingService namingService, ResolvedTypeInfo variableType)
	{
		return variableType.ResolvedType.CppNameForPointerToVariable;
	}

	public static string ForReferenceToVariable(this INamingService namingService, ResolvedTypeInfo variableType)
	{
		return variableType.ResolvedType.CppNameForReferenceToVariable;
	}

	public static string ForTypeNameOnly(this INamingService namingService, ResolvedTypeInfo type)
	{
		return type.ResolvedType.CppName;
	}

	public static string ForVariableName(this INamingService namingService, ResolvedVariable variable)
	{
		return namingService.ForVariableName(variable.VariableReference);
	}

	public static string ForThreadFieldsStruct(this INamingService namingService, ReadOnlyContext context, ResolvedTypeInfo declaringType)
	{
		return namingService.ForThreadFieldsStruct(context, declaringType.ResolvedType);
	}

	public static string ForStaticFieldsStruct(this INamingService namingService, ReadOnlyContext context, ResolvedTypeInfo declaringType)
	{
		return namingService.ForStaticFieldsStruct(context, declaringType.ResolvedType);
	}

	public static string ForStaticFieldsStructStorage(this INamingService namingService, ReadOnlyContext context, ResolvedTypeInfo declaringType)
	{
		return namingService.ForStaticFieldsStructStorage(context, declaringType.ResolvedType);
	}

	public static string FieldInfo(this IRuntimeMetadataAccess metadataAccess, ResolvedFieldInfo fieldInfo)
	{
		return metadataAccess.FieldInfo(fieldInfo.FieldReference);
	}

	public static string FieldRvaData(this IRuntimeMetadataAccess metadataAccess, ResolvedFieldInfo fieldInfo)
	{
		return metadataAccess.FieldRvaData(fieldInfo.FieldReference, fieldInfo.DeclaringType.ResolvedType);
	}

	public static string TypeInfoFor(this IRuntimeMetadataAccess metadataAccess, ResolvedTypeInfo type)
	{
		return metadataAccess.TypeInfoFor(type.UnresolvedType);
	}

	public static string TypeInfoFor(this IRuntimeMetadataAccess metadataAccess, ResolvedTypeInfo type, IRuntimeMetadataAccess.TypeInfoForReason reason)
	{
		return metadataAccess.TypeInfoFor(type.UnresolvedType, reason);
	}

	public static string Il2CppTypeFor(this IRuntimeMetadataAccess metadataAccess, ResolvedTypeInfo type)
	{
		return metadataAccess.Il2CppTypeFor(type.UnresolvedType);
	}

	public static string StaticData(this IRuntimeMetadataAccess metadataAccess, ResolvedTypeInfo type)
	{
		return metadataAccess.StaticData(type.UnresolvedType);
	}

	public static string HiddenMethodInfo(this IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo methodInfo)
	{
		return metadataAccess.HiddenMethodInfo(methodInfo.UnresovledMethodReference);
	}

	public static string Method(this IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo methodInfo)
	{
		return metadataAccess.Method(methodInfo.UnresovledMethodReference);
	}

	public static string MethodInfo(this IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo methodInfo)
	{
		return metadataAccess.MethodInfo(methodInfo.UnresovledMethodReference);
	}

	public static string Newobj(this IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo methodInfo)
	{
		return metadataAccess.Newobj(methodInfo.UnresovledMethodReference);
	}

	public static IMethodMetadataAccess MethodMetadataFor(this IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo methodInfo)
	{
		return metadataAccess.MethodMetadataFor(methodInfo.UnresovledMethodReference);
	}

	public static void AddIncludeForTypeDefinition(this IGeneratedMethodCodeWriter writer, ResolvedTypeInfo type)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, type.ResolvedType);
	}

	public static void AddIncludeForMethodDeclaration(this IGeneratedMethodCodeWriter writer, ResolvedMethodInfo method)
	{
		writer.AddIncludeForMethodDeclaration(method.ResolvedMethodReference);
	}

	public static RuntimeFieldLayoutKind RuntimeLayoutForFieldAccess(this ResolvedFieldInfo field, ReadOnlyContext context)
	{
		if (field.IsThreadStatic)
		{
			return field.DeclaringType.ResolvedType.GetThreadStaticRuntimeFieldLayout(context);
		}
		if (field.IsNormalStatic)
		{
			return field.DeclaringType.ResolvedType.GetRuntimeStaticFieldLayout(context);
		}
		return field.DeclaringType.GetRuntimeFieldLayout(context);
	}
}
