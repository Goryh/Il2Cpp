using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;
using Unity.IL2CPP.DataModel.Comparers;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class TypeReferenceResolver
{
	private class TypeMapping
	{
		public readonly Dictionary<Mono.Cecil.TypeReference, TypeReference> Types;

		public readonly Dictionary<GenericTypeReference, TypeReference> GenericInstances;

		public TypeMapping(ReadOnlyHashSet<Mono.Cecil.TypeReference> typeReferences, ReadOnlyHashSet<GenericTypeReference> genericInstances)
		{
			Types = typeReferences.ToDictionary((Mono.Cecil.TypeReference t) => t, (Mono.Cecil.TypeReference _) => (TypeReference)null);
			GenericInstances = genericInstances.ToDictionary((GenericTypeReference g) => g, (GenericTypeReference _) => (TypeReference)null, new GenericTypeReferenceEqualityComparer());
		}
	}

	private class GenericMappingContext : MappingContext
	{
		private readonly Dictionary<GenericTypeReference, TypeReference> _types;

		public override Mono.Cecil.IGenericInstance GenericInstance { get; }

		public GenericMappingContext(AssemblyDefinition assembly, TypeMapping mapping, Mono.Cecil.IGenericInstance genericInstance)
			: base(assembly, mapping)
		{
			GenericInstance = genericInstance;
			_types = mapping.GenericInstances;
		}

		public override bool TryGetValue(Mono.Cecil.TypeReference typeReference, out TypeReference typeRef)
		{
			if (!typeReference.ContainsGenericParameter)
			{
				return base.TryGetValue(typeReference, out typeRef);
			}
			if (_types.TryGetValue(new GenericTypeReference(typeReference, GenericInstance), out typeRef))
			{
				return typeRef != null;
			}
			return false;
		}

		public override void Add(Mono.Cecil.TypeReference typeReference, TypeReference typeRef)
		{
			if (!typeReference.ContainsGenericParameter)
			{
				base.Add(typeReference, typeRef);
			}
			else
			{
				_types[new GenericTypeReference(typeReference, GenericInstance)] = typeRef;
			}
		}
	}

	private class MappingContext
	{
		private readonly TypeMapping _mapping;

		private readonly Dictionary<Mono.Cecil.TypeReference, TypeReference> _types;

		public AssemblyDefinition Assembly { get; }

		public virtual Mono.Cecil.IGenericInstance GenericInstance => null;

		public MappingContext(AssemblyDefinition assembly, TypeMapping mapping)
		{
			Assembly = assembly;
			_mapping = mapping;
			_types = mapping.Types;
		}

		public virtual bool TryGetValue(Mono.Cecil.TypeReference typeReference, out TypeReference typeRef)
		{
			if (_types.TryGetValue(typeReference, out typeRef))
			{
				return typeRef != null;
			}
			return false;
		}

		public virtual void Add(Mono.Cecil.TypeReference typeReference, TypeReference typeRef)
		{
			_types[typeReference] = typeRef;
		}

		public MappingContext NewContext()
		{
			return new MappingContext(Assembly, _mapping);
		}
	}

	private readonly TypeContext _context;

	private readonly UnderConstructionTypeReferenceRepository _typeReferenceRepository;

	public TypeReferenceResolver(TypeContext context, UnderConstructionTypeReferenceRepository typeReferenceRepository)
	{
		_context = context;
		_typeReferenceRepository = typeReferenceRepository;
	}

	public void ProcessAssembly(CecilSourcedAssemblyData assemblyData)
	{
		ResolveAssemblyTypeReferences(assemblyData, assemblyData.ReferenceUsages.Types, assemblyData.ReferenceUsages.GenericInstances);
	}

	private void ResolveAssemblyTypeReferences(CecilSourcedAssemblyData assembly, ReadOnlyHashSet<Mono.Cecil.TypeReference> typeReferences, ReadOnlyHashSet<GenericTypeReference> genericInstances)
	{
		TypeMapping mapping = new TypeMapping(typeReferences, genericInstances);
		foreach (GenericTypeReference genericInstance in genericInstances)
		{
			ResolveTypeReference(genericInstance.TypeReference, new GenericMappingContext(assembly.Assembly.Ours, mapping, genericInstance.GenericInstance), assembly);
		}
		foreach (Mono.Cecil.TypeReference typeReference in typeReferences)
		{
			ResolveTypeReference(typeReference, new MappingContext(assembly.Assembly.Ours, mapping), assembly);
		}
		assembly.InitializeTypeReferences(mapping.Types.AsReadOnly());
		assembly.InitializeGenericInstanceLookup(mapping.GenericInstances.AsReadOnly());
	}

	private TypeReference ResolveTypeReference(Mono.Cecil.TypeReference typeReference, MappingContext mappingContext, CecilSourcedAssemblyData assembly)
	{
		if (typeReference == null)
		{
			return null;
		}
		if (mappingContext.TryGetValue(typeReference, out var typeRef))
		{
			return typeRef;
		}
		if (!(typeReference is Mono.Cecil.TypeDefinition typeDefinition))
		{
			if (!(typeReference is Mono.Cecil.GenericParameter genericParameter))
			{
				typeRef = ((typeReference is Mono.Cecil.ByReferenceType byReferenceType) ? _typeReferenceRepository.ResolveByRefType(ResolveTypeReference(byReferenceType.ElementType, mappingContext, assembly), byReferenceType, assembly) : ((typeReference is Mono.Cecil.PointerType pointerType) ? _typeReferenceRepository.ResolvePointerType(ResolveTypeReference(pointerType.ElementType, mappingContext, assembly), pointerType, assembly) : ((typeReference is Mono.Cecil.ArrayType arrayType) ? _typeReferenceRepository.ResolveArray(ResolveTypeReference(arrayType.ElementType, mappingContext, assembly), arrayType.Rank, arrayType.IsVector) : ((typeReference is Mono.Cecil.PinnedType pinnedType) ? _typeReferenceRepository.ResolvePinnedType(ResolveTypeReference(pinnedType.ElementType, mappingContext, assembly), pinnedType, assembly) : ((typeReference is Mono.Cecil.GenericInstanceType genericInstanceType) ? _typeReferenceRepository.ResolveGenericInst((TypeDefinition)ResolveTypeReference(genericInstanceType.ElementType, mappingContext, assembly), ResolveTypeReference(genericInstanceType.DeclaringType, mappingContext, assembly), genericInstanceType.GenericArguments.Select((Mono.Cecil.TypeReference a) => ResolveTypeReference(a, mappingContext, assembly)).ToArray().AsReadOnly(), genericInstanceType, assembly) : ((typeReference is Mono.Cecil.OptionalModifierType optionalModifierType) ? _typeReferenceRepository.ResolveOptionalModifierType(ResolveTypeReference(optionalModifierType.ModifierType, mappingContext, assembly), ResolveTypeReference(optionalModifierType.ElementType, mappingContext, assembly), optionalModifierType, assembly) : ((typeReference is Mono.Cecil.RequiredModifierType requiredModifierType) ? _typeReferenceRepository.ResolveRequiredModifierType(ResolveTypeReference(requiredModifierType.ModifierType, mappingContext, assembly), ResolveTypeReference(requiredModifierType.ElementType, mappingContext, assembly), requiredModifierType, assembly) : ((typeReference is Mono.Cecil.FunctionPointerType functionPointerType) ? _typeReferenceRepository.ResolveFunctionPointerType(ResolveTypeReference(functionPointerType.ReturnType, mappingContext, assembly), ParameterDefBuilder.BuildInitializedParameters(functionPointerType, (this, mappingContext, assembly), ((TypeReferenceResolver, MappingContext mappingContext, CecilSourcedAssemblyData assembly) arg, Mono.Cecil.TypeReference type) => ResolveTypeReference(type, arg.mappingContext, arg.assembly)), functionPointerType, functionPointerType, assembly) : ((!(typeReference is Mono.Cecil.SentinelType sentinelType)) ? _context.GetDef(typeReference) : ResolveTypeReference(sentinelType.ElementType, mappingContext, assembly))))))))));
			}
			else
			{
				if (mappingContext.GenericInstance != null)
				{
					return ResolveGenericParameterInstance(genericParameter, mappingContext, assembly);
				}
				if (genericParameter.Owner.IsDefinition)
				{
					return _context.GetDef(genericParameter);
				}
				if (genericParameter.Type == Mono.Cecil.GenericParameterType.Type)
				{
					typeRef = _context.GetGenericParameterDef(genericParameter.DeclaringType, genericParameter.Position);
				}
				else
				{
					if (genericParameter.Type != Mono.Cecil.GenericParameterType.Method)
					{
						throw new InvalidOperationException("Generic parameters that are declared on instances should have already been resolved in ResolveGenericParameterInstance");
					}
					typeRef = _context.GetGenericParameterDef(genericParameter.DeclaringMethod, genericParameter.Position);
				}
			}
			mappingContext.Add(typeReference, typeRef);
			return typeRef;
		}
		return _context.GetDef(typeDefinition);
	}

	private TypeReference ResolveGenericParameterInstance(Mono.Cecil.GenericParameter genericParameter, MappingContext mappingContext, CecilSourcedAssemblyData assembly)
	{
		Mono.Cecil.IGenericInstance genericInstance = mappingContext.GenericInstance;
		Mono.Cecil.TypeReference resolvedType = ((!(genericInstance is Mono.Cecil.GenericInstanceMethod genericInstanceMethod)) ? GenericParameterResolver.ResolveIfNeeded(null, (Mono.Cecil.GenericInstanceType)genericInstance, genericParameter) : GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, genericInstanceMethod.DeclaringType as Mono.Cecil.GenericInstanceType, genericParameter));
		MappingContext nonGenericContext = null;
		if (resolvedType is Mono.Cecil.GenericParameter resolvedGenericParameter && !resolvedGenericParameter.Owner.IsDefinition)
		{
			Mono.Cecil.IGenericParameterProvider owner = ((resolvedGenericParameter.Type != Mono.Cecil.GenericParameterType.Method) ? ((Mono.Cecil.IGenericParameterProvider)resolvedGenericParameter.DeclaringType.Resolve()) : ((Mono.Cecil.IGenericParameterProvider)resolvedGenericParameter.DeclaringMethod.Resolve()));
			resolvedType = owner.GenericParameters[resolvedGenericParameter.Position];
			nonGenericContext = mappingContext.NewContext();
		}
		TypeReference typeRef = ResolveTypeReference(resolvedType, mappingContext.NewContext(), assembly);
		mappingContext.Add(genericParameter, typeRef);
		nonGenericContext?.Add(genericParameter, typeRef);
		return typeRef;
	}
}
