using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata;

internal static class PerAssemblyCodeMetadataWriter
{
	public static AssemblyCodeMetadata Write(SourceWritingContext context, AssemblyDefinition assembly, GenericContextCollection genericContextCollection, string assemblyMetadataRegistrationVarName, string codeRegistrationVarName)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "CodeGen.c"));
		MethodDefinition[] sortedMethods = (from m in assembly.AllMethods()
			orderby m.MetadataToken.RID
			select m).ToArray();
		TypeDefinition[] collection = (from m in assembly.GetAllTypes()
			orderby m.MetadataToken.RID
			select m).ToArray();
		string methodPointers = WriteMethodPointers(context, writer, context.Global.Results.SecondaryCollection.MethodPointerNameTable[assembly]);
		WriteAdjustorThunks(context, writer, sortedMethods).Deconstruct(out var item2, out var item3);
		uint adjustorThunksCount = item2;
		string adjustorThunks = item3;
		string invokerIndices = WriteInvokerIndices(context, writer, sortedMethods);
		int usedMethodsWithReversePInvokeWrappersCount;
		string reversePinvokeIndices = WriteReversePInvokeIndices(context, writer, assembly, out usedMethodsWithReversePInvokeWrappersCount);
		List<IGenericParameterProvider> list = new List<IGenericParameterProvider>(collection);
		list.AddRange(sortedMethods);
		List<IGenericParameterProvider> res = list.Where((IGenericParameterProvider item) => genericContextCollection.GetRGCTXEntriesCount(item) > 0).ToList();
		string rgctxIndices = WriteRGCTXIndices(context, writer, genericContextCollection, res);
		List<RgctxEntryName> rgctxIndexNames = new List<RgctxEntryName>();
		string rgctxValues = WriteRGCTXValues(context, writer, genericContextCollection, res, rgctxIndexNames);
		string cleanAssemblyName = assembly.CleanFileName;
		string debugMetadata = WriteDebugger(context, writer, assembly, cleanAssemblyName);
		string moduleInitializer = "NULL";
		MethodReference moduleInitializerMethod = assembly.ModuleInitializerMethod();
		if (moduleInitializerMethod != null)
		{
			moduleInitializer = moduleInitializerMethod.CppName ?? "";
		}
		if (context.Global.Parameters.EmitComments)
		{
			moduleInitializer += ", // module initializer";
		}
		string staticConstructorsToRunAtStartupArray = "NULL";
		TypeDefinition[] staticConstructorsToRunAtStartup = assembly.GetAllTypes().Where(CompilerServicesSupport.HasEagerStaticClassConstructionEnabled).ToArray();
		if (staticConstructorsToRunAtStartup.Length != 0)
		{
			staticConstructorsToRunAtStartupArray = "s_staticConstructorsToRunAtStartup";
			writer.WriteArrayInitializer("static TypeDefinitionIndex", staticConstructorsToRunAtStartupArray, staticConstructorsToRunAtStartup.Select((TypeDefinition t) => context.Global.PrimaryCollectionResults.Metadata.GetTypeInfoIndex(t).ToString()).Append("0"), externArray: false, nullTerminate: false);
		}
		string codeGenModule = context.Global.Services.Naming.ForCodeGenModule(assembly);
		WriteCodeGenModuleInitializer(writer, codeGenModule, assembly.MainModule.GetModuleFileName(), sortedMethods, methodPointers, adjustorThunksCount, adjustorThunks, invokerIndices, usedMethodsWithReversePInvokeWrappersCount, reversePinvokeIndices, res, rgctxIndices, rgctxValues, debugMetadata, genericContextCollection, moduleInitializer, staticConstructorsToRunAtStartupArray, assemblyMetadataRegistrationVarName, codeRegistrationVarName);
		return new AssemblyCodeMetadata(codeGenModule, new ReadOnlyCollection<RgctxEntryName>(rgctxIndexNames));
	}

	public static void WriteGenericsPseudoCodeGenModule(SourceWritingContext context, string pseudoAssemblyName, string assemblyMetadataRegistrationVarName, string codeRegistrationVarName)
	{
		using ICppCodeStream writer = context.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Metadata, "CodeGen.c");
		string codeGenModuleName = writer.Context.Global.Services.ContextScope.ForCurrentCodeGenModuleVar();
		WriteCodeGenModuleInitializer(writer, codeGenModuleName, pseudoAssemblyName, null, null, null, null, null, 0, null, null, null, null, null, null, null, null, assemblyMetadataRegistrationVarName, codeRegistrationVarName);
	}

	private static void WriteCodeGenModuleInitializer(ICppCodeWriter writer, string codeGenModule, string assemblyName, MethodDefinition[] sortedMethods = null, string methodPointers = null, uint? adjustorThunksCount = null, string adjustorThunks = null, string invokerIndices = null, int usedMethodsWithReversePInvokeWrappersCount = 0, string reversePinvokeIndices = null, List<IGenericParameterProvider> res = null, string rgctxIndices = null, string rgctxValues = null, string debugMetadata = null, GenericContextCollection genericContextCollection = null, string moduleInitializer = null, string staticConstructorsToRunAtStartupArray = null, string assemblyMetadataRegistrationVarName = null, string codeRegistrationVarName = null)
	{
		writer.AddCodeGenMetadataIncludes();
		if (assemblyMetadataRegistrationVarName != null)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppMetadataRegistration " + assemblyMetadataRegistrationVarName);
		}
		if (codeRegistrationVarName != null)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppCodeRegistration " + codeRegistrationVarName);
		}
		writer.WriteStructInitializer("const Il2CppCodeGenModule", codeGenModule, new string[17]
		{
			"\"" + assemblyName + "\"",
			sortedMethods?.Length.ToString() ?? "0",
			methodPointers ?? "NULL",
			adjustorThunksCount.HasValue ? adjustorThunksCount.Value.ToString() : "0",
			adjustorThunks ?? "NULL",
			invokerIndices ?? "NULL",
			usedMethodsWithReversePInvokeWrappersCount.ToString(),
			reversePinvokeIndices ?? "NULL",
			res?.Count.ToString() ?? "0",
			rgctxIndices ?? "NULL",
			genericContextCollection?.GetRGCTXEntries().Count.ToString() ?? "0",
			rgctxValues ?? "NULL",
			debugMetadata ?? "NULL",
			moduleInitializer ?? "NULL",
			staticConstructorsToRunAtStartupArray ?? "NULL",
			(assemblyMetadataRegistrationVarName != null) ? ("&" + assemblyMetadataRegistrationVarName) : "NULL",
			(codeRegistrationVarName != null) ? ("&" + codeRegistrationVarName) : "NULL"
		}, externStruct: true);
	}

	private static string WriteMethodPointers(SourceWritingContext context, ICppCodeWriter writer, ReadOnlyMethodPointerNameTable nameTable)
	{
		ReadOnlyCollection<ReadOnlyMethodPointerNameEntryWithIndex> methodPointerNameEntries = nameTable.Items;
		foreach (ReadOnlyMethodPointerNameEntryWithIndex methodEntry in methodPointerNameEntries)
		{
			if (!methodEntry.HasIndex)
			{
				continue;
			}
			string methodName = methodEntry.Name;
			if (!(methodName == "NULL"))
			{
				MethodReference method = methodEntry.Method;
				if (writer.Context.Global.Parameters.EmitComments)
				{
					writer.WriteCommentedLine($"0x{method.MetadataToken.RID:X8} {method.FullName}");
				}
				writer.WriteLine($"extern void {methodName} (void);");
			}
		}
		string methodPointers = "NULL";
		if (methodPointerNameEntries.Count > 0)
		{
			methodPointers = "s_methodPointers";
			writer.WriteArrayInitializer("static Il2CppMethodPointer", methodPointers, methodPointerNameEntries.Select((ReadOnlyMethodPointerNameEntryWithIndex m) => m.Name), externArray: false, nullTerminate: false);
		}
		return methodPointers;
	}

	private static Tuple<uint, string> WriteAdjustorThunks(SourceWritingContext context, ICppCodeWriter writer, MethodDefinition[] sortedMethods)
	{
		List<MethodDefinition> methodsWithAdjustorThunk = new List<MethodDefinition>();
		foreach (MethodDefinition method in sortedMethods)
		{
			if (MethodWriter.HasAdjustorThunk(method) && context.Global.Results.PrimaryWrite.Methods.HasIndex(method))
			{
				string methodName = MethodTables.AdjustorThunkNameFor(context, method);
				if (methodName != "NULL")
				{
					writer.WriteLine($"extern void {methodName} (void);");
					methodsWithAdjustorThunk.Add(method);
				}
			}
		}
		string adjustorThunks = "NULL";
		if (methodsWithAdjustorThunk.Count > 0)
		{
			adjustorThunks = "s_adjustorThunks";
			writer.WriteArrayInitializer("static Il2CppTokenAdjustorThunkPair", adjustorThunks, methodsWithAdjustorThunk.Select((MethodDefinition m) => $"{{ 0x{m.MetadataToken.ToUInt32():X8}, {MethodTables.AdjustorThunkNameFor(context, m)} }}"), externArray: false, nullTerminate: false);
		}
		return new Tuple<uint, string>((uint)methodsWithAdjustorThunk.Count, adjustorThunks);
	}

	private static string WriteInvokerIndices(SourceWritingContext context, ICppCodeWriter writer, MethodDefinition[] sortedMethods)
	{
		string invokerIndices = "NULL";
		if (sortedMethods.Length != 0)
		{
			invokerIndices = "s_InvokerIndices";
			writer.WriteArrayInitializer("static const int32_t", invokerIndices, sortedMethods.Select((MethodDefinition m) => context.Global.Results.SecondaryCollection.Invokers.GetIndex(context, m).ToString()), externArray: false, nullTerminate: false);
		}
		return invokerIndices;
	}

	private static string WriteReversePInvokeIndices(SourceWritingContext context, ICppCodeWriter writer, AssemblyDefinition assembly, out int usedMethodsWithReversePInvokeWrappersCount)
	{
		string reversePinvokeIndices = "NULL";
		List<KeyValuePair<MethodReference, uint>> methodsWithReversePInvokeWrappers = (from m in context.Global.Results.PrimaryWrite.ReversePInvokeWrappers.SortedItems
			where m.Key.Module == assembly.MainModule
			orderby m.Key.Resolve().MetadataToken.RID
			select m).ToList();
		if (methodsWithReversePInvokeWrappers.Count == 0)
		{
			usedMethodsWithReversePInvokeWrappersCount = 0;
			return reversePinvokeIndices;
		}
		ReadOnlyHashSet<MethodReference> allUsedInflatedMethods = context.Global.Results.PrimaryWrite.MetadataUsage.GetInflatedMethods();
		KeyValuePair<MethodReference, uint>[] usedMethodsWithReversePInvokeWrappers = methodsWithReversePInvokeWrappers.Where((KeyValuePair<MethodReference, uint> m) => ReversePInvokeMethodBodyWriter.IsReversePInvokeMethodThatMustBeGenerated(m.Key) || allUsedInflatedMethods.Contains(m.Key)).ToArray();
		if (usedMethodsWithReversePInvokeWrappers.Length != 0)
		{
			reversePinvokeIndices = "s_reversePInvokeIndices";
			KeyValuePair<MethodReference, uint>[] array = usedMethodsWithReversePInvokeWrappers;
			foreach (KeyValuePair<MethodReference, uint> method in array)
			{
				writer.AddForwardDeclaration("extern const RuntimeMethod* " + context.Global.Services.Naming.ForRuntimeMethodInfo(context, method.Key));
			}
			writer.WriteArrayInitializer("static const Il2CppTokenIndexMethodTuple", reversePinvokeIndices, usedMethodsWithReversePInvokeWrappers.Select(delegate(KeyValuePair<MethodReference, uint> m)
			{
				context.Global.Results.PrimaryWrite.GenericMethods.TryGetValue(m.Key, out var genericMethodIndex);
				return $"{{ 0x{m.Key.Resolve().MetadataToken.ToUInt32():X8}, {m.Value.ToString()},  (void**)&{context.Global.Services.Naming.ForRuntimeMethodInfo(context, m.Key)}, {genericMethodIndex} }}";
			}), externArray: false, nullTerminate: false);
		}
		usedMethodsWithReversePInvokeWrappersCount = usedMethodsWithReversePInvokeWrappers.Length;
		return reversePinvokeIndices;
	}

	private static string WriteRGCTXIndices(SourceWritingContext context, ICppCodeWriter writer, GenericContextCollection genericContextCollection, List<IGenericParameterProvider> res)
	{
		string rgctxIndices = "NULL";
		if (res.Count > 0)
		{
			rgctxIndices = "s_rgctxIndices";
			writer.WriteArrayInitializer("static const Il2CppTokenRangePair", rgctxIndices, res.Select((IGenericParameterProvider item) => $"{{ 0x{item.MetadataToken.ToUInt32():X8}, {{ {genericContextCollection.GetRGCTXEntriesStartIndex(item)}, {genericContextCollection.GetRGCTXEntriesCount(item)} }} }}"), externArray: false, nullTerminate: false);
		}
		return rgctxIndices;
	}

	private static string WriteRGCTXValues(SourceWritingContext context, ICppCodeWriter writer, GenericContextCollection genericContextCollection, List<IGenericParameterProvider> res, List<RgctxEntryName> rgctxIndexNames)
	{
		string rgctxValues = "NULL";
		if (res.Count > 0)
		{
			string[] rgctxValueEntries = genericContextCollection.GetRGCTXEntries().Select(delegate(RGCTXEntry rgctxEntry)
			{
				try
				{
					string rgctxTokenOrIndexName = GetRgctxTokenOrIndexName(context, rgctxEntry);
					rgctxIndexNames.Add(new RgctxEntryName(rgctxTokenOrIndexName, rgctxEntry));
					return $"{{ (Il2CppRGCTXDataType){rgctxEntry.Type}, (const void *)&{rgctxTokenOrIndexName} }}";
				}
				catch (KeyNotFoundException e)
				{
					HandleRgctxKeyNotFoundException(context, rgctxEntry, e);
					throw;
				}
			}).ToArray();
			foreach (RgctxEntryName rgctxIndexName in rgctxIndexNames)
			{
				writer.WriteLine($"extern const {GetRgctxTokenStorageType(rgctxIndexName.Entry)} {rgctxIndexName.Name};");
			}
			rgctxValues = "s_rgctxValues";
			writer.WriteArrayInitializer("static const Il2CppRGCTXDefinition", rgctxValues, rgctxValueEntries, externArray: false, nullTerminate: false);
		}
		return rgctxValues;
	}

	private static string GetRgctxTokenStorageType(RGCTXEntry rgctxEntry)
	{
		switch (rgctxEntry.Type)
		{
		case RGCTXType.Type:
		case RGCTXType.Class:
		case RGCTXType.Method:
		case RGCTXType.Array:
			return "uint32_t";
		case RGCTXType.Constrained:
			return "Il2CppRGCTXConstrainedData";
		default:
			throw new InvalidOperationException($"Attempt to get metadata token for invalid ${"RGCTXType"} {rgctxEntry.Type}");
		}
	}

	private static string GetRgctxTokenOrIndexName(SourceWritingContext context, RGCTXEntry rgctxEntry)
	{
		switch (rgctxEntry.Type)
		{
		case RGCTXType.Type:
		case RGCTXType.Class:
		case RGCTXType.Array:
			return "g_rgctx_" + rgctxEntry.RuntimeType.Type.CppName;
		case RGCTXType.Method:
			return "g_rgctx_" + rgctxEntry.MethodReference.CppName;
		case RGCTXType.Constrained:
			return "g_rgctx_" + rgctxEntry.RuntimeType.Type.CppName + "_" + rgctxEntry.MethodReference.CppName;
		default:
			throw new InvalidOperationException($"Attempt to get metadata token for invalid ${"RGCTXType"} {rgctxEntry.Type}");
		}
	}

	private static void HandleRgctxKeyNotFoundException(ReadOnlyContext context, RGCTXEntry rgctxEntry, KeyNotFoundException e)
	{
		int depth = 0;
		string typeName = string.Empty;
		switch (rgctxEntry.Type)
		{
		case RGCTXType.Type:
		case RGCTXType.Class:
		case RGCTXType.Array:
			depth = GenericsUtilities.RecursiveGenericDepthFor((GenericInstanceType)rgctxEntry.RuntimeType.Type);
			typeName = rgctxEntry.RuntimeType.Type.FullName;
			break;
		case RGCTXType.Method:
		{
			GenericInstanceMethod method = (GenericInstanceMethod)rgctxEntry.MethodReference;
			depth = Math.Max(GenericsUtilities.RecursiveGenericDepthFor(method), GenericsUtilities.RecursiveGenericDepthFor(method.DeclaringType as GenericInstanceType));
			typeName = method.FullName;
			break;
		}
		}
		if (depth >= context.Global.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth)
		{
			throw new InvalidOperationException($"A generic type or method was used, but no code for it was generated. Consider increasing the --maximum-recursive-generic-depth command line argument to {depth + 1}.\nEncountered on: {typeName}", e);
		}
	}

	private static string WriteDebugger(SourceWritingContext context, ICppCodeWriter writer, AssemblyDefinition assembly, string cleanAssemblyName)
	{
		if (!DebugWriter.ShouldEmitDebugInformation(context.Global.InputData, assembly))
		{
			return null;
		}
		string debugMetadata = "NULL";
		if (context.Global.Parameters.EnableDebugger)
		{
			writer.WriteLine($"extern const Il2CppDebuggerMetadataRegistration g_DebuggerMetadataRegistration{cleanAssemblyName};");
			debugMetadata = "&g_DebuggerMetadataRegistration" + cleanAssemblyName;
		}
		return debugMetadata;
	}
}
