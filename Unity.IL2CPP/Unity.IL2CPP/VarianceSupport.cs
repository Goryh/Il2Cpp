using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public sealed class VarianceSupport
{
	public static bool IsNeededForConversion(TypeReference leftType, ResolvedTypeInfo rightType)
	{
		return IsNeededForConversion(leftType, rightType.ResolvedType);
	}

	public static bool IsNeededForConversion(TypeReference leftType, TypeReference rightType)
	{
		leftType = leftType.WithoutModifiers();
		rightType = rightType.WithoutModifiers();
		if (leftType.IsFunctionPointer || rightType.IsFunctionPointer)
		{
			return false;
		}
		if (leftType.IsByReference && rightType.IsPointer && leftType != rightType)
		{
			return true;
		}
		if (leftType.IsByReference || rightType.IsByReference)
		{
			return false;
		}
		if (leftType == rightType)
		{
			return false;
		}
		if (leftType.IsDelegate && rightType.IsDelegate)
		{
			return true;
		}
		if (!leftType.IsArray)
		{
			return rightType.IsArray;
		}
		return true;
	}

	public static string Apply(ReadOnlyContext context, TypeReference leftType, ResolvedTypeInfo rightType)
	{
		return Apply(context, leftType, rightType.ResolvedType);
	}

	public static string Apply(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
	{
		if (leftType == rightType)
		{
			return string.Empty;
		}
		return "(" + leftType.CppNameForVariable + ")";
	}
}
