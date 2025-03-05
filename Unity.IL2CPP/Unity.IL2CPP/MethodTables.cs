using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public sealed class MethodTables
{
	public static bool MethodNeedsTable(ReadOnlyContext context, Il2CppMethodSpec method)
	{
		MethodReference methodReference = method.GenericMethod;
		if (!methodReference.HasGenericParameters && !methodReference.DeclaringType.HasGenericParameters && !methodReference.ContainsGenericParameter)
		{
			if (methodReference.CanShare(context))
			{
				return methodReference.GetSharedMethod(context) == methodReference;
			}
			return true;
		}
		return false;
	}

	internal static string MethodPointerNameFor(ReadOnlyContext context, MethodReference method)
	{
		if (MethodPointerIsNull(context, method))
		{
			return "NULL";
		}
		if (method.IsUnmanagedCallersOnly)
		{
			return "NULL";
		}
		if (!method.CanShare(context))
		{
			return method.CppName;
		}
		return method.CppName + "_gshared";
	}

	internal static bool MethodPointerIsNull(ReadOnlyContext context, MethodReference method)
	{
		if (MethodWriter.IsGetOrSetGenericValueOnArray(method))
		{
			return true;
		}
		if (GenericsUtilities.IsGenericInstanceOfCompareExchange(method))
		{
			return true;
		}
		if (GenericsUtilities.IsGenericInstanceOfExchange(method))
		{
			return true;
		}
		if (!MethodWriter.MethodCanBeDirectlyCalled(context, method))
		{
			return true;
		}
		return false;
	}

	internal static string AdjustorThunkNameFor(ReadOnlyContext context, MethodReference method)
	{
		if (method.CanShare(context))
		{
			method = method.GetSharedMethod(context);
			if (MethodWriter.HasAdjustorThunk(method))
			{
				return method.NameForAdjustorThunk();
			}
			return "NULL";
		}
		if (MethodWriter.HasAdjustorThunk(method))
		{
			return method.NameForAdjustorThunk();
		}
		return "NULL";
	}
}
