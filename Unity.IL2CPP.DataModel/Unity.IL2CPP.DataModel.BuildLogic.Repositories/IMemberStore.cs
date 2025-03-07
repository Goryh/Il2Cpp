using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal interface IMemberStore : IDisposable
{
	bool IsReadOnly { get; }

	PointerType GetOrAddPointerType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, PointerType> callbacks);

	PointerType GetOrAddPointerType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, PointerType> callbacks);

	ByReferenceType GetOrAddByReferenceType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, ByReferenceType> callbacks);

	ByReferenceType GetOrAddByReferenceType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, ByReferenceType> callbacks);

	PinnedType GetOrAddPinnedType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, PinnedType> callbacks);

	PinnedType GetOrAddPinnedType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, PinnedType> callbacks);

	ArrayType GetOrAddArray(TypeReference elementType, int rank, bool isVector, IMemberStoreCreateCallbacks<TypeReference, ArrayType> vectorCallbacks, IMemberStoreCreateCallbacks<ArrayKey, ArrayType> callbacks);

	ArrayType GetOrAddArray<TArg>(TArg arg, TypeReference elementType, int rank, bool isVector, IMemberStoreCreateCallbacks<TArg, TypeReference, ArrayType> vectorCallbacks, IMemberStoreCreateCallbacks<TArg, ArrayKey, ArrayType> callbacks);

	FunctionPointerType GetOrAddFunctionPointerType(MethodSignatureKey signatureKey, IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType> create);

	FunctionPointerType GetOrAddFunctionPointerType<TArg>(TArg arg, MethodSignatureKey signatureKey, IMemberStoreCreateCallbacks<TArg, MethodSignatureKey, FunctionPointerType> create);

	OptionalModifierType GetOrAddOptionalModiferType(ModifierKey modifierKey, IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType> callbacks);

	OptionalModifierType GetOrAddOptionalModiferType<TArg>(TArg arg, ModifierKey modifierKey, IMemberStoreCreateCallbacks<TArg, ModifierKey, OptionalModifierType> callbacks);

	RequiredModifierType GetOrAddRequiredModifierType(ModifierKey modifierKey, IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType> callbacks);

	RequiredModifierType GetOrAddRequiredModifierType<TArg>(TArg arg, ModifierKey modifierKey, IMemberStoreCreateCallbacks<TArg, ModifierKey, RequiredModifierType> callbacks);

	GenericInstanceType GetOrAddGenericInstanceType<TArg>(TArg arg, GenericInstanceTypeKey key, IMemberStoreCreateCallbacks<TArg, GenericInstanceTypeKey, GenericInstanceType> callbacks);

	FieldInst GetOrAddFieldReference(FieldKey key, IMemberStoreCreateCallbacks<FieldKey, FieldInst> callbacks);

	FieldInst GetOrAddFieldReference<TArg>(TArg arg, FieldKey key, IMemberStoreCreateCallbacks<TArg, FieldKey, FieldInst> callbacks);

	GenericInstanceMethod GetOrAddGenericInstanceMethod<TArg>(TArg arg, MethodInstKey methodInstKey, IMemberStoreCreateCallbacks<TArg, MethodInstKey, GenericInstanceMethod> callbacks);

	MethodRefOnTypeInst GetOrAddMethodRefOnTypeInst<TArg>(TArg arg, MethodInstKey methodInstKey, IMemberStoreCreateCallbacks<TArg, MethodInstKey, MethodRefOnTypeInst> callbacks);

	SystemImplementedArrayMethod GetOrAddSystemImplementedArrayMethod<TArg>(TArg arg, SystemImplementedArrayMethodKey arrayMethodKey, IMemberStoreCreateCallbacks<TArg, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod> callbacks);
}
