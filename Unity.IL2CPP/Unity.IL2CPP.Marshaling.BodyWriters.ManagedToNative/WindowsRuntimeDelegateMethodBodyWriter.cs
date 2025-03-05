using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class WindowsRuntimeDelegateMethodBodyWriter : ComMethodBodyWriter
{
	public WindowsRuntimeDelegateMethodBodyWriter(ReadOnlyContext context, MethodReference invokeMethod)
		: base(context, invokeMethod, invokeMethod)
	{
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string interfaceTypeName = writer.Context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_interfaceType);
		string localVariableName = writer.Context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType);
		writer.WriteLine($"{interfaceTypeName}* {localVariableName} = il2cpp_codegen_com_query_interface<{interfaceTypeName}>(static_cast<{"Il2CppComObject*"}>({"__this"}));");
		writer.WriteLine();
	}

	protected override string GetMethodNameInGeneratedCode(ReadOnlyContext context)
	{
		return "Invoke";
	}
}
