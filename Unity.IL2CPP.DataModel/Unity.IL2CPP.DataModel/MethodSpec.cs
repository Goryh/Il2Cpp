using System.Collections.ObjectModel;
using System.Threading;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public abstract class MethodSpec : MethodReference
{
	private TypeReference _resolvedReturnType;

	private TypeReference _resolvedThisType;

	private ReadOnlyCollection<ParameterDefinition> _resolvedParameters;

	protected MethodSpec(TypeReference declaringType, MethodCallingConvention callingConvention, bool hasThis, bool explicitThis, MetadataToken metadataToken)
		: base(declaringType, callingConvention, hasThis, explicitThis, metadataToken)
	{
	}

	public override ReadOnlyCollection<ParameterDefinition> GetResolvedParameters(ITypeFactory typeFactory)
	{
		if (_resolvedParameters == null)
		{
			Interlocked.CompareExchange(ref _resolvedParameters, ParameterDefBuilder.BuildInitializedParameters(typeFactory, this), null);
		}
		return _resolvedParameters;
	}

	public override TypeReference GetResolvedReturnType(ITypeFactory typeFactory)
	{
		if (_resolvedReturnType == null)
		{
			Interlocked.CompareExchange(ref _resolvedReturnType, GenericParameterResolver.ResolveReturnTypeIfNeeded(typeFactory, this), null);
		}
		return _resolvedReturnType;
	}

	public override TypeReference GetResolvedThisType(ITypeFactory typeFactory)
	{
		if (_resolvedThisType == null)
		{
			Interlocked.CompareExchange(ref _resolvedThisType, GenericParameterResolver.ResolveThisTypeIfNeeded(typeFactory, this), null);
		}
		return _resolvedThisType;
	}
}
