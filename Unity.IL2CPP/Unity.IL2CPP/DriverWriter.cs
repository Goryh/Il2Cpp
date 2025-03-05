using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal class DriverWriter
{
	private readonly AssemblyDefinition _executable;

	private readonly MethodDefinition _entryPoint;

	private readonly SourceWritingContext _context;

	public DriverWriter(SourceWritingContext context, AssemblyDefinition executable)
	{
		_context = context;
		_executable = executable;
		_entryPoint = _executable.EntryPoint;
	}

	public void Write(NPath assemblyDirectory)
	{
		using IGeneratedMethodCodeStream writer = _context.CreateProfiledManagedSourceWriterInOutputDirectory(FileCategory.Other, "driver.cpp");
		WriteIncludes(writer);
		WriteMemoryCallbacks(writer);
		WriteMainInvoker(writer);
		WriteEntryPoint(writer, assemblyDirectory);
		WritePlatformSpecificEntryPoints(writer);
		MethodWriter.WriteInlineMethodDefinitions(_context, "driver", writer);
	}

	private void WriteIncludes(IGeneratedCodeWriter writer)
	{
		writer.WriteLine("#include \"il2cpp-api.h\"");
		writer.WriteLine("#include \"utils/Exception.h\"");
		writer.WriteLine("#include \"utils/StringUtils.h\"");
		writer.WriteLine("#include \"utils/Memory.h\"");
		writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
		writer.WriteLine("#include \"Windows.h\"");
		writer.WriteLine("#include \"Shellapi.h\"");
		writer.WriteLine("#elif IL2CPP_TARGET_WINDOWS_GAMES");
		writer.WriteLine("#include \"Windows.h\"");
		writer.WriteLine("#endif");
		writer.WriteLine();
		writer.WriteLine("extern \"C\" const char * platform_config_path();");
		writer.WriteLine("extern \"C\" const char * platform_data_path();");
		writer.WriteLine();
		if (_context.Global.Parameters.GoogleBenchmark)
		{
			writer.AddInclude("il2cpp-benchmark-support.h");
		}
	}

	private void WriteMemoryCallbacks(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine("static void* EmptyMalloc(size_t size){IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); return nullptr; }");
		writer.WriteLine("static void* EmptyAlignedMalloc(size_t size, size_t alignment) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); return nullptr; }");
		writer.WriteLine("static void EmptyFree(void* memory) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); }");
		writer.WriteLine("static void EmptyAlignedFree(void* memory) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); }");
		writer.WriteLine("static void* EmptyCalloc(size_t count, size_t size) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); return nullptr; }");
		writer.WriteLine("static void* EmptyRealloc(void* memory, size_t newSize) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); return nullptr; }");
		writer.WriteLine("static void* EmptyAlignedRealloc(void* memory, size_t newSize, size_t alignment) { IL2CPP_ASSERT(false && \"malloc/free called outside the runtime, object globally constructed?\"); return nullptr; }");
		writer.WriteLine();
	}

	private void WriteMainInvoker(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteMethodWithMetadataInitialization("int MainInvoker(int argc, const Il2CppNativeChar* const* argv)", WriteMainInvokerBody, "MainInvoker", null);
		writer.WriteLine();
	}

	private void WriteMainInvokerBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteLine("try");
		using (new BlockWriter(writer))
		{
			WriteMainInvocation(writer, metadataAccess);
		}
		writer.WriteLine("catch (const Il2CppExceptionWrapper& e)");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("il2cpp_codegen_write_to_stderr(\"Unhandled Exception: \");");
			writer.WriteLine("auto method = il2cpp_class_get_method_from_name(il2cpp_object_get_class(e.ex), \"ToString\", 0);");
			writer.WriteLine("auto exceptionString = (Il2CppString*)il2cpp_runtime_invoke(method, e.ex, NULL, NULL);");
			writer.WriteLine("if (exceptionString != NULL)");
			writer.Indent();
			writer.WriteLine("il2cpp_codegen_write_to_stderr(il2cpp::utils::StringUtils::Utf16ToUtf8(exceptionString->chars).c_str());");
			writer.Dedent();
			writer.WriteLine("else");
			writer.Indent();
			writer.WriteLine("il2cpp_codegen_write_to_stderr(\"The exception had no message\");");
			writer.Dedent();
			writer.WriteLine("#if IL2CPP_TARGET_IOS");
			writer.WriteLine("return 0;");
			writer.WriteLine("#else");
			writer.WriteLine("il2cpp_codegen_abort();");
			writer.WriteLine("il2cpp_codegen_no_return();");
			writer.WriteLine("#endif");
		}
	}

	private void WriteMainInvocation(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (!ValidateMainMethod(writer))
		{
			return;
		}
		List<string> args = new List<string>();
		if (_entryPoint.Parameters.Count > 0 && !_context.Global.Parameters.GoogleBenchmark)
		{
			ArrayType arrayType = (ArrayType)_entryPoint.Parameters[0].ParameterType;
			writer.AddIncludeForTypeDefinition(writer.Context, arrayType);
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{arrayType.CppNameForVariable} args = {Emit.NewSZArray(writer.Context, arrayType, "argc - 1", metadataAccess)};");
			writer.WriteLine();
			writer.WriteLine("for (int i = 1; i < argc; i++)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("DECLARE_NATIVE_C_STRING_AS_STRING_VIEW_OF_IL2CPP_CHARS(argumentUtf16, argv[i]);");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{_context.Global.Services.TypeProvider.SystemString.CppNameForVariable} argument = il2cpp_codegen_string_new_utf16(argumentUtf16);");
				writer.WriteStatement(Emit.StoreArrayElement("args", "i - 1", "argument", useArrayBoundsCheck: false));
			}
			writer.WriteLine();
			args.Add("args");
		}
		string returnVariable = "";
		if (_entryPoint.ReturnType.MetadataType == MetadataType.Int32)
		{
			returnVariable = "il2cppRetVal";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"int32_t {returnVariable} = 0;");
		}
		else if (_entryPoint.ReturnType.MetadataType == MetadataType.UInt32)
		{
			returnVariable = "il2cppRetVal";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"uint32_t {returnVariable} = 0;");
		}
		if (!_context.Global.Parameters.NoLazyStaticConstructors && (_entryPoint.DeclaringType.Attributes & TypeAttributes.BeforeFieldInit) == 0)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_runtime_class_init_inline({metadataAccess.TypeInfoFor(_entryPoint.DeclaringType)});");
		}
		TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(_entryPoint.DeclaringType, _entryPoint);
		MethodBodyWriter.WriteMethodCallExpression(returnVariable, writer, null, _entryPoint, typeResolver, MethodCallType.Normal, metadataAccess.MethodMetadataFor(_entryPoint), writer.Context.Global.Services.VTable, args, useArrayBoundsCheck: false);
		switch (_entryPoint.ReturnType.MetadataType)
		{
		case MetadataType.Void:
			writer.WriteLine("return 0;");
			break;
		case MetadataType.Int32:
			writer.WriteStatement("return il2cppRetVal");
			break;
		case MetadataType.UInt32:
			writer.WriteLine("return static_cast<int>(il2cppRetVal);");
			break;
		}
	}

	private bool ValidateMainMethod(IGeneratedMethodCodeWriter writer)
	{
		if (_entryPoint == null)
		{
			string exceptionText = "Entry point not found in assembly '" + _executable.FullName + "'.";
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"" + exceptionText + "\")"));
			return false;
		}
		if (_entryPoint.HasThis)
		{
			string exceptionText2 = "Entry point must be static.";
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"" + exceptionText2 + "\")"));
			return false;
		}
		TypeReference returnType = _entryPoint.ReturnType;
		if (returnType.MetadataType != MetadataType.Void && returnType.MetadataType != MetadataType.Int32 && returnType.MetadataType != MetadataType.UInt32)
		{
			string exceptionText3 = "Entry point must have a return type of void, integer, or unsigned integer.";
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"" + exceptionText3 + "\")"));
			return false;
		}
		ReadOnlyCollection<ParameterDefinition> parameters = _entryPoint.Parameters;
		bool isSignatureValid = parameters.Count < 2 && !_entryPoint.HasGenericParameters;
		if (isSignatureValid && parameters.Count == 1)
		{
			if (!(parameters[0].ParameterType is ArrayType { IsVector: not false } arrayType))
			{
				isSignatureValid = false;
			}
			else if (arrayType.ElementType.MetadataType != MetadataType.String)
			{
				isSignatureValid = false;
			}
		}
		if (!isSignatureValid)
		{
			string exceptionText4 = "Entry point method for type '" + _entryPoint.DeclaringType.FullName + "' has invalid signature.";
			writer.WriteStatement(string.Format(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"" + exceptionText4 + "\")")));
			return false;
		}
		if (_entryPoint.DeclaringType.HasGenericParameters)
		{
			string exceptionText5 = "Entry point method is defined on a generic type '" + _entryPoint.DeclaringType.FullName + "'.";
			writer.WriteStatement(string.Format(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"" + exceptionText5 + "\")")));
			return false;
		}
		return true;
	}

	private void WriteEntryPoint(IGeneratedMethodCodeWriter writer, NPath assemblyDirectory)
	{
		writer.WriteLine("int EntryPoint(int argc, const Il2CppNativeChar* const* argv)");
		using (new BlockWriter(writer))
		{
			WriteWaitForDebuggerHook(writer, _executable.Name.Name);
			WriteSetDebuggerOptions(writer);
			WriteSetCommandLineArgumentsAndInitIl2Cpp(writer);
			WriteSetConfiguration(writer);
			writer.WriteLine();
			writer.Dedent();
			writer.WriteLine();
			writer.Indent();
			if (_context.Global.Parameters.GoogleBenchmark)
			{
				writer.WriteLine("il2cpp_benchmark_initialize(argc, argv);");
			}
			writer.WriteLine("int exitCode = MainInvoker(argc, argv);");
			writer.WriteLine();
			writer.WriteLine("il2cpp_shutdown();");
			writer.WriteLine("Il2CppMemoryCallbacks emptyCallbacks = {EmptyMalloc, EmptyAlignedMalloc, EmptyFree, EmptyAlignedFree, EmptyCalloc, EmptyRealloc, EmptyAlignedRealloc};");
			writer.WriteLine("il2cpp_set_memory_callbacks(&emptyCallbacks);");
			writer.WriteLine("return exitCode;");
		}
		writer.WriteLine();
	}

	private void WriteSetDebuggerOptions(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		writer.WriteLine("#define DEBUGGER_STRINGIFY(x) #x");
		writer.WriteLine("#define DEBUGGER_STRINGIFY2(x) DEBUGGER_STRINGIFY(x)");
		writer.WriteLine("#ifdef IL2CPP_MONO_DEBUGGER_LOGFILE");
		writer.WriteLine("#if IL2CPP_TARGET_JAVASCRIPT || IL2CPP_TARGET_IOS");
		writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n,loglevel=9\");");
		writer.WriteLine("#else");
		writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n,loglevel=9,logfile=\" DEBUGGER_STRINGIFY2(IL2CPP_MONO_DEBUGGER_LOGFILE) \"\");");
		writer.WriteLine("#endif");
		writer.WriteLine("#else");
		writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n\");");
		writer.WriteLine("#endif");
		writer.WriteLine("#undef DEBUGGER_STRINGIFY");
		writer.WriteLine("#undef DEBUGGER_STRINGIFY2");
		writer.WriteLine("#endif");
		writer.WriteLine();
	}

	private void WriteSetConfiguration(ICodeWriter writer)
	{
		writer.WriteLine();
		writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
		writer.WriteLine("il2cpp_set_config_utf16(argv[0]);");
		writer.WriteLine("#elif IL2CPP_TARGET_JAVASCRIPT");
		writer.WriteLine("il2cpp_set_config(\"/\");");
		writer.WriteLine("#else");
		writer.WriteLine("il2cpp_set_config(argv[0]);");
		writer.WriteLine("#endif");
	}

	private static string EscapePath(string path)
	{
		return path.Replace("\\", "\\\\");
	}

	private void WriteSetCommandLineArgumentsAndInitIl2Cpp(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine("#if IL2CPP_DISABLE_GC");
		writer.WriteLine("il2cpp_gc_disable();");
		writer.WriteLine("#endif");
		writer.WriteLine();
		writer.Dedent();
		writer.WriteLine("#if IL2CPP_DRIVER_PLATFORM_CONFIG");
		writer.Indent();
		writer.WriteLine("il2cpp_set_data_dir(platform_data_path());");
		writer.Dedent();
		writer.WriteLine("#endif");
		writer.WriteLine();
		writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
		writer.Indent();
		writer.WriteLine("il2cpp_set_commandline_arguments_utf16(argc, argv, NULL);");
		writer.WriteLine("il2cpp_init_utf16(argv[0]);");
		writer.Dedent();
		writer.WriteLine("#else");
		writer.Indent();
		writer.WriteLine("il2cpp_set_commandline_arguments(argc, argv, NULL);");
		writer.WriteLine("il2cpp_init(argv[0]);");
		writer.Dedent();
		writer.WriteLine("#endif");
		writer.Indent();
	}

	private void WriteWaitForDebuggerHook(ICodeWriter writer, string executableName)
	{
		if (_context.Global.Parameters.NeverAttachDialog || (!System.Diagnostics.Debugger.IsAttached && !_context.Global.Parameters.EmitAttachDialog))
		{
			return;
		}
		writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
		using (new BlockWriter("if (!IsDebuggerPresent())", writer))
		{
			writer.WriteLine("HANDLE hDialogAttachMutex = CreateMutex(NULL, FALSE, L\"IL2CPP-DriverWriter-ShowAttachDialog\");");
			using (new BlockWriter("if (MessageBoxW(NULL, L\"Attach Debugger Now or [OK] to launch VS Jit Debugger Dialog, [Cancel] to continue\", L\"" + executableName + "\", MB_OKCANCEL) == IDOK)", writer))
			{
				using (new BlockWriter("if (!IsDebuggerPresent())", writer))
				{
					writer.WriteLine("DWORD pid = GetCurrentProcessId();");
					writer.WriteLine("WCHAR commandLine[100];");
					writer.WriteLine("wsprintf(commandLine, L\"vsjitdebugger.exe -p %d\", pid);");
					writer.WriteLine("STARTUPINFOW startInfo = {0};");
					writer.WriteLine("PROCESS_INFORMATION pi = {0};");
					using (new BlockWriter("if (CreateProcessW(NULL, commandLine, NULL, NULL, FALSE, 0, NULL, NULL, &startInfo, &pi))", writer))
					{
						writer.WriteLine("WaitForSingleObject(pi.hProcess, INFINITE);");
						writer.WriteLine("CloseHandle(pi.hThread);");
						writer.WriteLine("CloseHandle(pi.hProcess);");
					}
				}
			}
			writer.WriteLine("if (!IsDebuggerPresent() && hDialogAttachMutex != NULL) CloseHandle(hDialogAttachMutex);");
		}
		writer.WriteLine("#endif");
		writer.WriteLine();
	}

	private void WritePlatformSpecificEntryPoints(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine("#if IL2CPP_CUSTOM_NATIVE_ENTRYPOINT");
		writer.WriteLine();
		writer.WriteLine("#elif IL2CPP_TARGET_WINDOWS");
		writer.WriteLine();
		writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_GAMES");
		writer.WriteLine("#include <windef.h>");
		writer.WriteLine("#include <string>");
		writer.WriteLine("#include <locale>");
		writer.WriteLine("#include <codecvt>");
		writer.WriteLine("#elif !IL2CPP_TARGET_WINDOWS_DESKTOP");
		writer.WriteLine("#include \"ActivateApp.h\"");
		writer.WriteLine("#endif");
		writer.WriteLine();
		writer.WriteLine("int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nShowCmd)");
		using (new BlockWriter(writer))
		{
			writer.Dedent();
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP || IL2CPP_TARGET_WINDOWS_GAMES");
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
			writer.Indent();
			writer.WriteLine("int argc;");
			writer.WriteLine("wchar_t** argv = CommandLineToArgvW(GetCommandLineW(), &argc);");
			writer.WriteLine("int returnValue = EntryPoint(argc, argv);");
			writer.WriteLine("LocalFree(argv);");
			writer.WriteLine("return returnValue;");
			writer.Dedent();
			writer.WriteLine("#elif IL2CPP_TARGET_WINDOWS_GAMES");
			writer.Indent();
			writer.WriteLine("int result = EntryPoint(__argc, __wargv);");
			writer.WriteLine("return result;");
			writer.Dedent();
			writer.WriteLine("#endif");
			writer.WriteLine("#elif IL2CPP_WINRT_NO_ACTIVATE");
			writer.Indent();
			writer.WriteLine("wchar_t executableName[MAX_PATH + 2];");
			writer.WriteLine("GetModuleFileNameW(nullptr, executableName, MAX_PATH + 2);");
			writer.WriteLine();
			writer.WriteLine("int argc = 1;");
			writer.WriteLine("const wchar_t* argv[] = { executableName };");
			writer.WriteLine("return EntryPoint(argc, argv);");
			writer.Dedent();
			writer.WriteLine("#else");
			writer.Indent();
			writer.WriteLine("return WinRT::Activate(EntryPoint);");
			writer.Dedent();
			writer.WriteLine("#endif");
			writer.Indent();
		}
		writer.WriteLine();
		writer.WriteLine("#elif IL2CPP_TARGET_JAVASCRIPT && IL2CPP_MONO_DEBUGGER");
		writer.WriteLine("#include <emscripten.h>");
		writer.WriteLine("#include <emscripten/fetch.h>");
		writer.WriteLine("#include <emscripten/html5.h>");
		writer.WriteLine();
		writer.WriteLine();
		writer.WriteLine("int main(int argc, const char* argv[])");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("emscripten_fetch_attr_t attr;");
			writer.WriteLine("emscripten_fetch_attr_init(&attr);");
			writer.WriteLine("strcpy(attr.requestMethod, \"GET\");");
			writer.WriteLine("attr.attributes = EMSCRIPTEN_FETCH_LOAD_TO_MEMORY;");
			writer.WriteLine("attr.onsuccess = OnSuccess;");
			writer.WriteLine("attr.onerror = OnError;");
			writer.WriteLine("emscripten_fetch(&attr, \"Data/Metadata/global-metadata.dat\");");
			writer.WriteLine("#if (__EMSCRIPTEN_major__ >= 1) && (__EMSCRIPTEN_minor__ >= 39) && (__EMSCRIPTEN_tiny__ >= 5)");
			writer.WriteLine("emscripten_unwind_to_js_event_loop();");
			writer.WriteLine("#endif");
		}
		writer.WriteLine();
		writer.WriteLine("#else");
		writer.WriteLine();
		writer.WriteLine("int main(int argc, const char* argv[])");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("return EntryPoint(argc, argv);");
		}
		writer.WriteLine();
		writer.WriteLine("#endif");
	}
}
