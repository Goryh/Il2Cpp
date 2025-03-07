using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class CecilSourcedAssemblyData
{
	internal readonly UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> Assembly;

	internal readonly AssemblyDefinitionTables DefinitionTables;

	internal readonly bool NotAvailable;

	private readonly TypeContext _context;

	private ReadOnlyDictionary<Mono.Cecil.TypeReference, TypeReference> _typeReferenceLookup;

	private ReadOnlyDictionary<Mono.Cecil.MethodReference, MethodReference> _methodReferenceLookup;

	private ReadOnlyDictionary<Mono.Cecil.FieldReference, FieldReference> _fieldReferenceLookup;

	private ReadOnlyDictionary<GenericTypeReference, TypeReference> _genericInstanceLookup;

	internal ReferenceUsages ReferenceUsages { get; private set; }

	public CecilSourcedAssemblyData(TypeContext context, UnderConstruction<AssemblyDefinition, Mono.Cecil.AssemblyDefinition> assembly)
	{
		_context = context;
		Assembly = assembly;
		DefinitionTables = new AssemblyDefinitionTables(assembly.Ours, assembly.Source);
		NotAvailable = false;
	}

	internal void SetData(ReferenceUsages referenceUsages)
	{
		ReferenceUsages = referenceUsages;
	}

	internal void InitializeTypeReferences(ReadOnlyDictionary<Mono.Cecil.TypeReference, TypeReference> typeReferenceLookup)
	{
		_typeReferenceLookup = typeReferenceLookup;
	}

	internal void InitializeGenericInstanceLookup(ReadOnlyDictionary<GenericTypeReference, TypeReference> genericInstanceLookup)
	{
		_genericInstanceLookup = genericInstanceLookup;
	}

	internal void InitializeMethodReferences(ReadOnlyDictionary<Mono.Cecil.MethodReference, MethodReference> methodReferenceLookup)
	{
		_methodReferenceLookup = methodReferenceLookup;
	}

	internal void InitializeFieldReferences(ReadOnlyDictionary<Mono.Cecil.FieldReference, FieldReference> fieldReferenceLookup)
	{
		_fieldReferenceLookup = fieldReferenceLookup;
	}

	internal TypeReference ResolveReference(Mono.Cecil.TypeReference typeReference)
	{
		if (typeReference == null)
		{
			return null;
		}
		if (typeReference is Mono.Cecil.TypeDefinition typeDefinition)
		{
			return _context.GetDef(typeDefinition);
		}
		if (typeReference is Mono.Cecil.GenericParameter genericParameter && genericParameter.Owner.IsDefinition)
		{
			return _context.GetDef(genericParameter);
		}
		if (!_typeReferenceLookup.TryGetValue(typeReference, out var typeRef))
		{
			throw new KeyNotFoundException("Could not find TypeReference for " + typeReference.ToString());
		}
		return typeRef;
	}

	internal TypeReference ResolveReference(Mono.Cecil.TypeReference typeReference, Mono.Cecil.IGenericInstance genericInstance)
	{
		if (typeReference == null)
		{
			return null;
		}
		if (genericInstance != null && _genericInstanceLookup.TryGetValue(new GenericTypeReference(typeReference, genericInstance), out var typeRef))
		{
			return typeRef;
		}
		return ResolveReference(typeReference);
	}

	internal MethodReference ResolveReference(Mono.Cecil.MethodReference methodReference)
	{
		if (methodReference is Mono.Cecil.MethodDefinition methodDefinition)
		{
			return _context.GetDef(methodDefinition);
		}
		if (!_methodReferenceLookup.TryGetValue(methodReference, out var methodRef))
		{
			throw new KeyNotFoundException("Could not find MethodReference for " + methodReference.ToString());
		}
		return methodRef;
	}

	internal FieldReference ResolveReference(Mono.Cecil.FieldReference fieldReference)
	{
		if (fieldReference is Mono.Cecil.FieldDefinition fieldDefinition)
		{
			return _context.GetDef(fieldDefinition);
		}
		if (!_fieldReferenceLookup.TryGetValue(fieldReference, out var fieldRef))
		{
			throw new KeyNotFoundException("Could not find FieldReference for " + fieldReference.ToString());
		}
		return fieldRef;
	}

	public override string ToString()
	{
		return Assembly.Ours.Name.Name;
	}
}
