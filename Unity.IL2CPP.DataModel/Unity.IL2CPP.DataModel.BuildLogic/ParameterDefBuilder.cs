using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal static class ParameterDefBuilder
{
	public static ReadOnlyCollection<ParameterDefinition> BuildInitializedParameters<T>(Mono.Cecil.IMethodSignature methodSignature, T resolveParam, Func<T, Mono.Cecil.TypeReference, TypeReference> resolve)
	{
		if (!methodSignature.HasParameters)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		Mono.Cecil.IMethodSignature methodDef = methodSignature;
		if (methodDef is Mono.Cecil.MethodReference methodReference)
		{
			Mono.Cecil.IMethodSignature methodSignature2 = methodReference.Resolve();
			methodDef = methodSignature2 ?? methodDef;
		}
		List<ParameterDefinition> paramDefs = new List<ParameterDefinition>(methodSignature.Parameters.Count);
		for (int i = 0; i < methodSignature.Parameters.Count; i++)
		{
			Mono.Cecil.ParameterDefinition sigParam = methodSignature.Parameters[i];
			Mono.Cecil.ParameterDefinition parameterDefinition = methodDef.Parameters[i];
			ParameterDefinition paramDef = new ParameterDefinition(parameterDefinition, DefinitionModelBuilder.BuildCustomAttrs(parameterDefinition), null);
			paramDef.InitializeParameterType(resolve(resolveParam, sigParam.ParameterType));
			paramDefs.Add(paramDef);
		}
		return paramDefs.AsReadOnly();
	}

	public static ReadOnlyCollection<ParameterDefinition> BuildInitializedParameters(IMethodSignature methodSignature)
	{
		if (!methodSignature.HasParameters)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		ReadOnlyCollection<ParameterDefinition> parameters = methodSignature.Parameters;
		List<ParameterDefinition> paramDefs = new List<ParameterDefinition>(parameters.Count);
		for (int i = 0; i < parameters.Count; i++)
		{
			ParameterDefinition sigParam = parameters[i];
			ParameterDefinition paramDef = new ParameterDefinition(sigParam, MetadataToken.ParamZero);
			paramDef.InitializeParameterType(sigParam.ParameterType);
			paramDefs.Add(paramDef);
		}
		return paramDefs.AsReadOnly();
	}

	public static ReadOnlyCollection<ParameterDefinition> BuildInitializedParameters(ITypeFactory typeFactory, MethodReference methodReference)
	{
		if (!methodReference.HasParameters)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		ReadOnlyCollection<ParameterDefinition> parameters = methodReference.Parameters;
		List<ParameterDefinition> paramDefs = new List<ParameterDefinition>(parameters.Count);
		for (int i = 0; i < parameters.Count; i++)
		{
			ParameterDefinition sigParam = parameters[i];
			ParameterDefinition paramDef = new ParameterDefinition(sigParam, MetadataToken.ParamZero);
			paramDef.InitializeParameterType(GenericParameterResolver.ResolveParameterTypeIfNeeded(typeFactory, methodReference, sigParam));
			paramDefs.Add(paramDef);
		}
		return paramDefs.AsReadOnly();
	}

	public static ReadOnlyCollection<ParameterDefinition> BuildParametersForDefinition(Mono.Cecil.MethodDefinition method)
	{
		if (!method.HasParameters)
		{
			return ReadOnlyCollectionCache<ParameterDefinition>.Empty;
		}
		List<ParameterDefinition> paramBuilder = new List<ParameterDefinition>(method.Parameters.Count);
		foreach (Mono.Cecil.ParameterDefinition parameter in method.Parameters)
		{
			paramBuilder.Add(new ParameterDefinition(parameter, DefinitionModelBuilder.BuildCustomAttrs(parameter), DefinitionModelBuilder.BuildMarshalInfo(parameter)));
		}
		return paramBuilder.AsReadOnly();
	}
}
