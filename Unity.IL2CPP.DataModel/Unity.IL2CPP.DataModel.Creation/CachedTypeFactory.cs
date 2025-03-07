using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;

namespace Unity.IL2CPP.DataModel.Creation;

internal class CachedTypeFactory : BaseTypeFactory, IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>, IMemberStoreCreateCallbacks<TypeReference, PointerType>, IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>, IMemberStoreCreateCallbacks<TypeReference, PinnedType>, IMemberStoreCreateCallbacks<TypeReference, ArrayType>, IMemberStoreCreateCallbacks<ArrayKey, ArrayType>, IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>, IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>, IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>, IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>, IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>, IMemberStoreCreateCallbacks<FieldKey, FieldInst>, IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>
{
	private class CachingMemberStorageStrategy : IMemberStorageStrategy, IDisposable
	{
		private readonly TypeContext _typeContext;

		public bool IsReadOnly => false;

		public CachingMemberStorageStrategy(TypeContext typeContext)
		{
			_typeContext = typeContext;
		}

		public TDataModel GetOrAdd<TArg, TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TArg arg, TKey elementType, IMemberStoreCreateCallbacks<TArg, TKey, TDataModel> callbacks)
		{
			if (!mapping.TryGetValue(elementType, out var dataModel))
			{
				dataModel = callbacks.Create(arg, elementType);
				mapping.Add(elementType, dataModel);
				return dataModel;
			}
			return dataModel;
		}

		public TDataModel GetOrAdd<TKey, TDataModel>(Dictionary<TKey, TDataModel> mapping, TKey elementType, IMemberStoreCreateCallbacks<TKey, TDataModel> callbacks)
		{
			if (!mapping.TryGetValue(elementType, out var dataModel))
			{
				dataModel = callbacks.Create(elementType);
				mapping.Add(elementType, dataModel);
				return dataModel;
			}
			return dataModel;
		}

