using NiceIO;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.Common;

namespace il2cpp;

internal static class Extensions
{
	public static NPath DetermineDistributionDirectory(this Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		return il2CppCommandLineArguments.CustomIl2CppRoot ?? ((string)CommonPaths.Il2CppRoot);
	}
}
