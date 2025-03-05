using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.WindowsRuntime;

internal static class NamingExtensions
{
	public static string ForCreateComCallableWrapperFunction(this INamingService naming, TypeReference type)
	{
		return "CreateComCallableWrapperFor_" + type.CppName;
	}

	public static string ForCreateWindowsRuntimeFactoryFunction(this INamingService naming, TypeDefinition type)
	{
		return "CreateWindowsRuntimeFactoryFor_" + type.CppName;
	}

	public static string ForComCallableWrapperClass(this INamingService naming, TypeReference type)
	{
		return type.CppName + "_ComCallableWrapper";
	}

	public static string ForWindowsRuntimeFactory(this INamingService naming, TypeDefinition type)
	{
		return type.CppName + "_Factory";
	}

	public static string ForComCallableWrapperProjectedMethod(this INamingService naming, MethodReference method)
	{
		return method.CppName + "_ComCallableWrapperProjectedMethod";
	}

	public static string ForWindowsRuntimeAdapterClass(this INamingService naming, TypeReference type)
	{
		return type.CppName + "_Adapter";
	}

	public static string ForWindowsRuntimeAdapterTypeName(this INamingService naming, TypeDefinition fromType, TypeDefinition toType)
	{
		string obj = ((fromType != null) ? RemoveBackticks(fromType.Name) : "IInspectable");
		string toTypeName = RemoveBackticks(toType.Name);
		string adapterTypeName = obj + "To" + toTypeName + "Adapter";
		if (toType.HasGenericParameters)
		{
			adapterTypeName = adapterTypeName + "`" + toType.GenericParameters.Count;
		}
		return adapterTypeName;
	}

	public static string ForWindowsRuntimeDelegateNativeInvokerMethod(this INamingService naming, MethodReference invokeMethod)
	{
		return invokeMethod.CppName + "_NativeInvoker";
	}

	public static string ForWindowsRuntimeDelegateComCallableWrapperInterface(this INamingService naming, TypeReference delegateType)
	{
		return "I" + naming.ForComCallableWrapperClass(delegateType);
	}

	private static string RemoveBackticks(string typeName)
	{
		int backtickIndex = typeName.IndexOf('`');
		if (backtickIndex != -1)
		{
			typeName = typeName.Substring(0, backtickIndex);
		}
		return typeName;
	}
}
