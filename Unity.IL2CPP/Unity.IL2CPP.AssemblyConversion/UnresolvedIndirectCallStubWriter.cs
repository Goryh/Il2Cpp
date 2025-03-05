using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion;

internal static class UnresolvedIndirectCallStubWriter
{
	public static UnresolvedIndirectCallsTableInfo WriteUnresolvedStubs(SourceWritingContext context)
	{
		using IGeneratedCodeStream writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppUnresolvedIndirectCallStubs.cpp");
		ReadOnlyCollection<KeyValuePair<IndirectCallSignature, uint>> sortedSignatures = context.Global.Results.SecondaryWritePart1.IndirectCalls.SortedItems;
		foreach (KeyValuePair<IndirectCallSignature, uint> kvp in sortedSignatures)
		{
			IIl2CppRuntimeType[] signature = kvp.Key.Signature;
			IndirectCallUsage usage = kvp.Key.Usage;
			RecordIncludes(writer, signature);
			if (usage.HasFlag(IndirectCallUsage.Virtual))
			{
				writer.WriteLine(GetMethodSignature(writer, MethodNameFor(kvp), signature, context.Global.Services.TypeProvider.SystemObject));
				using (new BlockWriter(writer))
				{
					WriteInvokerCall(writer, signature, "__this", "il2cpp_codegen_get_method_pointer");
				}
			}
			if (usage.HasFlag(IndirectCallUsage.Instance))
			{
				writer.WriteLine(GetMethodSignature(writer, InstanceMethodNameFor(kvp), signature, context.Global.Services.TypeProvider.SystemVoidPointer));
				using (new BlockWriter(writer))
				{
					WriteInvokerCall(writer, signature, "__this", "il2cpp_codegen_get_direct_method_pointer");
				}
			}
			if (usage.HasFlag(IndirectCallUsage.Static))
			{
				writer.WriteLine(GetMethodSignature(writer, StaticMethodNameFor(kvp), signature, null));
				using (new BlockWriter(writer))
				{
					WriteInvokerCall(writer, signature, "NULL", "il2cpp_codegen_get_direct_method_pointer");
				}
			}
		}
		UnresolvedIndirectCallsTableInfo retVal = default(UnresolvedIndirectCallsTableInfo);
		retVal.VirtualMethodPointersInfo = writer.WriteArrayInitializer("const Il2CppMethodPointer", context.Global.Services.ContextScope.ForMetadataGlobalVar("g_UnresolvedVirtualMethodPointers"), sortedSignatures.Select(TableMethodNameFor), externArray: true, nullTerminate: false);
		retVal.InstanceMethodPointersInfo = writer.WriteArrayInitializer("const Il2CppMethodPointer", context.Global.Services.ContextScope.ForMetadataGlobalVar("g_UnresolvedInstanceMethodPointers"), sortedSignatures.Select(TableDirectMethodNameFor), externArray: true, nullTerminate: false);
		retVal.StaticMethodPointersInfo = writer.WriteArrayInitializer("const Il2CppMethodPointer", context.Global.Services.ContextScope.ForMetadataGlobalVar("g_UnresolvedStaticMethodPointers"), sortedSignatures.Select(TableStaticMethodNameFor), externArray: true, nullTerminate: false);
		retVal.SignatureTypes = context.Global.Results.SecondaryWritePart1.IndirectCalls.SortedKeys;
		return retVal;
	}

	private static void WriteInvokerCall(IGeneratedCodeWriter writer, IIl2CppRuntimeType[] signature, string thisValue, string getMethodPointerFunction)
	{
		bool returnsVoid = signature[0].Type.IsVoid;
		bool isReturnedByRef = signature[0].Type.IsReturnedByRef(writer.Context);
		string args = "NULL";
		if (signature.Length > 1)
		{
			args = "args";
			writer.Write("void* args[] = {");
			for (int i = 1; i < signature.Length; i++)
			{
				if (i != 1)
				{
					writer.Write(",");
				}
				writer.Write(FormatArgumentForInvoker(writer.Context, i, signature[i]));
			}
			writer.WriteLine("};");
		}
		string retBuffer = "NULL";
		IGeneratedCodeWriter generatedCodeWriter;
		if (!returnsVoid)
		{
			if (!isReturnedByRef)
			{
				generatedCodeWriter = writer;
				generatedCodeWriter.WriteLine($"{signature[0].Type.CppNameForVariable} {"il2cppRetVal"};");
			}
			retBuffer = "&il2cppRetVal";
		}
		generatedCodeWriter = writer;
		generatedCodeWriter.WriteLine($"method->invoker_method({getMethodPointerFunction}(method), method, {thisValue}, {args}, {retBuffer});");
		if (!returnsVoid && !isReturnedByRef)
		{
			writer.WriteLine("return il2cppRetVal;");
		}
	}

