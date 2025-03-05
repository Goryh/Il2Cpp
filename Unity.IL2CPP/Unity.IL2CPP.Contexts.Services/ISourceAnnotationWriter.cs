using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Services;

public interface ISourceAnnotationWriter
{
	void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint);
}
