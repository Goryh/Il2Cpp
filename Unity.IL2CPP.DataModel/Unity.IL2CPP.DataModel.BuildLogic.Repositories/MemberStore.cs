using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal abstract class MemberStore : IMemberStore, IDisposable
{
	private readonly IMemberStorageStrategy _storageStrategy;

	private readonly Dictionary<TypeReference, ByReferenceType> _byRefTypeDefs;

	private readonly Dictionary<TypeReference, PointerType> _pointerTypeDefs;

	private readonly Dictionary<ModifierKey, RequiredModifierType> _reqModTypeDefs;

	private readonly Dictionary<ModifierKey, OptionalModifierType> _optModTypeDefs;

	private readonly Dictionary<TypeReference, PinnedType> _pinnedTypeDefs;

	private readonly Dictionary<TypeReference, ArrayType> _vectorTypeDefs;

	private readonly Dictionary<ArrayKey, ArrayType> _arrayTypeDefs;

	private readonly Dictionary<MethodSignatureKey, FunctionPointerType> _functionPointerTypes;

	private readonly Dictionary<GenericInstanceTypeKey, GenericInstanceType> _genericInstanceTypeDefs;

	private readonly Dictionary<FieldKey, FieldInst> _genericInstanceFields;

	private readonly Dictionary<MethodInstKey, GenericInstanceMethod> _genericInstanceMethods;

	private readonly Dictionary<MethodInstKey, MethodRefOnTypeInst> _methodRefOnGenericInstanceTypes;

	private readonly Dictionary<SystemImplementedArrayMethodKey, SystemImplementedArrayMethod> _systemImplementedArrayMethods;

	public bool IsReadOnly => _storageStrategy.IsReadOnly;

	protected MemberStore(IMemberStorageStrategy storageStrategy)
	{
		_storageStrategy = storageStrategy;
		_byRefTypeDefs = new Dictionary<TypeReference, ByReferenceType>();
		_pointerTypeDefs = new Dictionary<TypeReference, PointerType>();
		_reqModTypeDefs = new Dictionary<ModifierKey, RequiredModifierType>();
		_optModTypeDefs = new Dictionary<ModifierKey, OptionalModifierType>();
		_pinnedTypeDefs = new Dictionary<TypeReference, PinnedType>();
		_vectorTypeDefs = new Dictionary<TypeReference, ArrayType>();
		_arrayTypeDefs = new Dictionary<ArrayKey, ArrayType>();
		_functionPointerTypes = new Dictionary<MethodSignatureKey, FunctionPointerType>();
		_genericInstanceTypeDefs = new Dictionary<GenericInstanceTypeKey, GenericInstanceType>();
		_genericInstanceFields = new Dictionary<FieldKey, FieldInst>();
		_genericInstanceMethods = new Dictionary<MethodInstKey, GenericInstanceMethod>();
		_methodRefOnGenericInstanceTypes = new Dictionary<MethodInstKey, MethodRefOnTypeInst>();
		_systemImplementedArrayMethods = new Dictionary<SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>();
	}

	protected MemberStore(IMemberStorageStrategy storageStrategy, MemberStore other)
	{
		_storageStrategy = storageStrategy;
		_byRefTypeDefs = other._byRefTypeDefs;
		_pointerTypeDefs = other._pointerTypeDefs;
		_reqModTypeDefs = other._reqModTypeDefs;
		_optModTypeDefs = other._optModTypeDefs;
		_pinnedTypeDefs = other._pinnedTypeDefs;
		_vectorTypeDefs = other._vectorTypeDefs;
		_arrayTypeDefs = other._arrayTypeDefs;
		_genericInstanceTypeDefs = other._genericInstanceTypeDefs;
		_genericInstanceFields = other._genericInstanceFields;
		_genericInstanceMethods = other._genericInstanceMethods;
		_methodRefOnGenericInstanceTypes = other._methodRefOnGenericInstanceTypes;
		_systemImplementedArrayMethods = other._systemImplementedArrayMethods;
		_functionPointerTypes = other._functionPointerTypes;
		_systemImplementedArrayMethods = other._systemImplementedArrayMethods;
	}

	public PointerType GetOrAddPointerType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, PointerType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_pointerTypeDefs, elementType, callbacks);
	}

	public PointerType GetOrAddPointerType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, PointerType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_pointerTypeDefs, arg, elementType, callbacks);
	}

	public ByReferenceType GetOrAddByReferenceType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, ByReferenceType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_byRefTypeDefs, elementType, callbacks);
	}

	public ByReferenceType GetOrAddByReferenceType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, ByReferenceType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_byRefTypeDefs, arg, elementType, callbacks);
	}

	public PinnedType GetOrAddPinnedType(TypeReference elementType, IMemberStoreCreateCallbacks<TypeReference, PinnedType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_pinnedTypeDefs, elementType, callbacks);
	}

	public PinnedType GetOrAddPinnedType<TArg>(TArg arg, TypeReference elementType, IMemberStoreCreateCallbacks<TArg, TypeReference, PinnedType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_pinnedTypeDefs, arg, elementType, callbacks);
	}

	public ArrayType GetOrAddArray(TypeReference elementType, int rank, bool isVector, IMemberStoreCreateCallbacks<TypeReference, ArrayType> vectorCallbacks, IMemberStoreCreateCallbacks<ArrayKey, ArrayType> callbacks)
	{
		if (isVector)
		{
			return _storageStrategy.GetOrAdd(_vectorTypeDefs, elementType, vectorCallbacks);
		}
		return _storageStrategy.GetOrAdd(_arrayTypeDefs, new ArrayKey(elementType, rank), callbacks);
	}

	public ArrayType GetOrAddArray<TArg>(TArg arg, TypeReference elementType, int rank, bool isVector, IMemberStoreCreateCallbacks<TArg, TypeReference, ArrayType> vectorCallbacks, IMemberStoreCreateCallbacks<TArg, ArrayKey, ArrayType> callbacks)
	{
		if (isVector)
		{
			return _storageStrategy.GetOrAdd(_vectorTypeDefs, arg, elementType, vectorCallbacks);
		}
		return _storageStrategy.GetOrAdd(_arrayTypeDefs, arg, new ArrayKey(elementType, rank), callbacks);
	}

	public FunctionPointerType GetOrAddFunctionPointerType(MethodSignatureKey signature, IMemberStoreCreateCallbacks<MethodSignatureKey, FunctionPointerType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_functionPointerTypes, signature, callbacks);
	}

	public FunctionPointerType GetOrAddFunctionPointerType<TArg>(TArg arg, MethodSignatureKey signature, IMemberStoreCreateCallbacks<TArg, MethodSignatureKey, FunctionPointerType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_functionPointerTypes, arg, signature, callbacks);
	}

	public OptionalModifierType GetOrAddOptionalModiferType(ModifierKey modifierKey, IMemberStoreCreateCallbacks<ModifierKey, OptionalModifierType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_optModTypeDefs, modifierKey, callbacks);
	}

	public OptionalModifierType GetOrAddOptionalModiferType<TArg>(TArg arg, ModifierKey modifierKey, IMemberStoreCreateCallbacks<TArg, ModifierKey, OptionalModifierType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_optModTypeDefs, arg, modifierKey, callbacks);
	}

	public RequiredModifierType GetOrAddRequiredModifierType(ModifierKey modifierKey, IMemberStoreCreateCallbacks<ModifierKey, RequiredModifierType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_reqModTypeDefs, modifierKey, callbacks);
	}

	public RequiredModifierType GetOrAddRequiredModifierType<TArg>(TArg arg, ModifierKey modifierKey, IMemberStoreCreateCallbacks<TArg, ModifierKey, RequiredModifierType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_reqModTypeDefs, arg, modifierKey, callbacks);
	}

	public GenericInstanceType GetOrAddGenericInstanceType<TArg>(TArg arg, GenericInstanceTypeKey key, IMemberStoreCreateCallbacks<TArg, GenericInstanceTypeKey, GenericInstanceType> callbacks)
	{
		return _storageStrategy.GetOrAdd(_genericInstanceTypeDefs, arg, key, callbacks);
	}

	public FieldInst GetOrAddFieldReference(FieldKey key, IMemberStoreCreateCallbacks<FieldKey, FieldInst> callbacks)
	{
		return _storageStrategy.GetOrAdd(_genericInstanceFields, key, callbacks);
	}

	public FieldInst GetOrAddFieldReference<TArg>(TArg arg, FieldKey key, IMemberStoreCreateCallbacks<TArg, FieldKey, FieldInst> callbacks)
	{
		return _storageStrategy.GetOrAdd(_genericInstanceFields, arg, key, callbacks);
	}

	public GenericInstanceMethod GetOrAddGenericInstanceMethod<TArg>(TArg arg, MethodInstKey methodInstKey, IMemberStoreCreateCallbacks<TArg, MethodInstKey, GenericInstanceMethod> callbacks)
	{
		return _storageStrategy.GetOrAdd(_genericInstanceMethods, arg, methodInstKey, callbacks);
	}

	public MethodRefOnTypeInst GetOrAddMethodRefOnTypeInst<TArg>(TArg arg, MethodInstKey methodInstKey, IMemberStoreCreateCallbacks<TArg, MethodInstKey, MethodRefOnTypeInst> callbacks)
	{
		return _storageStrategy.GetOrAdd(_methodRefOnGenericInstanceTypes, arg, methodInstKey, callbacks);
	}

	public SystemImplementedArrayMethod GetOrAddSystemImplementedArrayMethod<TArg>(TArg arg, SystemImplementedArrayMethodKey arrayMethodKey, IMemberStoreCreateCallbacks<TArg, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod> callbacks)
	{
		return _storageStrategy.GetOrAdd(_systemImplementedArrayMethods, arg, arrayMethodKey, callbacks);
	}

	internal IEnumerable<TypeReference> AllTypesUnordered()
	{
		foreach (ByReferenceType value in _byRefTypeDefs.Values)
		{
			yield return value;
		}
		foreach (PointerType value2 in _pointerTypeDefs.Values)
		{
			yield return value2;
		}
		foreach (RequiredModifierType value3 in _reqModTypeDefs.Values)
		{
			yield return value3;
		}
		foreach (OptionalModifierType value4 in _optModTypeDefs.Values)
		{
			yield return value4;
		}
		foreach (PinnedType value5 in _pinnedTypeDefs.Values)
		{
			yield return value5;
		}
		foreach (ArrayType value6 in _vectorTypeDefs.Values)
		{
			yield return value6;
		}
		foreach (ArrayType value7 in _arrayTypeDefs.Values)
		{
			yield return value7;
		}
		foreach (GenericInstanceType value8 in _genericInstanceTypeDefs.Values)
		{
			yield return value8;
		}
		foreach (FunctionPointerType value9 in _functionPointerTypes.Values)
		{
			yield return value9;
		}
	}

	internal IEnumerable<MethodReference> AllMethodsUnordered()
	{
		foreach (GenericInstanceMethod value in _genericInstanceMethods.Values)
		{
			yield return value;
		}
		foreach (MethodRefOnTypeInst value2 in _methodRefOnGenericInstanceTypes.Values)
		{
			yield return value2;
		}
		foreach (SystemImplementedArrayMethod value3 in _systemImplementedArrayMethods.Values)
		{
			yield return value3;
		}
	}

	internal IEnumerable<FieldReference> AllFieldsUnordered()
	{
		foreach (FieldInst value in _genericInstanceFields.Values)
		{
			yield return value;
		}
	}

	public void Dispose()
	{
		_storageStrategy?.Dispose();
	}
}
