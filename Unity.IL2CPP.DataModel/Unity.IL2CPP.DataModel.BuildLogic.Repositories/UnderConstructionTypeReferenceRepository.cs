using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionTypeReferenceRepository : IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData, TypeReference), GenericInstanceTypeKey, GenericInstanceType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PointerType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ByReferenceType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PinnedType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ArrayType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ArrayKey, ArrayType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, OptionalModifierType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, RequiredModifierType>, IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), MethodSignatureKey, FunctionPointerType>, IDisposable
{
	private readonly TypeContext _context;

	private readonly IMemberStore _memberStore;

	private readonly ConcurrentBag<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> _allNonDefinitions = new ConcurrentBag<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>>();

	public UnderConstructionTypeReferenceRepository(TypeContext context, IMemberStore memberStore)
	{
		_context = context;
		_memberStore = memberStore;
	}

	public ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> CurrentItems()
	{
		return _allNonDefinitions.ToArray().AsReadOnly();
	}

	private void ProcessCreate<TDataModel, TSource>(TDataModel item, TSource source, CecilSourcedAssemblyData assemblyData) where TDataModel : TypeReference where TSource : Mono.Cecil.TypeReference
	{
		_allNonDefinitions.Add(new UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>(item, source, assemblyData));
	}

	public ByReferenceType ResolveByRefType(TypeReference elementType)
	{
		return ResolveByRefType(elementType, null, null);
	}

	public PointerType ResolvePointerType(TypeReference elementType)
	{
		return ResolvePointerType(elementType, null, null);
	}

	public OptionalModifierType ResolveOptionalModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return ResolveOptionalModifierType(modifierType, elementType, null, null);
	}

	public RequiredModifierType ResolveRequiredModifierType(TypeReference modifierType, TypeReference elementType)
	{
		return ResolveRequiredModifierType(modifierType, elementType, null, null);
	}

	public PinnedType ResolvePinnedType(TypeReference elementType)
	{
		return ResolvePinnedType(elementType, null, null);
	}

	public ArrayType ResolveArray(TypeReference elementType, int rank, bool isVector)
	{
		return ResolveArray(elementType, rank, isVector, null, null);
	}

	public GenericInstanceType ResolveGenericInst(TypeDefinition genericTypeDef, TypeReference declaringType, TypeReference[] genericArguments)
	{
		return ResolveGenericInst(genericTypeDef, declaringType, genericArguments.AsReadOnly(), null, null);
	}

	public GenericInstanceType ResolveGenericInst(TypeDefinition genericTypeDef, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments)
	{
		return ResolveGenericInst(genericTypeDef, declaringType, genericArguments, null, null);
	}

	public ByReferenceType ResolveByRefType(TypeReference elementType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddByReferenceType((source, assemblyData), elementType, this);
	}

	public PointerType ResolvePointerType(TypeReference elementType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddPointerType((source, assemblyData), elementType, this);
	}

	public OptionalModifierType ResolveOptionalModifierType(TypeReference modifierType, TypeReference elementType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddOptionalModiferType((source, assemblyData), new ModifierKey(modifierType, elementType), this);
	}

	public RequiredModifierType ResolveRequiredModifierType(TypeReference modifierType, TypeReference elementType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddRequiredModifierType((source, assemblyData), new ModifierKey(modifierType, elementType), this);
	}

	public PinnedType ResolvePinnedType(TypeReference elementType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddPinnedType((source, assemblyData), elementType, this);
	}

	public ArrayType ResolveArray(TypeReference elementType, int rank, bool isVector, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddArray((source, assemblyData), elementType, rank, isVector, this, this);
	}

	public GenericInstanceType ResolveGenericInst(TypeDefinition genericTypeDef, TypeReference declaringType, ReadOnlyCollection<TypeReference> genericArguments, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddGenericInstanceType((source, assemblyData, declaringType), new GenericInstanceTypeKey(genericTypeDef, genericArguments), this);
	}

	public FunctionPointerType ResolveFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis)
	{
		return ResolveFunctionPointerType(new MethodSignatureKey(returnType, parameters, callingConvention, hasThis, explicitThis), null, null);
	}

	public FunctionPointerType ResolveFunctionPointerType(TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters, Mono.Cecil.FunctionPointerType functionPointerType, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return ResolveFunctionPointerType(new MethodSignatureKey(returnType, parameters, (MethodCallingConvention)functionPointerType.CallingConvention, functionPointerType.HasThis, functionPointerType.ExplicitThis), source, assemblyData);
	}

	private FunctionPointerType ResolveFunctionPointerType(MethodSignatureKey key, Mono.Cecil.TypeReference source, CecilSourcedAssemblyData assemblyData)
	{
		return _memberStore.GetOrAddFunctionPointerType((source, assemblyData), key, this);
	}

	GenericInstanceType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData, TypeReference), GenericInstanceTypeKey, GenericInstanceType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData, TypeReference) arg, GenericInstanceTypeKey key)
	{
		return new GenericInstanceType(key.TypeDefinition, arg.Item3, new GenericInst(key.GenericArguments), _context);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData, TypeReference), GenericInstanceTypeKey, GenericInstanceType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData, TypeReference) arg, GenericInstanceType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	PointerType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PointerType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, TypeReference key)
	{
		return new PointerType(key, _context);
	}

	ArrayType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ArrayKey, ArrayType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ArrayKey key)
	{
		return new ArrayType(key.ElementType, key.Rank, isVector: false, _context);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ArrayKey, ArrayType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ArrayType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ArrayType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ArrayType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PinnedType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, PinnedType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ByReferenceType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ByReferenceType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PointerType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, PointerType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	ByReferenceType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ByReferenceType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, TypeReference key)
	{
		return new ByReferenceType(key, _context);
	}

	PinnedType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, PinnedType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, TypeReference key)
	{
		return new PinnedType(key, _context);
	}

	ArrayType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), TypeReference, ArrayType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, TypeReference key)
	{
		return new ArrayType(key, 1, isVector: true, _context);
	}

	OptionalModifierType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, OptionalModifierType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ModifierKey key)
	{
		return new OptionalModifierType(key.ModifierType, key.ElementType, _context);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, RequiredModifierType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, RequiredModifierType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, OptionalModifierType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, OptionalModifierType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	RequiredModifierType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), ModifierKey, RequiredModifierType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, ModifierKey key)
	{
		return new RequiredModifierType(key.ModifierType, key.ElementType, _context);
	}

	FunctionPointerType IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), MethodSignatureKey, FunctionPointerType>.Create((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, MethodSignatureKey key)
	{
		return new FunctionPointerType(key.ReturnType, key.Parameters, key.CallingConvention, key.HasThis, key.ExplicitThis, _context);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.TypeReference, CecilSourcedAssemblyData), MethodSignatureKey, FunctionPointerType>.OnCreate((Mono.Cecil.TypeReference, CecilSourcedAssemblyData) arg, FunctionPointerType created)
	{
		ProcessCreate(created, arg.Item1, arg.Item2);
	}

	public void Dispose()
	{
		_allNonDefinitions.Clear();
	}
}
