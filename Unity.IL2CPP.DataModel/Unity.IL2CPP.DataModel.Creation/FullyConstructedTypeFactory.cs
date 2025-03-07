using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;
using Unity.IL2CPP.DataModel.Stats;

namespace Unity.IL2CPP.DataModel.Creation;

internal class FullyConstructedTypeFactory : BaseTypeFactory, IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>, IMemberStoreCreateCallbacks<TypeReference, PointerType>, IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>, IMemberStoreCreateCallbacks<TypeReference, PinnedType>, IMemberStoreCreateCallbacks<TypeReference, ArrayType>, IMemberStoreCreateCallbacks<ArrayKey, ArrayType>, IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>, IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>, IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>, IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>, IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>, IMemberStoreCreateCallbacks<FieldKey, FieldInst>, IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>
{
	private readonly IMemberStore _memberStore;

	private readonly Statistics _statistics;

	public override bool IsReadOnly => _memberStore.IsReadOnly;

	private FullyConstructedTypeFactory(TypeContext typeContext)
		: base(typeContext)
	{
		_memberStore = new UnderConstructionMemberStore();
		_statistics = typeContext.Statistics;
	}

	internal FullyConstructedTypeFactory(TypeContext typeContext, IMemberStore memberStore)
		: base(typeContext)
	{
		_memberStore = memberStore;
		_statistics = typeContext.Statistics;
	}

	private void OnInitializeTypeReference(TypeReference newResult)
	{
		CreatedReferencePopulater.InitializeTypeReference(newResult, _typeContext, this);
	}

	private void OnInitializeMethodReference(MethodReference newResult)
	{
		CreatedReferencePopulater.InitializeMethodReference(_typeContext, newResult, newResult.Resolve());
	}

	private void OnInitializeFieldReference(FieldReference newResult)
	{
		CreatedReferencePopulater.InitializeFieldReference(newResult);
	}

	internal static ITypeFactory CreateEmpty(TypeContext context)
	{
		return new FullyConstructedTypeFactory(context);
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition genericTypeDef, TypeReference declaringType, params TypeReference[] genericArguments)
	{
		return CreateGenericInstanceType(genericTypeDef, declaringType, genericArguments.AsReadOnly());
	}

	public override GenericInstanceType CreateGenericInstanceType(TypeDefinition typeDefinition, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments)
	{
		return _memberStore.GetOrAddGenericInstanceType(declaringType, new GenericInstanceTypeKey(typeDefinition, genericArguments), this);
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition genericMethodDef, params TypeReference[] methodGenericArguments)
	{
		return CreateGenericInstanceMethod(declaringType, genericMethodDef, methodGenericArguments.AsReadOnly());
	}

