using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.MethodWriting;

public static class BaseTypeHelper
{
	public static TypeReference GetFirstNonVariableLayoutBaseType(ReadOnlyContext context, TypeReference typeReference)
	{
		for (TypeReference baseType = typeReference.GetBaseType(context); baseType != null; baseType = baseType.GetBaseType(context))
		{
			if (baseType.GetRuntimeFieldLayout(context) != RuntimeFieldLayoutKind.Variable)
			{
				return baseType;
			}
		}
		return typeReference;
	}
}
