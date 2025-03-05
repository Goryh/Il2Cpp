using NiceIO;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Services;

public interface IPathFactoryService
{
	string GetFileNameForAssembly(AssemblyDefinition assembly, string fileName);

	NPath GetFilePath(FileCategory category, NPath filePath);
}
