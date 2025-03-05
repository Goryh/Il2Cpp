using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class PInvokeMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
{
	protected readonly MethodDefinition _methodDefinition;

	protected readonly PInvokeInfo _pinvokeInfo;

	internal const string FORCE_PINVOKE_INTERNAL = "FORCE_PINVOKE_INTERNAL";

	public static readonly ReadOnlyDictionary<string, ReadOnlyHashSet<string>> InternalizedMethods = new Dictionary<string, ReadOnlyHashSet<string>>
	{
		{
			"MonoPosixHelper",
			new string[8] { "CreateZStream", "CloseZStream", "Flush", "ReadZStream", "WriteZStream", "CreateNLSocket", "ReadEvents", "CloseNLSocket" }.AsReadOnlyHashSet()
		},
		{
			"System.Native",
			new string[39]
			{
				"Stat", "LStat", "Unlink", "GetReadDirRBufferSize", "ReadDirR", "OpenDir", "CloseDir", "MkDir", "ChMod", "Link",
				"ReadLink", "Symlink", "Rename", "RmDir", "CopyFile", "LChflags", "LChflagsCanSetHiddenFlag", "ConvertErrorPlatformToPal", "ConvertErrorPalToPlatform", "mono_pal_init",
				"UTime", "FStat", "StrErrorR", "GetNonCryptographicallySecureRandomBytes", "UTimes", "GetEUid", "GetEGid", "GetDomainSocketSizes", "BrotliDecoderCreateInstance", "BrotliDecoderDecompressStream",
				"BrotliDecoderDecompress", "BrotliDecoderDestroyInstance", "BrotliDecoderIsFinished", "BrotliEncoderCreateInstance", "BrotliEncoderSetParameter", "BrotliEncoderCompressStream", "BrotliEncoderHasMoreOutput", "BrotliEncoderDestroyInstance", "BrotliEncoderCompress"
			}.AsReadOnlyHashSet()
		},
		{
			"System.Globalization.Native",
			new string[1] { "GetTimeZoneDisplayName" }.AsReadOnlyHashSet()
		}
	}.AsReadOnly();

	public PInvokeMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod)
		: base(context, interopMethod, interopMethod, MarshalType.PInvoke, MarshalingUtils.UseUnicodeAsDefaultMarshalingForStringParameters(interopMethod))
	{
		_methodDefinition = interopMethod.Resolve();
		_pinvokeInfo = _methodDefinition.PInvokeInfo;
	}

	internal static string FORCE_PINVOKE_lib_INTERNAL(string lib)
	{
		string validLib = Path.GetFileNameWithoutExtension(lib).Replace('-', '_').Replace('.', '_');
		return "FORCE_PINVOKE_" + validLib + "_INTERNAL";
	}

	public void WriteExternMethodDeclarationForInternalPInvoke(IGeneratedMethodCodeWriter writer)
	{
		if (CanInternalizeMethod())
		{
			bool useForcedPInvoke = !ShouldInternalizeMethod();
			writer.AddInternalPInvokeMethodDeclaration(_pinvokeInfo.EntryPoint, $"IL2CPP_EXTERN_C {FormatReturnTypeForTypedef()} {GetCallingConvention()} {_pinvokeInfo.EntryPoint}({FormatParametersForTypedef()});", _pinvokeInfo.Module.Name, useForcedPInvoke, IsMethodExplicitlyMarkedInternal());
		}
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"typedef {GetPInvokeMethodVariable(writer.Context)};");
		if (ShouldInternalizeMethod())
		{
			writer.WriteLine();
			return;
		}
		if (CanInternalizeMethod())
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"#if !{"FORCE_PINVOKE_INTERNAL"} && !{FORCE_PINVOKE_lib_INTERNAL(_pinvokeInfo.Module.Name)}");
		}
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"static {writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()} {writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerVariable()};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerVariable()} == NULL)");
		writer.BeginBlock();
		string parameterSizeVariableName = "parameterSize";
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"int {parameterSizeVariableName} = {CalculateParameterSize(writer.Context)};");
		string nativeDynamicLibrary = _pinvokeInfo.Module.Name;
		string nativeMethodName = _pinvokeInfo.EntryPoint;
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerVariable()} = il2cpp_codegen_resolve_pinvoke<{writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()}>(IL2CPP_NATIVE_STRING(\"{nativeDynamicLibrary}\"), \"{nativeMethodName}\", {GetIl2CppCallConvention()}, {GetCharSet()}, {parameterSizeVariableName}, {(_pinvokeInfo.IsNoMangle ? "true" : "false")});");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"IL2CPP_ASSERT({writer.Context.Global.Services.Naming.ForPInvokeFunctionPointerVariable()} != {"NULL"});");
		writer.EndBlock();
		if (CanInternalizeMethod())
		{
			writer.WriteLine("#endif");
		}
		writer.WriteLine();
	}

	private void EmitInternalAndExternalInvocation(IGeneratedMethodCodeWriter writer, string[] localVariableNames, string returnValueAssignment = "")
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter;
		if (!CanInternalizeMethod())
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{returnValueAssignment}{GetExternalMethodCallExpression(writer.Context, localVariableNames)};");
			return;
		}
		if (!ShouldInternalizeMethod())
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"#if {"FORCE_PINVOKE_INTERNAL"} || {FORCE_PINVOKE_lib_INTERNAL(_pinvokeInfo.Module.Name)}");
		}
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{returnValueAssignment}{GetInternalizedMethodCallExpression(writer.Context, localVariableNames)};");
		if (!ShouldInternalizeMethod())
		{
			writer.WriteLine("#else");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{returnValueAssignment}{GetExternalMethodCallExpression(writer.Context, localVariableNames)};");
			writer.WriteLine("#endif");
		}
	}

	protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReturnType methodReturnType = GetMethodReturnType();
		string assignReturnValueStr = "";
		if (!PreserveSig())
		{
			assignReturnValueStr = "il2cpp_hresult_t " + writer.Context.Global.Services.Naming.ForInteropHResultVariable() + " = ";
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				MarshalInfoWriterFor(writer.Context, methodReturnType).WriteNativeVariableDeclarationOfType(writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
			}
		}
		else if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
		{
			assignReturnValueStr = MarshaledReturnType.DecoratedName + " " + writer.Context.Global.Services.Naming.ForInteropReturnValue() + " = ";
		}
		EmitInternalAndExternalInvocation(writer, localVariableNames, assignReturnValueStr);
		if (_pinvokeInfo != null && _pinvokeInfo.SupportsLastError)
		{
			writer.WriteLine("il2cpp_codegen_marshal_store_last_error();");
		}
		if (!PreserveSig())
		{
			writer.WriteLine();
			writer.WriteStatement(Emit.Call(writer.Context, "il2cpp_codegen_com_raise_exception_if_failed", writer.Context.Global.Services.Naming.ForInteropHResultVariable(), "false"));
		}
	}

	private string GetPInvokeMethodVariable(ReadOnlyContext context)
	{
		return $"{FormatReturnTypeForTypedef()} ({GetCallingConvention()} *{context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()}) ({FormatParametersForTypedef()})";
	}

	private string GetCallingConvention()
	{
		if (_pinvokeInfo.IsCallConvStdCall)
		{
			return "STDCALL";
		}
		if (_pinvokeInfo.IsCallConvCdecl)
		{
			return "CDECL";
		}
		return "DEFAULT_CALL";
	}

	private string GetIl2CppCallConvention()
	{
		if (_pinvokeInfo.IsCallConvStdCall)
		{
			return "IL2CPP_CALL_STDCALL";
		}
		if (_pinvokeInfo.IsCallConvCdecl)
		{
			return "IL2CPP_CALL_C";
		}
		return "IL2CPP_CALL_DEFAULT";
	}

	private string GetCharSet()
	{
		if (_pinvokeInfo.IsCharSetNotSpec)
		{
			return "CHARSET_NOT_SPECIFIED";
		}
		if (_pinvokeInfo.IsCharSetAnsi)
		{
			return "CHARSET_ANSI";
		}
		return "CHARSET_UNICODE";
	}

	private string CalculateParameterSize(ReadOnlyContext context)
	{
		MarshaledParameter[] parameters = Parameters;
		StringBuilder size = new StringBuilder();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (i > 0)
			{
				size.Append(" + ");
			}
			size.Append(GetParameterSize(context, parameters[i]));
		}
		if (!PreserveSig() && InteropMethod.ReturnType.MetadataType != MetadataType.Void)
		{
			if (parameters.Length != 0)
			{
				size.Append(" + ");
			}
			size.Append("sizeof(void*)");
		}
		if (size.Length <= 0)
		{
			return "0";
		}
		return size.ToString();
	}

	private string GetParameterSize(ReadOnlyContext context, MarshaledParameter parameter)
	{
		DefaultMarshalInfoWriter marshalInfo = MarshalInfoWriterFor(context, parameter);
		if (marshalInfo.GetNativeSize(context) == "-1" && parameter.ParameterType.MetadataType != MetadataType.Array)
		{
			throw new NotSupportedException($"Cannot marshal parameter {parameter.NameInGeneratedCode} of type {parameter.ParameterType.FullName} for P/Invoke.");
		}
		MetadataType metadataType = parameter.ParameterType.MetadataType;
		if (metadataType == MetadataType.Class || metadataType == MetadataType.Array)
		{
			return "sizeof(void*)";
		}
		int remainderTo4 = 4 - marshalInfo.GetNativeSizeWithoutPointers(context) % 4;
		if (remainderTo4 != 4)
		{
			return marshalInfo.GetNativeSize(context) + " + " + remainderTo4;
		}
		return marshalInfo.GetNativeSize(context);
	}

	private string GetInternalizedMethodCallExpression(ReadOnlyContext context, string[] localVariableNames)
	{
		string parameters = GetFunctionCallParametersExpression(context, localVariableNames, !PreserveSig());
		return $"reinterpret_cast<{context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()}>({_pinvokeInfo.EntryPoint})({parameters})";
	}

	private string GetExternalMethodCallExpression(ReadOnlyContext context, string[] localVariableNames)
	{
		string parameters = GetFunctionCallParametersExpression(context, localVariableNames, !PreserveSig());
		return context.Global.Services.Naming.ForPInvokeFunctionPointerVariable() + "(" + parameters + ")";
	}

	protected string FormatReturnTypeForTypedef()
	{
		if (PreserveSig())
		{
			return MarshaledReturnType.DecoratedName;
		}
		return "il2cpp_hresult_t";
	}

	protected string FormatParametersForTypedef()
	{
		StringBuilder parameterList = new StringBuilder();
		for (int i = 0; i < MarshaledParameterTypes.Length; i++)
		{
			if (parameterList.Length > 0)
			{
				parameterList.Append(", ");
			}
			parameterList.Append(MarshaledParameterTypes[i].DecoratedName);
		}
		if (!PreserveSig() && _methodDefinition.ReturnType.MetadataType != MetadataType.Void)
		{
			if (parameterList.Length > 0)
			{
				parameterList.Append(", ");
			}
			parameterList.Append(MarshaledReturnType.DecoratedName);
			parameterList.Append('*');
		}
		return parameterList.ToString();
	}

	private bool CanInternalizeMethod()
	{
		return _pinvokeInfo != null;
	}

	private bool ShouldInternalizeMethod()
	{
		if (!CanInternalizeMethod())
		{
			return false;
		}
		if (IsMethodExplicitlyMarkedInternal())
		{
			return true;
		}
		return ShouldInternalizeMethod(_methodDefinition, _pinvokeInfo);
	}

	public static bool ShouldInternalizeMethod(MethodDefinition methodDefinition, PInvokeInfo pInvokeInfo)
	{
		if (InternalizedMethods.TryGetValue(pInvokeInfo.Module.Name, out var names))
		{
			return names.Contains(methodDefinition.Name);
		}
		return false;
	}

	private bool IsMethodExplicitlyMarkedInternal()
	{
		if (_pinvokeInfo.Module.Name == "__Internal")
		{
			return true;
		}
		return false;
	}

	private bool PreserveSig()
	{
		if (_methodDefinition.HasPInvokeInfo)
		{
			return _methodDefinition.IsPreserveSig;
		}
		return true;
	}
}
