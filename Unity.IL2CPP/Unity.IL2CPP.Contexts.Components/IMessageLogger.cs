using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Components;

public interface IMessageLogger
{
	void LogWarning(string message);

	void LogWarning(MethodReference aboutMethod, string message)
	{
		LogWarning($"[{aboutMethod.Module.Assembly.Name.Name}] - {aboutMethod} - {message}");
	}

	void LogWarning(TypeReference aboutType, string message)
	{
		LogWarning($"[{aboutType.Module.Assembly.Name.Name}] - {aboutType} - {message}");
	}
}
