using System.Collections.ObjectModel;

namespace Unity.IL2CPP.DataModel;

public interface IMethodSignature
{
	bool HasThis { get; }

	bool ExplicitThis { get; }

	bool HasParameters { get; }

	MethodCallingConvention CallingConvention { get; }

	TypeReference ReturnType { get; }

	ReadOnlyCollection<ParameterDefinition> Parameters { get; }
}
