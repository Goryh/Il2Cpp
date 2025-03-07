using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.Awesome;

public static class VirtualMethodResolution
{
	public static bool MethodSignaturesMatch(MethodReference candidate, MethodReference method, ITypeFactory typeFactory)
	{
		if (candidate.HasThis != method.HasThis)
		{
			return false;
		}
		return MethodSignaturesMatchIgnoreStaticness(candidate, method, typeFactory);
	}

	public static bool MethodSignaturesMatchIgnoreStaticness(MethodReference candidate, MethodReference method, ITypeFactory typeFactory)
	{
		if (candidate.Parameters.Count != method.Parameters.Count)
		{
			return false;
		}
		if (candidate.GenericParameters.Count != method.GenericParameters.Count)
		{
			return false;
		}
		if (!TypeReferenceSignatureEqualityComparer.AreEqual(candidate.GetResolvedReturnType(typeFactory), method.GetResolvedReturnType(typeFactory), TypeComparisonMode.SignatureOnly))
		{
			return false;
		}
		ReadOnlyCollection<ParameterDefinition> candidateParameters = candidate.GetResolvedParameters(typeFactory);
		ReadOnlyCollection<ParameterDefinition> methodParameters = method.GetResolvedParameters(typeFactory);
		for (int i = 0; i < candidateParameters.Count; i++)
		{
			if (!TypeReferenceSignatureEqualityComparer.AreEqual(candidateParameters[i].ParameterType, methodParameters[i].ParameterType, TypeComparisonMode.SignatureOnly))
			{
				return false;
			}
		}
		return true;
	}
}
