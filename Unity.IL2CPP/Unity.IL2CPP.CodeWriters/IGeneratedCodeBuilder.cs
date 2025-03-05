namespace Unity.IL2CPP.CodeWriters;

public interface IGeneratedCodeBuilder : ICodeBuilder
{
	GeneratedCodeString ToGeneratedCodeStringValue();
}
