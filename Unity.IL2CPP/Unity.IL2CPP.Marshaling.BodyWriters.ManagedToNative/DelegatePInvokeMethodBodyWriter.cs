using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class DelegatePInvokeMethodBodyWriter : PInvokeMethodBodyWriter
{
	public DelegatePInvokeMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod)
		: base(context, interopMethod)
	{
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		INamingService naming = writer.Context.Global.Services.Naming;
		string nativeMethodReturnType = MarshaledReturnType.DecoratedName;
		string fnPtrTypeDef = naming.ForPInvokeFunctionPointerTypedef();
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"typedef {nativeMethodReturnType} ({InteropMethodBodyWriter.GetDelegateCallingConvention(_methodDefinition.DeclaringType)} *{naming.ForPInvokeFunctionPointerTypedef()})({FormatParametersForTypedef()});");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{fnPtrTypeDef} {naming.ForPInvokeFunctionPointerVariable()} = reinterpret_cast<{fnPtrTypeDef}>(il2cpp_codegen_get_reverse_pinvoke_function_ptr({"__this"}));");
	}

	public static bool IsDelegatePInvokeWrapperNecessaryQuickChecks(ReadOnlyContext context, in LazilyInflatedMethod method)
	{
		if (method.Definition.Name != "Invoke")
		{
			return false;
		}
		MethodDefinition methodDefinition = method.Definition;
		TypeDefinition typeDef = methodDefinition.DeclaringType;
		if (!typeDef.IsDelegate)
		{
			return false;
		}
		if (typeDef.HasGenericParameters)
		{
			return false;
		}
		if (methodDefinition.HasGenericParameters)
		{
			return false;
		}
		if (methodDefinition.ReturnType.IsGenericParameter)
		{
			return false;
		}
		if (!methodDefinition.IsRuntime)
		{
			return false;
		}
		if (methodDefinition.ReturnType.IsByReference && !MarshalingUtils.IsBlittable(context, methodDefinition.ReturnType.GetElementType(), null, MarshalType.PInvoke, useUnicodeCharset: false))
		{
			return false;
		}
		return true;
	}

	public static bool IsDelegatePInvokeWrapperNecessary(ReadOnlyContext context, in LazilyInflatedMethod method, out DelegatePInvokeMethodBodyWriter writer)
	{
		writer = null;
		if (IsDelegatePInvokeWrapperNecessaryQuickChecks(context, in method))
		{
			return IsDelegatePInvokeWrapperNecessaryFinalCheck(context, method.InflatedMethod, out writer);
		}
		return false;
	}

	public static bool IsDelegatePInvokeWrapperNecessaryFinalCheck(ReadOnlyContext context, MethodReference method, out DelegatePInvokeMethodBodyWriter writer)
	{
		writer = new DelegatePInvokeMethodBodyWriter(context, method);
		return writer.FirstOrDefaultUnmarshalableMarshalInfoWriter(context) == null;
	}
}
