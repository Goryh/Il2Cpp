using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel.Creation;

internal abstract class BaseTypeFactory : ITypeFactory
{
	private readonly TypeResolver _emptyTypeResolver;

	protected readonly TypeContext _typeContext;

	public abstract bool IsReadOnly { get; }

	protected BaseTypeFactory(TypeContext typeContext)
	{
		_typeContext = typeContext;
		_emptyTypeResolver = new TypeResolver(null, null, _typeContext, this);
	}

	public abstract GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, params TypeReference[] genericArguments);

	public abstract GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments);

	public abstract GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, params TypeReference[] methodGenericArguments);

	public abstract GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments);

	public abstract MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition methodDefinition);

	public abstract SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType declaringType, SystemImplementedArrayMethod arrayMethod);

	public abstract FieldReference CreateFieldReference(GenericInstanceType declaringType, FieldReference fieldReference);

	public abstract ArrayType CreateArrayType(TypeReference elementType, int rank, bool isVector);

	public abstract PointerType CreatePointerType(TypeReference elementType);

	public abstract ByReferenceType CreateByReferenceType(TypeReference elementType);

	public abstract PinnedType CreatePinnedType(TypeReference elementType);

	public abstract FunctionPointerType CreateFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis);

	public abstract OptionalModifierType CreateOptionalModifierType(TypeReference modifierType, TypeReference elementType);

	public abstract RequiredModifierType CreateRequiredModifierType(TypeReference modifierType, TypeReference elementType);

	public abstract SentinelType CreateSentinelType(TypeReference elementType);

	public TypeResolver ResolverFor(TypeReference typeReference)
	{
		if (typeReference is GenericInstanceType genericInstanceType)
		{
			return new TypeResolver(genericInstanceType, null, _typeContext, this);
		}
		return EmptyResolver();
	}

	public TypeResolver ResolverFor(TypeReference typeReference, MethodReference methodReference)
	{
		GenericInstanceType genericInstanceType = typeReference as GenericInstanceType;
		GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
		if (genericInstanceType != null || genericInstanceMethod != null)
		{
			return new TypeResolver(genericInstanceType, genericInstanceMethod, _typeContext, this);
		}
		return EmptyResolver();
	}

	public TypeResolver EmptyResolver()
	{
		return _emptyTypeResolver;
	}
}
