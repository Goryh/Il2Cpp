using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionFieldReferenceRepository : IMemberStoreCreateCallbacks<Mono.Cecil.FieldReference, FieldKey, FieldInst>, IDisposable
{
	private readonly IMemberStore _memberStore;

	private readonly ConcurrentBag<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> _allNonDefinitions = new ConcurrentBag<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>>();

	public UnderConstructionFieldReferenceRepository(TypeContext context, IMemberStore memberStore)
	{
		_memberStore = memberStore;
	}

	public ReadOnlyCollection<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> CurrentItems()
	{
		return _allNonDefinitions.ToArray().AsReadOnly();
	}

	private void ProcessCreated<TDataModel, TSource>(TDataModel item, TSource source) where TDataModel : FieldReference where TSource : Mono.Cecil.FieldReference
	{
		_allNonDefinitions.Add(new UnderConstruction<FieldReference, Mono.Cecil.FieldReference>(item, source));
	}

	public FieldInst ResolveGenericInst(GenericInstanceType declaringType, FieldReference fieldReference)
	{
		return ResolveGenericInst(fieldReference.FieldDef, declaringType, null);
	}

	public FieldInst ResolveGenericInst(FieldDefinition genericFieldDef, GenericInstanceType declaringType, Mono.Cecil.FieldReference source)
	{
		return _memberStore.GetOrAddFieldReference(source, new FieldKey(genericFieldDef, declaringType), this);
	}

	FieldInst IMemberStoreCreateCallbacks<Mono.Cecil.FieldReference, FieldKey, FieldInst>.Create(Mono.Cecil.FieldReference arg, FieldKey key)
	{
		return new FieldInst(key.Field.FieldDef, key.DeclaringType);
	}

	void IMemberStoreCreateCallbacks<Mono.Cecil.FieldReference, FieldKey, FieldInst>.OnCreate(Mono.Cecil.FieldReference arg, FieldInst created)
	{
		ProcessCreated(created, arg);
	}

	public void Dispose()
	{
		_allNonDefinitions.Clear();
	}
}
