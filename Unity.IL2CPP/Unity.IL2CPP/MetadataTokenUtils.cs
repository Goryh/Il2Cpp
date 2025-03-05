using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public static class MetadataTokenUtils
{
	public static string FormattedMetadataTokenFor(FieldReference fieldRef)
	{
		return $"0x{MetadataTokenFor(fieldRef):X8} /* {fieldRef.FullName} */";
	}

	public static uint MetadataTokenFor(TypeReference typeReference)
	{
		return ResolvedTypeFor(typeReference).MetadataToken.ToUInt32();
	}

	public static uint MetadataTokenFor(TypeDefinition typeDefinition)
	{
		return typeDefinition.MetadataToken.ToUInt32();
	}

	public static uint MetadataTokenFor(MethodReference methodReference)
	{
		return ResolvedMethodFor(methodReference).MetadataToken.ToUInt32();
	}

	public static uint MetadataTokenFor(MethodDefinition methodDefinition)
	{
		return methodDefinition.MetadataToken.ToUInt32();
	}

	public static uint MetadataTokenFor(FieldReference fieldReference)
	{
		return fieldReference.FieldDef.MetadataToken.ToUInt32();
	}

	public static AssemblyDefinition AssemblyDefinitionFor(TypeReference typeReference)
	{
		return ResolvedTypeFor(typeReference).Module.Assembly;
	}

	public static AssemblyDefinition AssemblyDefinitionFor(TypeDefinition typeDefinition)
	{
		return typeDefinition.Module.Assembly;
	}

	public static AssemblyDefinition AssemblyDefinitionFor(MethodReference methodReference)
	{
		return ResolvedMethodFor(methodReference).Module.Assembly;
	}

	public static AssemblyDefinition AssemblyDefinitionFor(MethodDefinition methodDefinition)
	{
		return methodDefinition.Module.Assembly;
	}

	public static TypeReference ResolvedTypeFor(TypeReference typeReference)
	{
		TypeReference resolvedType = typeReference;
		if (!resolvedType.IsGenericInstance && !typeReference.IsArray)
		{
			resolvedType = typeReference.Resolve() ?? resolvedType;
		}
		return resolvedType;
	}

	private static MethodReference ResolvedMethodFor(MethodReference methodReference)
	{
		MethodReference resolvedMethod = methodReference;
		if (!methodReference.IsGenericInstance && !methodReference.DeclaringType.IsGenericInstance)
		{
			resolvedMethod = methodReference.Resolve() ?? resolvedMethod;
		}
		return resolvedMethod;
	}
}