		public void Dispose()
		{
		}
	}

	private class CachedMemberStore : MemberStore
	{
		public CachedMemberStore(TypeContext typeContext)
			: base(new CachingMemberStorageStrategy(typeContext))
		{
		}
	}

	private readonly ITypeFactory _typeFactory;

	private readonly CachedMemberStore _memberStore;

	public override bool IsReadOnly => _typeFactory.IsReadOnly;

	public CachedTypeFactory(TypeContext typeContext, ITypeFactory typeFactory)
		: base(typeContext)
	{
		_typeFactory = typeFactory;
		_memberStore = new CachedMemberStore(typeContext);
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, params TypeReference[] genericArguments)
	{
		return CreateGenericInstanceType(typeDefinition, declaringType, genericArguments.AsReadOnly());
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments)
	{
		return _memberStore.GetOrAddGenericInstanceType(declaringType, new GenericInstanceTypeKey(typeDefinition, genericArguments), this);
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, params TypeReference[] methodGenericArguments)
	{
		return CreateGenericInstanceMethod(declaringType, methodDefinition, methodGenericArguments.AsReadOnly());
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		return _memberStore.GetOrAddGenericInstanceMethod(declaringType, new MethodInstKey(methodDefinition, (declaringType as GenericInstanceType)?.GenericArguments, methodGenericArguments), this);
	}

	public override MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition methodDefinition)
	{
		return _memberStore.GetOrAddMethodRefOnTypeInst(declaringType, new MethodInstKey(methodDefinition, declaringType.GenericArguments, null), this);
	}

	public override SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType declaringType, SystemImplementedArrayMethod arrayMethod)
	{
		return _memberStore.GetOrAddSystemImplementedArrayMethod(arrayMethod, new SystemImplementedArrayMethodKey(declaringType, arrayMethod.Name), this);
	}

	public override FieldReference CreateFieldReference(GenericInstanceType declaringType, FieldReference fieldReference)
	{
		return _memberStore.GetOrAddFieldReference(new FieldKey(fieldReference, declaringType), this);
	}

	public override ArrayType CreateArrayType(TypeReference elementType, int rank, bool isVector)
	{
		return _memberStore.GetOrAddArray(elementType, rank, isVector, this, this);
	}

	public override PointerType CreatePointerType(TypeReference elementType)
	{
		return _memberStore.GetOrAddPointerType(elementType, this);
	}

	public override ByReferenceType CreateByReferenceType(TypeReference elementType)
	{
		return _memberStore.GetOrAddByReferenceType(elementType, this);
	}

	public override PinnedType CreatePinnedType(TypeReference elementType)
	{
		return _memberStore.GetOrAddPinnedType(elementType, this);
	}

	public override FunctionPointerType CreateFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis)
	{
		return _memberStore.GetOrAddFunctionPointerType(new MethodSignatureKey(returnType, parameters, callingConvention, hasThis, explicitThis), this);
	}

	public override OptionalModifierType CreateOptionalModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return _memberStore.GetOrAddOptionalModiferType(new ModifierKey(modifierType, elementType), this);
	}

	public override RequiredModifierType CreateRequiredModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return _memberStore.GetOrAddRequiredModifierType(new ModifierKey(modifierType, elementType), this);
	}

	public override SentinelType CreateSentinelType(TypeReference elementType)
	{
		throw new NotImplementedException();
	}

	GenericInstanceType IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>.Create(TypeReference arg, GenericInstanceTypeKey key)
	{
		return _typeFactory.CreateGenericInstanceType(key.TypeDefinition, arg, key.GenericArguments);
	}

	PointerType IMemberStoreCreateCallbacks<TypeReference, PointerType>.Create(TypeReference key)
	{
		return _typeFactory.CreatePointerType(key);
	}

	ArrayType IMemberStoreCreateCallbacks<ArrayKey, ArrayType>.Create(ArrayKey key)
	{
		return _typeFactory.CreateArrayType(key.ElementType, key.Rank, isVector: false);
	}

	ByReferenceType IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>.Create(TypeReference key)
	{
		return _typeFactory.CreateByReferenceType(key);
	}

	PinnedType IMemberStoreCreateCallbacks<TypeReference, PinnedType>.Create(TypeReference key)
	{
		return _typeFactory.CreatePinnedType(key);
	}

	ArrayType IMemberStoreCreateCallbacks<TypeReference, ArrayType>.Create(TypeReference key)
	{
		return _typeFactory.CreateArrayType(key, 1, isVector: true);
	}

	OptionalModifierType IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>.Create(ModifierKey key)
	{
		return _typeFactory.CreateOptionalModifierType(key.ModifierType, key.ElementType);
	}

	RequiredModifierType IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>.Create(ModifierKey key)
	{
		return _typeFactory.CreateRequiredModifierType(key.ModifierType, key.ElementType);
	}

	FunctionPointerType IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>.Create(MethodSignatureKey key)
	{
		return _typeFactory.CreateFunctionPointerType(key.ReturnType, key.Parameters, key.CallingConvention, key.HasThis, key.ExplicitThis);
	}

	GenericInstanceMethod IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.Create(TypeReference arg, MethodInstKey key)
	{
		return _typeFactory.CreateGenericInstanceMethod(arg, key.MethodDef, key.MethodGenericArguments);
	}

	MethodRefOnTypeInst IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.Create(GenericInstanceType arg, MethodInstKey key)
	{
		return (MethodRefOnTypeInst)_typeFactory.CreateMethodReferenceOnGenericInstance(arg, key.MethodDef);
	}

	FieldInst IMemberStoreCreateCallbacks<FieldKey, FieldInst>.Create(FieldKey key)
	{
		return (FieldInst)_typeFactory.CreateFieldReference(key.DeclaringType, key.Field);
	}

	SystemImplementedArrayMethod IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.Create(SystemImplementedArrayMethod arg, SystemImplementedArrayMethodKey key)
	{
		return _typeFactory.CreateSystemImplementedArrayMethod(key.ArrayType, arg);
	}

	void IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>.OnCreate(TypeReference arg, GenericInstanceType created)
	{
	}

	void IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>.OnCreate(RequiredModifierType created)
	{
	}

	void IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>.OnCreate(OptionalModifierType created)
	{
	}

	void IMemberStoreCreateCallbacks<ArrayKey, ArrayType>.OnCreate(ArrayType created)
	{
	}

	void IMemberStoreCreateCallbacks<TypeReference, ArrayType>.OnCreate(ArrayType created)
	{
	}

	void IMemberStoreCreateCallbacks<TypeReference, PinnedType>.OnCreate(PinnedType created)
	{
	}

	void IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>.OnCreate(ByReferenceType created)
	{
	}

	void IMemberStoreCreateCallbacks<TypeReference, PointerType>.OnCreate(PointerType created)
	{
	}

	void IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>.OnCreate(FunctionPointerType created)
	{
	}

	void IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.OnCreate(TypeReference arg, GenericInstanceMethod created)
	{
	}

	void IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.OnCreate(GenericInstanceType arg, MethodRefOnTypeInst created)
	{
	}

	void IMemberStoreCreateCallbacks<FieldKey, FieldInst>.OnCreate(FieldInst created)
	{
	}

	void IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.OnCreate(SystemImplementedArrayMethod arg, SystemImplementedArrayMethod created)
	{
	}
}
