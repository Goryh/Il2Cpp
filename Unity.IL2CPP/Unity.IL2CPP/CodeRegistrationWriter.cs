using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.TableWriters;

namespace Unity.IL2CPP;

public static class CodeRegistrationWriter
{
	private enum CodeRegistrationWriterMode
	{
		AllAssemblies,
		PerAssembly,
		PerAssemblyGlobal
	}

	public static void WriteCodeRegistration(SourceWritingContext context, UnresolvedIndirectCallsTableInfo virtualCallTables, ReadOnlyCollection<AssemblyCodeMetadata> unorderedCodeMetadata, WritePerAssemblyCodeMetadata.Tables tables)
	{
		new WriteRgctxTable(unorderedCodeMetadata).Schedule(context);
		WriteCodeRegistration(context, tables.InvokerTable, tables.ReversePInvokeWrappersTable, tables.GenericMethodPointerTable, tables.GenericAdjustorThunkTable, virtualCallTables, tables.InteropDataTable, tables.WindowsRuntimeFactoryTable, unorderedCodeMetadata.Select((AssemblyCodeMetadata d) => d.CodegenModule).ToSortedCollection(), (context.Global.AsReadOnly().Services.ContextScope.UniqueIdentifier != null) ? CodeRegistrationWriterMode.PerAssembly : CodeRegistrationWriterMode.AllAssemblies);
	}

	public static void WritePerAssemblyGlobalCodeRegistration(SourceWritingContext context, ReadOnlyCollection<string> codeGenModules)
	{
		TableInfo reversePInvokeWrappersTable = TableInfo.Empty;
		TableInfo genericMethodPointerTable = TableInfo.Empty;
		TableInfo genericAdjustorThunkTable = TableInfo.Empty;
		TableInfo invokerTable = TableInfo.Empty;
		TableInfo interopDataTable = TableInfo.Empty;
		TableInfo windowsRuntimeFactoryTable = TableInfo.Empty;
		UnresolvedIndirectCallsTableInfo virtualCallTables = new UnresolvedIndirectCallsTableInfo
		{
			VirtualMethodPointersInfo = TableInfo.Empty,
			InstanceMethodPointersInfo = TableInfo.Empty,
			StaticMethodPointersInfo = TableInfo.Empty,
			SignatureTypes = Array.Empty<IndirectCallSignature>().AsReadOnly()
		};
		WriteCodeRegistration(context, invokerTable, reversePInvokeWrappersTable, genericMethodPointerTable, genericAdjustorThunkTable, virtualCallTables, interopDataTable, windowsRuntimeFactoryTable, codeGenModules, CodeRegistrationWriterMode.PerAssemblyGlobal);
	}

	public static string CodeRegistrationTableName(ReadOnlyContext context)
	{
		return context.Global.Services.ContextScope.ForMetadataGlobalVar("g_CodeRegistration");
	}

