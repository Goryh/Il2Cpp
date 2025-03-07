using System.Collections.ObjectModel;
using System.Threading;
using Mono.Cecil;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel;

public class CallSite : IMethodSignature
{
	private string _fullName;

	public bool HasThis { get; }

	public bool ExplicitThis { get; }

	public bool HasParameters => Parameters.Count > 0;

	public MethodCallingConvention CallingConvention { get; }

	public TypeReference ReturnType { get; }

	public ReadOnlyCollection<ParameterDefinition> Parameters { get; }

	public string FullName
	{
		get
		{
			if (_fullName == null)
			{
				Interlocked.CompareExchange(ref _fullName, LazyNameHelpers.GetFullName(this), null);
			}
			return _fullName;
		}
	}

	public CallSite(Mono.Cecil.CallSite callSite, TypeReference returnType, ReadOnlyCollection<ParameterDefinition> parameters)
	{
		HasThis = callSite.HasThis;
		ExplicitThis = callSite.ExplicitThis;
		CallingConvention = (MethodCallingConvention)callSite.CallingConvention;
		ReturnType = returnType;
		Parameters = parameters;
	}

	public TypeReference GetResolvedReturnType(ITypeFactory typeFactory)
	{
		return ReturnType;
	}

	public ReadOnlyCollection<ParameterDefinition> GetResolvedParameters(ITypeFactory typeFactory)
	{
		return Parameters;
	}

	public override string ToString()
	{
		return FullName;
	}

	internal void InitializeFullName(string fullName)
	{
		_fullName = fullName;
	}
}
