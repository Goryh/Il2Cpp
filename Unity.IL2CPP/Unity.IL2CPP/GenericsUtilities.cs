using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public static class GenericsUtilities
{
	public static bool WasCollectedForMarshalling(ReadOnlyContext context, TypeReference typeReference)
	{
		if (typeReference is GenericInstanceType genericInstanceType)
		{
			return context.Global.Results.PrimaryCollection.Generics.TypeDeclarations.Contains(genericInstanceType);
		}
		return true;
	}

	public static bool WasCollectedForMarshalling(ReadOnlyContext context, MethodReference methodReference)
	{
		if (methodReference is GenericInstanceMethod genericInstanceMethod)
		{
			return context.Global.Results.PrimaryCollection.Generics.Methods.Contains(genericInstanceMethod);
		}
		return WasCollectedForMarshalling(context, methodReference.DeclaringType);
	}

	public static bool CheckForMaximumRecursion(ReadOnlyContext context, TypeReference genericInstance)
	{
		return RecursiveGenericDepthFor(genericInstance as GenericInstanceType) >= context.Global.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth;
	}

	public static bool CheckForMaximumRecursion(ReadOnlyContext context, MethodReference methodReference)
	{
		if (!(methodReference is GenericInstanceMethod genericInstanceMethod) || RecursiveGenericDepthFor(genericInstanceMethod) < context.Global.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth)
		{
			if (methodReference.DeclaringType is GenericInstanceType genericInstanceType)
			{
				return CheckForMaximumRecursion(context, genericInstanceType);
			}
			return false;
		}
		return true;
	}

	public static int RecursiveGenericDepthFor(IGenericInstance genericInstance)
	{
		return genericInstance?.RecursiveGenericDepth ?? 0;
	}

	public static bool IsGenericInstanceOfCompareExchange(MethodReference methodReference)
	{
		if (methodReference.DeclaringType.Name == "Interlocked" && methodReference.Name == "CompareExchange")
		{
			return methodReference.IsGenericInstance;
		}
		return false;
	}

	public static bool IsGenericInstanceOfExchange(MethodReference methodReference)
	{
		if (methodReference.DeclaringType.Name == "Interlocked" && methodReference.Name == "Exchange")
		{
			return methodReference.IsGenericInstance;
		}
		return false;
	}
}
