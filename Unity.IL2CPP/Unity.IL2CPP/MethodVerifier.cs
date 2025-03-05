using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal static class MethodVerifier
{
	public static bool IsNonGenericMethodThatDoesntExist(MethodReference method)
	{
		if (!method.IsGenericInstance && !method.DeclaringType.IsGenericInstance)
		{
			TypeDefinition typeDefinition = method.DeclaringType.Resolve();
			MethodDefinition resolvedOriginalMethod = method.Resolve();
			return typeDefinition != resolvedOriginalMethod.DeclaringType;
		}
		return false;
	}
}
