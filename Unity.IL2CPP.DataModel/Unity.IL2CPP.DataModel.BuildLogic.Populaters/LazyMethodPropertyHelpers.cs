using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.BuildLogic.Populaters;

internal static class LazyMethodPropertyHelpers
{
	public static bool ContainsGenericParameters(GenericInstanceMethod method)
	{
		if (!MethodRefContainsGenericParameter(method))
		{
			return ReferencePopulater.HasGenericParameterInGenericArguments(method);
		}
		return true;
	}

	public static bool ContainsFullGenericSharedTypes(GenericInstanceMethod method)
	{
		if (!method.DeclaringType.ContainsFullySharedGenericTypes)
		{
			return ReferencePopulater.ContainsFullGenericSharingTypes(method);
		}
		return true;
	}

	private static bool MethodRefContainsGenericParameter(MethodReference method)
	{
		if (method.DeclaringType != null)
		{
			return method.DeclaringType.ContainsGenericParameter;
		}
		return false;
	}

	public static bool HasFullGenericSharingSignature(ITypeFactory typeFactory, MethodReference method)
	{
		if (method.ContainsGenericParameter)
		{
			return false;
		}
		if (!method.CanShare(typeFactory))
		{
			return false;
		}
		MethodReference sharedMethod = method.GetSharedMethod(typeFactory);
		MethodReference fullySharedMethod = method.Resolve().FullySharedMethod;
		if (sharedMethod != fullySharedMethod)
		{
			return false;
		}
		if (method != fullySharedMethod && !fullySharedMethod.HasFullGenericSharingSignature(typeFactory))
		{
			return false;
		}
		if (IsVariableSized(typeFactory, fullySharedMethod.GetResolvedReturnType(typeFactory)))
		{
			return true;
		}
		ReadOnlyCollection<ParameterDefinition> fullySharedParameters = fullySharedMethod.GetResolvedParameters(typeFactory);
		ReadOnlyCollection<ParameterDefinition> parameters = fullySharedMethod.GetResolvedParameters(typeFactory);
		for (int i = 0; i < parameters.Count; i++)
		{
			if (IsVariableSized(typeFactory, fullySharedParameters[i].ParameterType) && !CanTypeBePassedUnmodifiedToAnFullGenericSharingMethod(typeFactory, parameters[i].ParameterType))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsVariableSized(ITypeFactory typeFactory, TypeReference typeReference)
	{
		RuntimeStorageKind runtimeStorage = typeReference.GetRuntimeStorage(typeFactory);
		if (runtimeStorage != RuntimeStorageKind.VariableSizedAny)
		{
			return runtimeStorage == RuntimeStorageKind.VariableSizedValueType;
		}
		return true;
	}

	private static bool CanTypeBePassedUnmodifiedToAnFullGenericSharingMethod(ITypeFactory typeFactory, TypeReference typeReference)
	{
		RuntimeStorageKind runtimeStorage = typeReference.GetRuntimeStorage(typeFactory);
		if (runtimeStorage != RuntimeStorageKind.Pointer)
		{
			return runtimeStorage == RuntimeStorageKind.ReferenceType;
		}
		return true;
	}
}
