namespace Unity.IL2CPP.CodeWriters;

public interface ICppCodeWriter : ICodeWriter, IDirectWriter
{
	void AddInclude(string include);

	void AddForwardDeclaration(string type);

	void AddMethodForwardDeclaration(string declaration);

	void AddStdInclude(string include);
}
