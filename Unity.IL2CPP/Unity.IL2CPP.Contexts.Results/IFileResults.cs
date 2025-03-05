using System.Collections.ObjectModel;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Results;

public interface IFileResults
{
	ReadOnlyCollection<NPath> PerAssembly { get; init; }

	ReadOnlyCollection<NPath> Generics { get; init; }

	ReadOnlyCollection<NPath> Metadata { get; init; }

	ReadOnlyCollection<NPath> Debugger { get; init; }

	ReadOnlyCollection<NPath> Other { get; init; }
}
