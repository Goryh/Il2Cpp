using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class MethodReferenceResolver
{
	private readonly TypeContext _context;

	private readonly UnderConstructionMethodReferenceRepository _methodReferenceRepository;

	public MethodReferenceResolver(TypeContext context, UnderConstructionMethodReferenceRepository methodReferenceRepository)
	{
		_context = context;
		_methodReferenceRepository = methodReferenceRepository;
	}

	public void ProcessAssembly(CecilSourcedAssemblyData assemblyData)
	{
		ResolveAssemblyMethodReferences(_context, _methodReferenceRepository, assemblyData, assemblyData.ReferenceUsages.Methods);
	}

	private static void ResolveAssemblyMethodReferences(TypeContext context, UnderConstructionMethodReferenceRepository repository, CecilSourcedAssemblyData assemblyDef, ReadOnlyHashSet<Mono.Cecil.MethodReference> methodReferences)
	{
		Dictionary<Mono.Cecil.MethodReference, MethodReference> mapping = new Dictionary<Mono.Cecil.MethodReference, MethodReference>(methodReferences.Count);
		foreach (Mono.Cecil.MethodReference typeReference in methodReferences)
		{
			ResolveMethodReference(context, repository, assemblyDef, typeReference, mapping);
		}
		assemblyDef.InitializeMethodReferences(mapping.AsReadOnly());
	}

	private static MethodReference ResolveMethodReference(TypeContext context, UnderConstructionMethodReferenceRepository repository, CecilSourcedAssemblyData assemblyData, Mono.Cecil.MethodReference methodReference, Dictionary<Mono.Cecil.MethodReference, MethodReference> mapping)
	{
		if (mapping.TryGetValue(methodReference, out var methodRef))
		{
			return methodRef;
		}
		if (methodReference is Mono.Cecil.GenericInstanceMethod genericInstanceMethod)
		{
			methodRef = repository.ResolveGenericMethodInst(assemblyData, context.GetDef(genericInstanceMethod.ElementMethod), BuildTypeGenericArgs(assemblyData, methodReference), BuildMethodGenericArgs(assemblyData, methodReference), genericInstanceMethod);
		}
		else if (methodReference.DeclaringType.IsGenericInstance)
		{
			methodRef = repository.ResolveMethodRefOnGenericInstType(assemblyData, context.GetDef(methodReference), BuildTypeGenericArgs(assemblyData, methodReference), methodReference);
		}
		else if (IsSpecialArrayMethod(methodReference))
		{
			Mono.Cecil.IGenericInstance genericInstance = (methodReference as Mono.Cecil.IGenericInstance) ?? (methodReference.DeclaringType as Mono.Cecil.IGenericInstance);
			ArrayType arrayType = (ArrayType)assemblyData.ResolveReference(methodReference.DeclaringType, genericInstance);
			methodRef = repository.ResolveSystemImplementedArrayMethod(assemblyData, arrayType, methodReference, methodReference);
		}
		else
		{
			Mono.Cecil.MethodDefinition methodDefinition = methodReference.Resolve();
			if (methodDefinition == null)
			{
				throw new InvalidOperationException("Found a method reference we cannot resolve to a method definition: " + methodReference.ToString());
			}
			methodRef = context.GetDef(methodDefinition);
		}
		mapping.Add(methodReference, methodRef);
		return methodRef;
	}

	internal static bool IsSpecialArrayMethod(Mono.Cecil.MethodReference methodReference)
	{
		if (methodReference.Name == "Set" || methodReference.Name == "Get" || methodReference.Name == "Address" || methodReference.Name == ".ctor")
		{
			return methodReference.DeclaringType.IsArray;
		}
		return false;
	}

	private static TypeReference[] BuildMethodGenericArgs(CecilSourcedAssemblyData assemblyData, Mono.Cecil.MethodReference methodReference)
	{
		if (methodReference is Mono.Cecil.GenericInstanceMethod genericInstanceMethod)
		{
			return BuildGenericArgumentArray(assemblyData, genericInstanceMethod);
		}
		if (methodReference.HasGenericParameters)
		{
			TypeReference[] resolvedArguments = new TypeReference[methodReference.GenericParameters.Count];
			for (int i = 0; i < resolvedArguments.Length; i++)
			{
				resolvedArguments[i] = assemblyData.ResolveReference(methodReference.GenericParameters[i], methodReference.DeclaringType as Mono.Cecil.GenericInstanceType);
			}
			return resolvedArguments;
		}
		return Array.Empty<TypeReference>();
	}

	private static TypeReference[] BuildTypeGenericArgs(CecilSourcedAssemblyData assemblyData, Mono.Cecil.MethodReference methodReference)
	{
		if (methodReference.DeclaringType is Mono.Cecil.GenericInstanceType genericInstanceType)
		{
			return BuildGenericArgumentArray(assemblyData, genericInstanceType);
		}
		return Array.Empty<TypeReference>();
	}

	private static TypeReference[] BuildGenericArgumentArray(CecilSourcedAssemblyData assemblyData, Mono.Cecil.IGenericInstance genericInstance)
	{
		TypeReference[] resolvedArguments = new TypeReference[genericInstance.GenericArguments.Count];
		for (int i = 0; i < resolvedArguments.Length; i++)
		{
			resolvedArguments[i] = assemblyData.ResolveReference(genericInstance.GenericArguments[i], genericInstance);
		}
		return resolvedArguments;
	}
}