	private static void WriteCodeRegistration(SourceWritingContext context, TableInfo invokerTable, TableInfo reversePInvokeWrappersTable, TableInfo genericMethodPointerTable, TableInfo genericAdjustorThunkTable, UnresolvedIndirectCallsTableInfo virtualCallTables, TableInfo interopDataTable, TableInfo windowsRuntimeFactoryTable, ReadOnlyCollection<string> codeGenModules, CodeRegistrationWriterMode mode)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "Il2CppCodeRegistration.cpp");
		if (reversePInvokeWrappersTable.Count > 0)
		{
			writer.WriteLine(reversePInvokeWrappersTable.GetDeclaration());
		}
		if (genericMethodPointerTable.Count > 0)
		{
			writer.WriteLine(genericMethodPointerTable.GetDeclaration());
		}
		if (genericAdjustorThunkTable.Count > 0)
		{
			writer.WriteLine(genericAdjustorThunkTable.GetDeclaration());
		}
		if (invokerTable.Count > 0)
		{
			writer.WriteLine(invokerTable.GetDeclaration());
		}
		if (virtualCallTables.VirtualMethodPointersInfo.Count > 0)
		{
			writer.WriteLine(virtualCallTables.VirtualMethodPointersInfo.GetDeclaration());
			writer.WriteLine(virtualCallTables.InstanceMethodPointersInfo.GetDeclaration());
			writer.WriteLine(virtualCallTables.StaticMethodPointersInfo.GetDeclaration());
		}
		if (virtualCallTables.VirtualMethodPointersInfo.Count != virtualCallTables.InstanceMethodPointersInfo.Count)
		{
			throw new InvalidOperationException("The unresolved-virtual and unresolved-direct-tables must be the same size");
		}
		if (virtualCallTables.VirtualMethodPointersInfo.Count != virtualCallTables.StaticMethodPointersInfo.Count)
		{
			throw new InvalidOperationException("The unresolved-virtual and unresolved-direct-tables must be the same size");
		}
		if (interopDataTable.Count > 0)
		{
			writer.WriteLine(interopDataTable.GetDeclaration());
		}
		if (windowsRuntimeFactoryTable.Count > 0)
		{
			writer.WriteLine(windowsRuntimeFactoryTable.GetDeclaration());
		}
		if (mode == CodeRegistrationWriterMode.AllAssemblies || mode == CodeRegistrationWriterMode.PerAssemblyGlobal)
		{
			foreach (string codeGenModule in codeGenModules)
			{
				ICppCodeStream cppCodeStream = writer;
				cppCodeStream.WriteLine($"IL2CPP_EXTERN_C_CONST Il2CppCodeGenModule {codeGenModule};");
			}
			writer.WriteArrayInitializer("const Il2CppCodeGenModule*", "g_CodeGenModules", codeGenModules.Select(Emit.AddressOf), externArray: true, nullTerminate: false);
		}
		writer.WriteStructInitializer("const Il2CppCodeRegistration", CodeRegistrationTableName(context), new string[17]
		{
			reversePInvokeWrappersTable.Count.ToString(),
			(reversePInvokeWrappersTable.Count > 0) ? reversePInvokeWrappersTable.Name : "NULL",
			genericMethodPointerTable.Count.ToString(),
			(genericMethodPointerTable.Count > 0) ? genericMethodPointerTable.Name : "NULL",
			(genericAdjustorThunkTable.Count > 0) ? genericAdjustorThunkTable.Name : "NULL",
			invokerTable.Count.ToString(),
			(invokerTable.Count > 0) ? invokerTable.Name : "NULL",
			virtualCallTables.VirtualMethodPointersInfo.Count.ToString(),
			(virtualCallTables.VirtualMethodPointersInfo.Count > 0) ? virtualCallTables.VirtualMethodPointersInfo.Name : "NULL",
			(virtualCallTables.InstanceMethodPointersInfo.Count > 0) ? virtualCallTables.InstanceMethodPointersInfo.Name : "NULL",
			(virtualCallTables.StaticMethodPointersInfo.Count > 0) ? virtualCallTables.StaticMethodPointersInfo.Name : "NULL",
			interopDataTable.Count.ToString(),
			(interopDataTable.Count > 0) ? interopDataTable.Name : "NULL",
			windowsRuntimeFactoryTable.Count.ToString(),
			(windowsRuntimeFactoryTable.Count > 0) ? windowsRuntimeFactoryTable.Name : "NULL",
			codeGenModules.Count.ToString(),
			(codeGenModules.Count > 0) ? "g_CodeGenModules" : "NULL"
		}, externStruct: true);
		if (mode == CodeRegistrationWriterMode.AllAssemblies || mode == CodeRegistrationWriterMode.PerAssemblyGlobal)
		{
			WriteGlobalCodeRegistrationCalls(context, mode, writer);
		}
	}

	private static void WriteGlobalCodeRegistrationCalls(SourceWritingContext context, CodeRegistrationWriterMode mode, ICppCodeWriter writer)
	{
		string metadataRegistrationVarPtr = "NULL";
		if (mode == CodeRegistrationWriterMode.AllAssemblies)
		{
			writer.WriteLine("IL2CPP_EXTERN_C_CONST Il2CppMetadataRegistration g_MetadataRegistration;");
			metadataRegistrationVarPtr = "&g_MetadataRegistration";
		}
		if (context.Global.Parameters.EnableReload)
		{
			writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
			writer.WriteLine("extern \"C\" void ClearMethodMetadataInitializedFlags();");
			writer.WriteLine("#endif");
		}
		string codeGenOptionsVariableStorageClass = "static";
		writer.WriteStructInitializer(codeGenOptionsVariableStorageClass + " const Il2CppCodeGenOptions", "s_Il2CppCodeGenOptions", new string[3]
		{
			context.Global.Parameters.CanShareEnumTypes ? "true" : "false",
			context.Global.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth.ToString(),
			context.Global.Results.Initialize.GenericLimits.VirtualMethodIterations.ToString()
		}, externStruct: false);
		writer.WriteLine("void s_Il2CppCodegenRegistration()");
		writer.BeginBlock();
		writer.WriteLine($"il2cpp_codegen_register (&g_CodeRegistration, {metadataRegistrationVarPtr}, &s_Il2CppCodeGenOptions);");
		if (context.Global.Parameters.EnableDebugger)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			writer.WriteLine("il2cpp_codegen_register_debugger_data(NULL);");
			writer.WriteLine("#endif");
		}
		if (context.Global.Parameters.EnableReload)
		{
			writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
			writer.WriteLine("il2cpp_codegen_register_metadata_initialized_cleanup(ClearMethodMetadataInitializedFlags);");
			writer.WriteLine("#endif");
		}
		writer.EndBlock();
		writer.WriteLine("#if RUNTIME_IL2CPP");
		writer.WriteLine("typedef void (*CodegenRegistrationFunction)();");
		writer.WriteLine("CodegenRegistrationFunction g_CodegenRegistration = s_Il2CppCodegenRegistration;");
		writer.WriteLine("#endif");
	}
}
