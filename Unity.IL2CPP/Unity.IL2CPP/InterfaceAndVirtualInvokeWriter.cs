using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP;

internal static class InterfaceAndVirtualInvokeWriter
{
	internal static void WriteGenericInterface(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.GenericInterface, delegate
		{
			writer.WriteLine("VirtualInvokeData invokeData;");
			writer.WriteLine("il2cpp_codegen_get_generic_interface_invoke_data(method, obj, &invokeData);");
		}, () => new string[2] { "const RuntimeMethod* method", "RuntimeObject* obj" }, InvokeDataMethodPtr(writer.Context), InvokeDataInvokerCallStart());
	}

	internal static void WriteInterface(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.Interface, delegate
		{
			writer.WriteLine("const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);");
		}, () => new string[3] { "Il2CppMethodSlot slot", "RuntimeClass* declaringInterface", "RuntimeObject* obj" }, InvokeDataMethodPtr(writer.Context), InvokeDataInvokerCallStart());
	}

	internal static void WriteGenericVirtual(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.GenericVirtual, delegate
		{
			writer.WriteLine("VirtualInvokeData invokeData;");
			writer.WriteLine("il2cpp_codegen_get_generic_virtual_invoke_data(method, obj, &invokeData);");
		}, () => new string[2] { "const RuntimeMethod* method", "RuntimeObject* obj" }, InvokeDataMethodPtr(writer.Context), InvokeDataInvokerCallStart());
	}

	internal static void WriteVirtual(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.Virtual, delegate
		{
			writer.WriteLine("const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);");
		}, () => new string[2] { "Il2CppMethodSlot slot", "RuntimeObject* obj" }, InvokeDataMethodPtr(writer.Context), InvokeDataInvokerCallStart());
	}

	internal static void WriteInvokerCall(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.InvokerCall, delegate
		{
		}, () => new string[1] { "Il2CppMethodPointer methodPtr, const RuntimeMethod* method, void* obj" }, delegate
		{
			throw new NotSupportedException("Only invoker calls are supported");
		}, () => "method->invoker_method(methodPtr, method");
	}

	internal static void WriteConstrainedCall(ICodeWriter writer, InvokerData[] invokerGroup)
	{
		Write(writer, invokerGroup, VirtualMethodCallType.ConstrainedInvokerCall, delegate
		{
		}, () => new string[1] { "RuntimeClass* type, const RuntimeMethod* constrainedMethod, void* boxBuffer, void* obj" }, delegate
		{
			throw new NotSupportedException("Only invoker calls are supported");
		}, () => "il2cpp_codegen_runtime_constrained_call(type, constrainedMethod, boxBuffer");
	}

	private static Func<string> InvokeDataMethodPtr(ReadOnlyContext context)
	{
		return () => "invokeData.methodPtr";
	}

	private static Func<string> InvokeDataInvokerCallStart()
	{
		return () => "invokeData.method->invoker_method(il2cpp_codegen_get_method_pointer(invokeData.method), invokeData.method";
	}

	private static void Write(ICodeWriter writer, InvokerData[] invokerGroup, VirtualMethodCallType callType, Action writeRetrieveInvokeData, Func<string[]> getInvokeArgs, Func<string> getMethodPointer, Func<string> getInvokerCallStart)
	{
		invokerGroup = invokerGroup.OrderBy((InvokerData d) => d.Parameters.Count((InvokerParameterData p) => p.SpecializeAsPointerType)).ToArray();
		if (invokerGroup[0].Parameters.Any((InvokerParameterData p) => p.SpecializeAsPointerType))
		{
			Write(writer, invokerGroup[0], callType, writeRetrieveInvokeData, getInvokeArgs, getMethodPointer, getInvokerCallStart, writeNonSpecializedDefinitionOnly: true);
		}
		for (int i = 0; i < invokerGroup.Length; i++)
		{
			Write(writer, invokerGroup[i], callType, writeRetrieveInvokeData, getInvokeArgs, getMethodPointer, getInvokerCallStart, writeNonSpecializedDefinitionOnly: false);
		}
	}

	private static void Write(ICodeWriter writer, InvokerData data, VirtualMethodCallType callType, Action writeRetrieveInvokeData, Func<string[]> getInvokeArgs, Func<string> getMethodPointer, Func<string> getInvokerCallStart, bool writeNonSpecializedDefinitionOnly)
	{
		string actionOrFunc = (data.VoidReturn ? "Action" : "Func");
		string templateContents = TemplateParametersFor(writer.Context, data);
		ICodeWriter codeWriter;
		if (!string.IsNullOrEmpty(templateContents))
		{
			codeWriter = writer;
			codeWriter.WriteLine($"template <{templateContents}>");
		}
		codeWriter = writer;
		codeWriter.Write($"struct {InvokerData.FormatInvokerName(callType, data.Parameters.Count, !data.VoidReturn, data.DoCallViaInvoker)}");
		if (writeNonSpecializedDefinitionOnly)
		{
			writer.WriteLine(";");
			return;
		}
		if (data.Parameters.Any((InvokerParameterData p) => p.SpecializeAsPointerType))
		{
			codeWriter = writer;
			codeWriter.WriteLine($"<{TemplateSpecializationFor(writer.Context, data)}>");
		}
		else
		{
			writer.WriteLine();
		}
		writer.BeginBlock();
		if (!data.DoCallViaInvoker)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"typedef {ReturnTypeFor(data)} (*{actionOrFunc})({FunctionPointerParametersFor(writer.Context, data)});");
			writer.WriteLine();
		}
		string invokerArgs = getInvokeArgs().Concat(data.Parameters.Select((InvokerParameterData m, int i) => string.Format("T{0}{1} p{0}", i + 1, m.SpecializeAsPointerType ? "*" : ""))).AggregateWithComma(writer.Context);
		codeWriter = writer;
		codeWriter.WriteLine($"static inline {ReturnTypeFor(data)} Invoke ({invokerArgs})");
		writer.BeginBlock();
		writeRetrieveInvokeData();
		if (data.DoCallViaInvoker)
		{
			string returnArg = "NULL";
			string paramsArg = "NULL";
			if (!data.VoidReturn)
			{
				writer.WriteLine("R ret;");
				returnArg = "&ret";
			}
			else if (!writer.Context.Global.Parameters.DisableFullGenericSharing && data.Parameters.Count > 0)
			{
				returnArg = $"params[{data.Parameters.Count - 1}]";
			}
			if (data.Parameters.Count > 0)
			{
				codeWriter = writer;
				codeWriter.WriteLine($"void* params[{data.Parameters.Count}] = {{ {data.Parameters.Select((InvokerParameterData p, int i) => $"{(p.SpecializeAsPointerType ? "" : "&")}p{i + 1}").AggregateWithComma(writer.Context)} }};");
				paramsArg = "params";
			}
			codeWriter = writer;
			codeWriter.WriteLine($"{getInvokerCallStart()}, obj, {paramsArg}, {returnArg});");
			if (!data.VoidReturn)
			{
				writer.WriteLine("return ret;");
			}
		}
		else
		{
			codeWriter = writer;
			codeWriter.WriteLine($"{(data.VoidReturn ? "" : "return ")}(({actionOrFunc}){getMethodPointer()})({CallParametersFor(writer.Context, data)});");
		}
		writer.EndBlock();
		writer.EndBlock(semicolon: true);
	}

	private static string ReturnTypeFor(InvokerData data)
	{
		if (!data.VoidReturn)
		{
			return "R";
		}
		return "void";
	}

	private static string TemplateParametersFor(ReadOnlyContext context, InvokerData data)
	{
		List<string> templateParameters = new List<string>(data.Parameters.Count + 1);
		if (!data.VoidReturn)
		{
			templateParameters.Add("typename R");
		}
		templateParameters.AddRange(data.Parameters.Select((InvokerParameterData p, int i) => $"typename T{i + 1}"));
		return templateParameters.AggregateWithComma(context);
	}

	private static string TemplateSpecializationFor(ReadOnlyContext context, InvokerData data)
	{
		List<string> templateParameters = new List<string>(data.Parameters.Count + 1);
		if (!data.VoidReturn)
		{
			templateParameters.Add("R");
		}
		templateParameters.AddRange(data.Parameters.Select((InvokerParameterData p, int i) => $"T{i + 1}{(p.SpecializeAsPointerType ? "*" : "")}"));
		return templateParameters.AggregateWithComma(context);
	}

	private static string CallParametersFor(ReadOnlyContext context, InvokerData data)
	{
		return new string[1] { "obj" }.Concat(Enumerable.Range(1, data.Parameters.Count).Select((int m, int i) => $"p{i + 1}")).Concat(new string[1] { "invokeData.method" }).AggregateWithComma(context);
	}

	private static string FunctionPointerParametersFor(ReadOnlyContext context, InvokerData data)
	{
		return new string[1] { "void*" }.Concat(Enumerable.Range(1, data.Parameters.Count).Select((int m, int i) => $"T{i + 1}")).Concat(new string[1] { "const RuntimeMethod*" }).AggregateWithComma(context);
	}
}
