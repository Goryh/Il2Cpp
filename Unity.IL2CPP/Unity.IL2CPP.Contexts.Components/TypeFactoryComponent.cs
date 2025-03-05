using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Stats;

namespace Unity.IL2CPP.Contexts.Components;

public class TypeFactoryComponent : ServiceComponentBase<IDataModelService, TypeFactoryComponent>, IDataModelService, ITypeFactory
{
	private ITypeFactory _cachingFactory;

	private TypeContext _typeContext;

	public bool IsReadOnly => _cachingFactory.IsReadOnly;

	public TypeFactoryComponent()
	{
	}

	private TypeFactoryComponent(TypeContext typeContext)
	{
		Initialize(typeContext);
	}

	public void Initialize(TypeContext typeContext)
	{
		_typeContext = typeContext;
		ITypeFactory globalFactory = typeContext.CreateThreadSafeFactoryForFullConstruction();
		if (globalFactory.IsReadOnly)
		{
			_cachingFactory = globalFactory;
		}
		else
		{
			_cachingFactory = globalFactory.CreateCached(_typeContext);
		}
	}

	public IDisposable BeginStats(string name)
	{
		return StatisticsSection.Begin(_typeContext, name);
	}

	protected override TypeFactoryComponent ThisAsFull()
	{
		return this;
	}

	protected override IDataModelService ThisAsRead()
	{
		return this;
	}

	protected override void ResetPooledInstanceStateIfNecessary()
	{
	}

	protected override void SyncPooledInstanceWithParent(TypeFactoryComponent parent)
	{
	}

	protected override TypeFactoryComponent CreateEmptyInstance()
	{
		return new TypeFactoryComponent(_typeContext);
	}

	protected override TypeFactoryComponent CreatePooledInstance()
	{
		return new TypeFactoryComponent(_typeContext);
	}

	protected override void ForkForPrimaryWrite(in ForkingData data, out object writer, out IDataModelService reader, out TypeFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForPrimaryCollection(in ForkingData data, out object writer, out IDataModelService reader, out TypeFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForSecondaryCollection(in ForkingData data, out object writer, out IDataModelService reader, out TypeFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Pooled);
	}

	protected override void ForkForSecondaryWrite(in ForkingData data, out object writer, out IDataModelService reader, out TypeFactoryComponent full)
	{
		ReadOnlyFork(in data, out writer, out reader, out full, ForkMode.Empty);
	}

	public GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, params TypeReference[] genericArguments)
	{
		return _cachingFactory.CreateGenericInstanceType(typeDefinition, declaringType, genericArguments);
	}

	public GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments)
	{
		return _cachingFactory.CreateGenericInstanceType(typeDefinition, declaringType, genericArguments);
	}

	public GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, params TypeReference[] methodGenericArguments)
	{
		return _cachingFactory.CreateGenericInstanceMethod(declaringType, methodDefinition, methodGenericArguments);
	}

	public GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		return _cachingFactory.CreateGenericInstanceMethod(declaringType, methodDefinition, methodGenericArguments);
	}

	public MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition methodDefinition)
	{
		return _cachingFactory.CreateMethodReferenceOnGenericInstance(declaringType, methodDefinition);
	}

	public SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType declaringType, SystemImplementedArrayMethod arrayMethod)
	{
		return _cachingFactory.CreateSystemImplementedArrayMethod(declaringType, arrayMethod);
	}

	public FieldReference CreateFieldReference(GenericInstanceType declaringType, FieldReference fieldReference)
	{
		return _cachingFactory.CreateFieldReference(declaringType, fieldReference);
	}

	public ArrayType CreateArrayType(TypeReference elementType, int rank, bool isVector)
	{
		return _cachingFactory.CreateArrayType(elementType, rank, isVector);
	}

	public PointerType CreatePointerType(TypeReference elementType)
	{
		return _cachingFactory.CreatePointerType(elementType);
	}

	public ByReferenceType CreateByReferenceType(TypeReference elementType)
	{
		return _cachingFactory.CreateByReferenceType(elementType);
	}

	public PinnedType CreatePinnedType(TypeReference elementType)
	{
		return _cachingFactory.CreatePinnedType(elementType);
	}

	public FunctionPointerType CreateFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis)
	{
		return _cachingFactory.CreateFunctionPointerType(returnType, parameters, callingConvention, hasThis, explicitThis);
	}

	public OptionalModifierType CreateOptionalModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return _cachingFactory.CreateOptionalModifierType(modifierType, elementType);
	}

	public RequiredModifierType CreateRequiredModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return _cachingFactory.CreateRequiredModifierType(modifierType, elementType);
	}

	public SentinelType CreateSentinelType(TypeReference elementType)
	{
		return _cachingFactory.CreateSentinelType(elementType);
	}

	public TypeResolver ResolverFor(TypeReference typeReference)
	{
		return _cachingFactory.ResolverFor(typeReference);
	}

	public TypeResolver ResolverFor(TypeReference typeReference, MethodReference methodReference)
	{
		return _cachingFactory.ResolverFor(typeReference, methodReference);
	}

	public TypeResolver EmptyResolver()
	{
		return _cachingFactory.EmptyResolver();
	}
}
