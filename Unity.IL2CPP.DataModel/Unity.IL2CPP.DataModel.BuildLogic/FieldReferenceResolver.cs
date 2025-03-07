using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class FieldReferenceResolver
{
	private readonly TypeContext _context;

	private readonly UnderConstructionFieldReferenceRepository _globalLookup;

	public FieldReferenceResolver(TypeContext context, UnderConstructionFieldReferenceRepository fieldReferenceRepository)
	{
		_context = context;
		_globalLookup = fieldReferenceRepository;
	}

	public void ProcessAssembly(CecilSourcedAssemblyData assemblyData)
	{
		ResolveAssemblyFieldReferences(_context, _globalLookup, assemblyData, assemblyData.ReferenceUsages.Fields);
	}

	private static void ResolveAssemblyFieldReferences(TypeContext context, UnderConstructionFieldReferenceRepository globalLookup, CecilSourcedAssemblyData assemblyDef, ReadOnlyHashSet<Mono.Cecil.FieldReference> fieldReferences)
	{
		Dictionary<Mono.Cecil.FieldReference, FieldReference> mapping = new Dictionary<Mono.Cecil.FieldReference, FieldReference>(fieldReferences.Count);
		foreach (Mono.Cecil.FieldReference typeReference in fieldReferences)
		{
			ResolveFieldReference(context, globalLookup, assemblyDef, typeReference, mapping);
		}
		assemblyDef.InitializeFieldReferences(mapping.AsReadOnly());
	}

	private static void ResolveFieldReference(TypeContext context, UnderConstructionFieldReferenceRepository globalLookup, CecilSourcedAssemblyData assemblyDef, Mono.Cecil.FieldReference fieldReference, Dictionary<Mono.Cecil.FieldReference, FieldReference> mapping)
	{
		if (!mapping.TryGetValue(fieldReference, out var fieldRef))
		{
			fieldRef = ((!(fieldReference.DeclaringType is Mono.Cecil.GenericInstanceType genericInstanceType)) ? ((FieldReference)context.GetDef(fieldReference)) : ((FieldReference)globalLookup.ResolveGenericInst(context.GetDef(fieldReference), (GenericInstanceType)assemblyDef.ResolveReference(fieldReference.DeclaringType, genericInstanceType), fieldReference)));
			mapping.Add(fieldReference, fieldRef);
		}
	}
}
