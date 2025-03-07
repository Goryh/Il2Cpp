using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public static class CustomAttributeSupport
{
	public static bool ShouldProcess(Mono.Cecil.CustomAttribute customAttribute)
	{
		return customAttribute.AttributeType.Resolve() != null;
	}
}
