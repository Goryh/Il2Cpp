using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters;

public interface IGeneratedCodeWriter : IReadOnlyContextGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDirectWriter
{
	new SourceWritingContext Context { get; }
}