	public override GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition methodDefinition, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		return _memberStore.GetOrAddGenericInstanceMethod(declaringType, new MethodInstKey(methodDefinition, (declaringType as GenericInstanceType)?.GenericArguments, methodGenericArguments), this);
	}

	public override SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType arrayType, SystemImplementedArrayMethod methodReference)
	{
		return _memberStore.GetOrAddSystemImplementedArrayMethod(methodReference, new SystemImplementedArrayMethodKey(arrayType, methodReference.Name), this);
	}

	public override MethodReference CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition genericMethodDef)
	{
		return _memberStore.GetOrAddMethodRefOnTypeInst(declaringType, new MethodInstKey(genericMethodDef, declaringType.GenericArguments, null), this);
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
		return new SentinelType(elementType, _typeContext);
	}

	GenericInstanceType IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>.Create(TypeReference arg, GenericInstanceTypeKey key)
	{
		return new GenericInstanceType(key.TypeDefinition, arg, new GenericInst(key.GenericArguments), _typeContext);
	}

	void IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.OnCreate(TypeReference arg, GenericInstanceMethod created)
	{
		OnInitializeMethodReference(created);
	}

	void IMemberStoreCreateCallbacks<TypeReference, GenericInstanceTypeKey, GenericInstanceType>.OnCreate(TypeReference arg, GenericInstanceType created)
	{
		OnInitializeTypeReference(created);
	}

	PointerType IMemberStoreCreateCallbacks<TypeReference, PointerType>.Create(TypeReference key)
	{
		return new PointerType(key, _typeContext);
	}

	void IMemberStoreCreateCallbacks<TypeReference, PointerType>.OnCreate(PointerType created)
	{
		OnInitializeTypeReference(created);
	}

	ByReferenceType IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>.Create(TypeReference key)
	{
		return new ByReferenceType(key, _typeContext);
	}

	ArrayType IMemberStoreCreateCallbacks<ArrayKey, ArrayType>.Create(ArrayKey key)
	{
		return new ArrayType(key.ElementType, key.Rank, isVector: false, _typeContext);
	}

	void IMemberStoreCreateCallbacks<ArrayKey, ArrayType>.OnCreate(ArrayType created)
	{
		OnInitializeTypeReference(created);
	}

	void IMemberStoreCreateCallbacks<TypeReference, ArrayType>.OnCreate(ArrayType created)
	{
		OnInitializeTypeReference(created);
	}

	void IMemberStoreCreateCallbacks<TypeReference, PinnedType>.OnCreate(PinnedType created)
	{
		OnInitializeTypeReference(created);
	}

	void IMemberStoreCreateCallbacks<TypeReference, ByReferenceType>.OnCreate(ByReferenceType created)
	{
		OnInitializeTypeReference(created);
	}

	PinnedType IMemberStoreCreateCallbacks<TypeReference, PinnedType>.Create(TypeReference key)
	{
		return new PinnedType(key, _typeContext);
	}

	ArrayType IMemberStoreCreateCallbacks<TypeReference, ArrayType>.Create(TypeReference key)
	{
		return new ArrayType(key, 1, isVector: true, _typeContext);
	}

	OptionalModifierType IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>.Create(ModifierKey key)
	{
		return new OptionalModifierType(key.ModifierType, key.ElementType, _typeContext);
	}

	void IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>.OnCreate(RequiredModifierType created)
	{
		OnInitializeTypeReference(created);
	}

	void IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType>.OnCreate(OptionalModifierType created)
	{
		OnInitializeTypeReference(created);
	}

	RequiredModifierType IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType>.Create(ModifierKey key)
	{
		return new RequiredModifierType(key.ModifierType, key.ElementType, _typeContext);
	}

	FunctionPointerType IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>.Create(MethodSignatureKey key)
	{
		return new FunctionPointerType(key.ReturnType, key.Parameters, key.CallingConvention, key.HasThis, key.ExplicitThis, _typeContext);
	}

	void IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType>.OnCreate(FunctionPointerType created)
	{
		OnInitializeTypeReference(created);
	}

	GenericInstanceMethod IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.Create(TypeReference arg, MethodInstKey key)
	{
		return new GenericInstanceMethod(arg, key.MethodDef, new GenericInst(key.MethodGenericArguments));
	}

	MethodRefOnTypeInst IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.Create(GenericInstanceType arg, MethodInstKey key)
	{
		return new MethodRefOnTypeInst(arg, key.MethodDef);
	}

	void IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.OnCreate(GenericInstanceType arg, MethodRefOnTypeInst created)
	{
		OnInitializeMethodReference(created);
	}

	FieldInst IMemberStoreCreateCallbacks<FieldKey, FieldInst>.Create(FieldKey key)
	{
		return new FieldInst(key.Field.FieldDef, key.DeclaringType);
	}

	void IMemberStoreCreateCallbacks<FieldKey, FieldInst>.OnCreate(FieldInst created)
	{
		OnInitializeFieldReference(created);
	}

	SystemImplementedArrayMethod IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.Create(SystemImplementedArrayMethod arg, SystemImplementedArrayMethodKey key)
	{
		return arg.Clone(key.ArrayType);
	}

	void IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.OnCreate(SystemImplementedArrayMethod arg, SystemImplementedArrayMethod created)
	{
		OnInitializeMethodReference(created);
	}
}
