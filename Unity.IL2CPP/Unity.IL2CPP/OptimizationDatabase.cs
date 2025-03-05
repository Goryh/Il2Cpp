using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal static class OptimizationDatabase
{
	public static string[] GetPlatformsWithDisabledOptimizations(MethodReference method)
	{
		if (method == null)
		{
			return null;
		}
		if (method.Name == ".cctor" && method.DeclaringType.Name == "MSCompatUnicodeTable" && method.DeclaringType.Namespace == "Mono.Globalization.Unicode")
		{
			return new string[1] { "IL2CPP_TARGET_XBOXONE" };
		}
		if (method.Name == "GrabLongs" && method.DeclaringType.Name == "ParseNumbers" && method.DeclaringType.Namespace == "System")
		{
			return new string[1] { "(IL2CPP_TARGET_WINDOWS && IL2CPP_TARGET_ARMV7)" };
		}
		if (method.Name == "Max" && method.DeclaringType.Name == "Math" && method.DeclaringType.Namespace == "System" && method.ReturnType.Name == "Decimal")
		{
			return new string[1] { "IL2CPP_TARGET_WINDOWS && IL2CPP_TARGET_X64" };
		}
		return null;
	}
}
