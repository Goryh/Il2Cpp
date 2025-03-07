using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class ConstructionTimeTypeFactory : BaseTypeFactory
{
	private readonly UnderConstructionTypeReferenceRepository _typeReferenceRepository;

	private readonly UnderConstructionMethodReferenceRepository _methodReferenceRepository;

	private readonly UnderConstructionFieldReferenceRepository _fieldReferenceRepository;

	public override bool IsReadOnly => false;

	public ConstructionTimeTypeFactory(TypeContext typeContext, UnderConstructionMemberRepositories repositories)
		: base(typeContext)
	{
		_typeReferenceRepository = repositories.Types;
		_methodReferenceRepository = repositories.Methods;
		_fieldReferenceRepository = repositories.Fields;
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, params TypeReference[] genericArguments)
	{
		return _typeReferenceRepository.ResolveGenericInst(typeDefinition, declaringType, genericArguments);
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments)
	{
		return _typeReferenceRepository.ResolveGenericInst(typeDefinition, declaringType, genericArguments);
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, params TypeReference[] methodGenericArguments)
	{
		return _methodReferenceRepository.CreateGenericInstanceMethod(declaringType, methodDefinition, methodGenericArguments);
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		return _methodReferenceRepository.CreateGenericInstanceMethod(declaringType, methodDefinition, methodGenericArguments);
	}

	public override MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition methodDefinition)
	{
		return _methodReferenceRepository.CreateMethodReferenceOnGenericInstance(declaringType, methodDefinition);
	}

	public override SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType declaringType, SystemImplementedArrayMethod arrayMethod)
	{
		return _methodReferenceRepository.CreateSystemImplementedArrayMethod(declaringType, arrayMethod);
	}

	public override FieldReference CreateFieldReference(GenericInstanceType declaringType, FieldReference fieldReference)
	{
		return _fieldReferenceRepository.ResolveGenericInst(declaringType, fieldReference);
	}

	public override ArrayType CreateArrayType(TypeReference elementType, int rank, bool isVector)
	{
		return _typeReferenceRepository.ResolveArray(elementType, rank, isVector);
	}

	public override PointerType CreatePointerType(TypeReference elementType)
	{
		return _typeReferenceRepository.ResolvePointerType(elementType);
	}

	public override ByReferenceType CreateByReferenceType(TypeReference elementType)
	{
		return _typeReferenceRepository.ResolveByRefType(elementType);
	}

	public override PinnedType CreatePinnedType(TypeReference elementType)
	{
		return _typeReferenceRepository.ResolvePinnedType(elementType);
	}

	public override FunctionPointerType CreateFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis)
	{
		return _typeReferenceRepository.ResolveFunctionPointerType(returnType, parameters, callingConvention, hasThis, explicitThis);
	}

	public override OptionalModifierType CreateOptionalModifierType(TypeReference elementType, TypeReference modifierType)
	{
		return _typeReferenceRepository.ResolveOptionalModifierType(elementType, modifierType);
	}

	public override RequiredModifierType CreateRequiredModifierType(TypeReference elementType, TypeReference modifierType)
	{
		return _typeReferenceRepository.ResolveRequiredModifierType(elementType, modifierType);
	}

	public override SentinelType CreateSentinelType(TypeReference elementType)
	{
		throw new NotImplementedException();
	}
}
