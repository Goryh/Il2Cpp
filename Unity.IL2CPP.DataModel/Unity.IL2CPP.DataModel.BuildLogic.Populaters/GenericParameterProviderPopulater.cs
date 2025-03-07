using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.DataModel.BuildLogic.Populaters;

internal static class GenericParameterProviderPopulater
{
	public static void InitializeEmpty(IGenericParameterProvider destination)
	{
		((IGenericParamProviderInitializer)destination).InitializeGenericParameters(ReadOnlyCollectionCache<GenericParameter>.Empty);
	}

	public static void InitializeMethodReference(TypeContext context, MethodReference methodReference)
	{
		if (methodReference.IsDefinition)
		{
			throw new ArgumentException("InitializeMethodReference must not be called with a " + methodReference.GetType().Name);
		}
		if (methodReference is MethodRefOnTypeInst)
		{
			Clone(context, methodReference.Resolve(), methodReference);
		}
		else
		{
			InitializeEmpty(methodReference);
		}
	}

	private static void Clone(TypeContext context, IGenericParameterProvider source, IGenericParameterProvider destination)
	{
		if (!source.HasGenericParameters)
		{
			InitializeEmpty(destination);
			return;
		}
		List<GenericParameter> newGenericParameters = new List<GenericParameter>(source.GenericParameters.Count);
		foreach (GenericParameter genericParameter in source.GenericParameters)
		{
			GenericParameter newGp = new GenericParameter(genericParameter, destination, context);
			ReferencePopulater.PopulateTypeRefProperties(newGp);
			newGenericParameters.Add(newGp);
		}
		((IGenericParamProviderInitializer)destination).InitializeGenericParameters(newGenericParameters.AsReadOnly());
	}
}
