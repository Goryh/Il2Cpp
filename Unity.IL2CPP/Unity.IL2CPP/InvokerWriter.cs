using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

internal class InvokerWriter
{
	public static void WriteInvokerBody(ICodeWriter writer, bool hasThis, IList<TypeReference> data, TypeReference returnType)
	{
		List<string> parameters = new List<string>(data.Count + 2);
		if (hasThis)
		{
			parameters.Add("void* obj");
		}
		bool returnAsByRef = returnType.IsReturnedByRef(writer.Context);
		for (int index = 1; index < data.Count; index++)
		{
			parameters.Add(data[index].CppNameForVariable + " p" + index);
		}
		if (returnType.IsNotVoid && returnAsByRef)
		{
			parameters.Add(returnType.CppNameForPointerToVariable + " il2cppRetVal");
		}
		parameters.Add("const RuntimeMethod* method");
		ICodeWriter codeWriter = writer;
		codeWriter.WriteStatement($"typedef {MethodSignatureWriter.FormatReturnType(writer.Context, returnType)} (*Func)({parameters.AggregateWithComma(writer.Context)})");
		List<string> arguments = new List<string>(data.Count + 2);
		if (hasThis)
		{
			arguments.Add("obj");
		}
		for (int i = 1; i < data.Count; i++)
		{
			int parameterIndex = i - 1;
			string parameterName = $"args[{parameterIndex}]";
			if (data[i].GetRuntimeStorage(writer.Context).IsVariableSized())
			{
				string parameterTypeName = data[i].CppNameForVariable;
				string varParameterName = $"var_param_{parameterIndex}";
				string varParameterRuntimeTypeName = $"var_param_type_{parameterIndex}";
				string sizeName = $"size_param_{parameterIndex}";
				codeWriter = writer;
				codeWriter.WriteLine($"const RuntimeType* {varParameterRuntimeTypeName} = il2cpp_codegen_method_parameter_type(methodMetadata, {parameterIndex});");
				codeWriter = writer;
				codeWriter.WriteLine($"{parameterTypeName} {varParameterName};");
				codeWriter = writer;
				codeWriter.WriteLine($"if (il2cpp_codegen_type_is_value_type({varParameterRuntimeTypeName}))");
				using (new BlockWriter(writer))
				{
					codeWriter = writer;
					codeWriter.WriteLine($"uint32_t {sizeName} = il2cpp_codegen_sizeof(il2cpp_codegen_class_from_type({varParameterRuntimeTypeName}));");
					codeWriter = writer;
					codeWriter.WriteLine($"{varParameterName} = alloca({sizeName});");
					codeWriter = writer;
					codeWriter.WriteLine($"il2cpp_codegen_memcpy({varParameterName}, {parameterName}, {sizeName});");
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					codeWriter = writer;
					codeWriter.WriteLine($"{varParameterName} = {parameterName};");
				}
				parameterName = varParameterName;
			}
			arguments.Add(LoadParameter(writer.Context, data[i], parameterName, parameterIndex));
		}
		if (returnType.IsNotVoid && returnAsByRef)
		{
			arguments.Add("(" + returnType.CppNameForPointerToVariable + ")returnAddress");
		}
		arguments.Add("methodMetadata");
		if (returnType.IsNotVoid && !returnAsByRef)
		{
			codeWriter = writer;
			codeWriter.Write($"*(({returnType.CppNameForPointerToVariable})returnAddress) = ");
		}
		codeWriter = writer;
		codeWriter.WriteLine($"((Func)methodPointer)({arguments.AggregateWithComma(writer.Context)});");
	}

	internal static string LoadParameter(ReadOnlyContext context, TypeReference type, string param, int index)
	{
		type = type.WithoutModifiers();
		if (type.IsByReference)
		{
			return Emit.Cast(context, type, param);
		}
		if (type.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable)
		{
			return Emit.Cast(context, type, param);
		}
		if (type.MetadataType == MetadataType.SByte || type.MetadataType == MetadataType.Byte || type.MetadataType == MetadataType.Boolean || type.MetadataType == MetadataType.Int16 || type.MetadataType == MetadataType.UInt16 || type.MetadataType == MetadataType.Char || type.MetadataType == MetadataType.Int32 || type.MetadataType == MetadataType.UInt32 || type.MetadataType == MetadataType.Int64 || type.MetadataType == MetadataType.UInt64 || type.MetadataType == MetadataType.IntPtr || type.MetadataType == MetadataType.UIntPtr || type.MetadataType == MetadataType.Single || type.MetadataType == MetadataType.Double)
		{
			return $"*(({type.CppNameForPointerToVariable}){param})";
		}
		if ((type.MetadataType == MetadataType.String || type.MetadataType == MetadataType.Class || type.MetadataType == MetadataType.Array || type.MetadataType == MetadataType.Pointer || type.MetadataType == MetadataType.Object) && !type.IsValueType)
		{
			return Emit.Cast(context, type, param);
		}
		if (type.MetadataType == MetadataType.GenericInstance && !type.IsValueType)
		{
			return Emit.Cast(context, type, param);
		}
		if (!type.IsValueType)
		{
			throw new Exception();
		}
		if (type.IsEnum)
		{
			return LoadParameter(context, type.GetUnderlyingEnumType(), param, index);
		}
		return $"*(({type.CppNameForPointerToVariable}){param})";
	}
}
