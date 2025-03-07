using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionMethodReferenceRepository : IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, GenericInstanceMethod>, IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>, IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, MethodRefOnTypeInst>, IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>, IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData, Mono.Cecil.MethodReference), SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>, IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>, IDisposable
{
	private readonly ConcurrentBag<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> _allNonDefinitions = new ConcurrentBag<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>>();

	private readonly IMemberStore _memberStore;

	public int CurrentCount => _allNonDefinitions.Count;

	public UnderConstructionMethodReferenceRepository(TypeContext context, IMemberStore memberStore)
	{
		_memberStore = memberStore;
	}

	public ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> CurrentItems()
	{
		return _allNonDefinitions.ToArray().AsReadOnly();
	}

	private void ProcessCreated<TDataModel, TSource>(TDataModel item, TSource source, CecilSourcedAssemblyData assemblyData) where TDataModel : MethodReference where TSource : Mono.Cecil.MethodReference
	{
		_allNonDefinitions.Add(new UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>(item, source, assemblyData));
	}

	public GenericInstanceMethod ResolveGenericMethodInst(CecilSourcedAssemblyData assemblyData, MethodDefinition genericMethodDef, TypeReference[] typeGenericArguments, TypeReference[] methodGenericArguments, Mono.Cecil.MethodReference source)
	{
		return _memberStore.GetOrAddGenericInstanceMethod((source, assemblyData), new MethodInstKey(genericMethodDef, typeGenericArguments, methodGenericArguments), this);
	}

	public MethodRefOnTypeInst ResolveMethodRefOnGenericInstType(CecilSourcedAssemblyData assemblyData, MethodDefinition genericMethodDef, TypeReference[] typeGenericArguments, Mono.Cecil.MethodReference source)
	{
		return _memberStore.GetOrAddMethodRefOnTypeInst((source, assemblyData), new MethodInstKey(genericMethodDef, typeGenericArguments, null), this);
	}

	public SystemImplementedArrayMethod ResolveSystemImplementedArrayMethod(CecilSourcedAssemblyData assemblyData, ArrayType arrayType, Mono.Cecil.MethodReference methodReference, Mono.Cecil.MethodReference source)
	{
		return _memberStore.GetOrAddSystemImplementedArrayMethod((source, assemblyData, methodReference), new SystemImplementedArrayMethodKey(arrayType, methodReference.Name), this);
	}

	public GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition genericMethodDef, TypeReference[] methodGenericArguments)
	{
		return CreateGenericInstanceMethod(declaringType, genericMethodDef, methodGenericArguments.AsReadOnly());
	}

	public GenericInstanceMethod CreateGenericInstanceMethod(TypeReference declaringType, MethodDefinition genericMethodDef, ReadOnlyCollection<TypeReference> methodGenericArguments)
	{
		return _memberStore.GetOrAddGenericInstanceMethod(declaringType, new MethodInstKey(genericMethodDef, (declaringType as GenericInstanceType)?.GenericArguments, methodGenericArguments), this);
	}

	public SystemImplementedArrayMethod CreateSystemImplementedArrayMethod(ArrayType arrayType, SystemImplementedArrayMethod methodReference)
	{
		return _memberStore.GetOrAddSystemImplementedArrayMethod(methodReference, new SystemImplementedArrayMethodKey(arrayType, methodReference.Name), this);
	}

	public MethodRefOnTypeInst CreateMethodReferenceOnGenericInstance(GenericInstanceType declaringType, MethodDefinition genericMethodDef)
	{
		return _memberStore.GetOrAddMethodRefOnTypeInst(declaringType, new MethodInstKey(genericMethodDef, declaringType.GenericArguments, null), this);
	}

	GenericInstanceMethod IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, GenericInstanceMethod>.Create((Mono.Cecil.MethodReference, CecilSourcedAssemblyData) arg, MethodInstKey key)
	{
		Mono.Cecil.IGenericInstance genericInstance = (arg.Item1 as Mono.Cecil.IGenericInstance) ?? (arg.Item1.DeclaringType as Mono.Cecil.IGenericInstance);
		return new GenericInstanceMethod(arg.Item2.ResolveReference(arg.Item1.DeclaringType, genericInstance), key.MethodDef, new GenericInst(key.MethodGenericArguments));
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, MethodRefOnTypeInst>.OnCreate((Mono.Cecil.MethodReference, CecilSourcedAssemblyData) arg, MethodRefOnTypeInst created)
	{
		ProcessCreated(created, arg.Item1, arg.Item2);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, GenericInstanceMethod>.OnCreate((Mono.Cecil.MethodReference, CecilSourcedAssemblyData) arg, GenericInstanceMethod created)
	{
		ProcessCreated(created, arg.Item1, arg.Item2);
	}

	MethodRefOnTypeInst IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData), MethodInstKey, MethodRefOnTypeInst>.Create((Mono.Cecil.MethodReference, CecilSourcedAssemblyData) arg, MethodInstKey key)
	{
		Mono.Cecil.IGenericInstance genericInstance = arg.Item1.DeclaringType as Mono.Cecil.IGenericInstance;
		return new MethodRefOnTypeInst((GenericInstanceType)arg.Item2.ResolveReference(arg.Item1.DeclaringType, genericInstance), key.MethodDef);
	}

	SystemImplementedArrayMethod IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData, Mono.Cecil.MethodReference), SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.Create((Mono.Cecil.MethodReference, CecilSourcedAssemblyData, Mono.Cecil.MethodReference) arg, SystemImplementedArrayMethodKey key)
	{
		Mono.Cecil.IGenericInstance genericInstance = (arg.Item3 as Mono.Cecil.IGenericInstance) ?? (arg.Item3.DeclaringType as Mono.Cecil.IGenericInstance);
		return new SystemImplementedArrayMethod(arg.Item3.Name, arg.Item2.ResolveReference(arg.Item3.ReturnType, genericInstance), ParameterDefBuilder.BuildInitializedParameters(arg.Item1, (arg.Item2, genericInstance), ((CecilSourcedAssemblyData, Mono.Cecil.IGenericInstance genericInstance) r, Mono.Cecil.TypeReference type) => r.Item1.ResolveReference(type, r.genericInstance)), key.ArrayType);
	}

	void IMemberStoreCreateCallbacks<(Mono.Cecil.MethodReference, CecilSourcedAssemblyData, Mono.Cecil.MethodReference), SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.OnCreate((Mono.Cecil.MethodReference, CecilSourcedAssemblyData, Mono.Cecil.MethodReference) arg, SystemImplementedArrayMethod created)
	{
		ProcessCreated(created, arg.Item1, arg.Item2);
	}

	GenericInstanceMethod IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.Create(TypeReference arg, MethodInstKey key)
	{
		return new GenericInstanceMethod(arg, key.MethodDef, new GenericInst(key.MethodGenericArguments));
	}

	void IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.OnCreate(GenericInstanceType arg, MethodRefOnTypeInst created)
	{
		ProcessCreated<MethodRefOnTypeInst, Mono.Cecil.MethodReference>(created, null, null);
	}

	void IMemberStoreCreateCallbacks<TypeReference, MethodInstKey, GenericInstanceMethod>.OnCreate(TypeReference arg, GenericInstanceMethod created)
	{
		ProcessCreated<GenericInstanceMethod, Mono.Cecil.MethodReference>(created, null, null);
	}

	MethodRefOnTypeInst IMemberStoreCreateCallbacks<GenericInstanceType, MethodInstKey, MethodRefOnTypeInst>.Create(GenericInstanceType arg, MethodInstKey key)
	{
		return new MethodRefOnTypeInst(arg, key.MethodDef);
	}

	SystemImplementedArrayMethod IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.Create(SystemImplementedArrayMethod arg, SystemImplementedArrayMethodKey key)
	{
		return arg.Clone(key.ArrayType);
	}

	void IMemberStoreCreateCallbacks<SystemImplementedArrayMethod, SystemImplementedArrayMethodKey, SystemImplementedArrayMethod>.OnCreate(SystemImplementedArrayMethod arg, SystemImplementedArrayMethod created)
	{
		ProcessCreated<SystemImplementedArrayMethod, Mono.Cecil.MethodReference>(created, null, null);
	}

	public void Dispose()
	{
		_allNonDefinitions.Clear();
	}
}
