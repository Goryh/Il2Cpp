using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.Creation;

public interface ITypeFactory
{
	bool IsReadOnly { get; }

	GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, params TypeReference[] genericArguments);

	GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments);

	GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, params TypeReference[] methodGenericArguments);

	GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments);

	MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition methodDefinition);

	SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType declaringType, SystemImplementedArrayMethod arrayMethod);

	FieldReference CreateFieldReference(GenericInstanceType declaringType, FieldReference fieldReference);

	ArrayType CreateArrayType(TypeReference elementType, int rank, bool isVector);

	PointerType CreatePointerType(TypeReference elementType);

	ByReferenceType CreateByReferenceType(TypeReference elementType);

	PinnedType CreatePinnedType(TypeReference elementType);

	FunctionPointerType CreateFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis);

	OptionalModifierType CreateOptionalModifierType(TypeReference modifierType, TypeReference elementType);

	RequiredModifierType CreateRequiredModifierType(TypeReference modifierType, TypeReference elementType);

	SentinelType CreateSentinelType(TypeReference elementType);

	TypeResolver ResolverFor(TypeReference typeReference);

	TypeResolver ResolverFor(TypeReference typeReference, MethodReference methodReference);

	TypeResolver EmptyResolver();
}