	private static string FormatArgumentForInvoker(ReadOnlyContext context, int i, IIl2CppRuntimeType type)
	{
		if (type.Type.GetRuntimeStorage(context) == RuntimeStorageKind.ValueType)
		{
			return "&" + FormatParameterVarName(i);
		}
		return FormatParameterVarName(i);
	}

	private static string FormatParameterVarName(int i)
	{
		return "p" + i;
	}

	private static string GetMethodSignature(IGeneratedCodeWriter writer, string methodName, IIl2CppRuntimeType[] signature, TypeReference thisParameterType)
	{
		return MethodSignatureWriter.GetMethodSignature(methodName, MethodSignatureWriter.FormatReturnType(writer.Context, signature[0].Type), FormatParameters(writer.Context, signature, thisParameterType), "static");
	}

	private static string FormatParameters(ReadOnlyContext context, IIl2CppRuntimeType[] signature, TypeReference thisParameterType)
	{
		return ParametersFor(context, signature, thisParameterType).AggregateWithComma(context);
	}

	private static IEnumerable<string> ParametersFor(ReadOnlyContext context, IIl2CppRuntimeType[] signature, TypeReference thisParameterType)
	{
		if (thisParameterType != null)
		{
			yield return FormatParameterArgName(thisParameterType, "__this");
		}
		for (int i = 1; i < signature.Length; i++)
		{
			yield return FormatParameterArgName(signature[i].Type, FormatParameterVarName(i));
		}
		if (signature[0].Type.IsReturnedByRef(context))
		{
			TypeReference returnByRef = signature[0].Type.CreatePointerType(context);
			yield return FormatParameterArgName(returnByRef, "il2cppRetVal");
		}
		yield return "const RuntimeMethod* method";
	}

	private static void RecordIncludes(IGeneratedCodeWriter writer, IIl2CppRuntimeType[] signature)
	{
		if (signature[0].Type.IsNotVoid)
		{
			writer.AddIncludesForTypeReference(writer.Context, signature[0].Type);
		}
		for (int i = 1; i < signature.Length; i++)
		{
			writer.AddIncludesForTypeReference(writer.Context, signature[i].Type, requiresCompleteType: true);
		}
	}

	private static string MethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		return "UnresolvedVirtualCall_" + kvp.Value;
	}

	private static string InstanceMethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		return "UnresolvedInstanceCall_" + kvp.Value;
	}

	private static string StaticMethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		return "UnresolvedStaticCall_" + kvp.Value;
	}

	private static string TableMethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		if (!kvp.Key.Usage.HasFlag(IndirectCallUsage.Virtual))
		{
			return "NULL";
		}
		return "(const Il2CppMethodPointer)" + MethodNameFor(kvp);
	}

	private static string TableDirectMethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		if (!kvp.Key.Usage.HasFlag(IndirectCallUsage.Instance))
		{
			return "NULL";
		}
		return "(const Il2CppMethodPointer)" + InstanceMethodNameFor(kvp);
	}

	private static string TableStaticMethodNameFor(KeyValuePair<IndirectCallSignature, uint> kvp)
	{
		if (!kvp.Key.Usage.HasFlag(IndirectCallUsage.Static))
		{
			return "NULL";
		}
		return "(const Il2CppMethodPointer)" + StaticMethodNameFor(kvp);
	}

	private static string FormatParameterArgName(TypeReference parameterType, string parameterName)
	{
		return parameterType.CppNameForVariable + " " + parameterName;
	}
}
