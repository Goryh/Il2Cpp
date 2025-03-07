using Unity.IL2CPP.DataModel.BuildLogic.Populaters;

namespace Unity.IL2CPP.DataModel.Creation;

internal static class CreatedReferencePopulater
{
	public static void InitializeTypeReference(TypeReference typeReference, TypeContext typeContext, ITypeFactory typeFactory)
	{
		GenericParameterProviderPopulater.InitializeEmpty(typeReference);
		ReferencePopulater.PopulateTypeRefProperties(typeReference);
	}

	public static void InitializeFieldReference(FieldReference fieldReference)
	{
		ReferencePopulater.PopulateFieldRefProperties(fieldReference);
	}

	public static void InitializeMethodReference(TypeContext context, MethodReference methodReference, MethodDefinition methodDefinition)
	{
		if (methodDefinition != null)
		{
			GenericParameterProviderPopulater.InitializeMethodReference(context, methodReference);
			ReferencePopulater.PopulateMethodRefProperties(methodReference);
		}
	}
}
