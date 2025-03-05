using System.Collections.ObjectModel;

namespace Unity.IL2CPP.CodeWriters;

public interface IGeneratedMethodCodeWriter : IGeneratedCodeWriter, IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter
{
	bool ErrorOccurred { get; set; }

	ReadOnlyDictionary<string, MethodMetadataUsage> MethodMetadataUsages { get; }

	void AddMetadataUsage(string identifier, MethodMetadataUsage usage);

	void Write(IGeneratedMethodCodeStream other);
}
