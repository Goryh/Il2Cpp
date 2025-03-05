using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.TableWriters;

public class WriteInteropDataTable : GeneratedCodeTableWriterBaseCustom<KeyValuePair<IIl2CppRuntimeType, InteropData>>
{
	protected override string TableName => "Il2CppInteropDataTable";

	protected override string CodeTableType => "Il2CppInteropData";

	protected override bool ExternTable => true;

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.InteropDataTable.Items);
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppInteropData");
	}

	protected override void WriteFileContents(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> items, Tag tag)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		List<string> tableEntries;
		using (tinyProfiler.Section("Write Declarations"))
		{
			tableEntries = WriteDeclarations(context, writer, items);
		}
		using (tinyProfiler.Section("Write Table"))
		{
			ScheduledTableWriterBase<KeyValuePair<IIl2CppRuntimeType, InteropData>, IGeneratedCodeStream>.WriteTableDeclaration(writer, tag);
			writer.BeginBlock();
			foreach (string item in tableEntries)
			{
				writer.Write(item);
				writer.WriteLine(",");
			}
			writer.EndBlock(semicolon: true);
		}
	}

	private static List<string> WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> items)
	{
		List<string> tableEntries = new List<string>(items.Count);
		foreach (KeyValuePair<IIl2CppRuntimeType, InteropData> item in items)
		{
			item.Deconstruct(out var key, out var value);
			IIl2CppRuntimeType runtimeType = key;
			InteropData interopData = value;
			TypeReference type = runtimeType.Type;
			string delegatePInvokeWrapperFunction = "NULL";
			string pinvokeMarshalToNativeFunction = "NULL";
			string pinvokeMarshalFromNativeFunction = "NULL";
			string pinvokeMarshalCleanupFunction = "NULL";
			string createCCWFunction = "NULL";
			string typeGuid = "NULL";
			IGeneratedCodeStream generatedCodeStream;
			if (interopData.HasDelegatePInvokeWrapperMethod)
			{
				delegatePInvokeWrapperFunction = context.Global.Services.Naming.ForDelegatePInvokeWrapper(type);
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C void {delegatePInvokeWrapperFunction}();");
			}
			if (interopData.HasPInvokeMarshalingFunctions)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type, MarshalType.PInvoke);
				pinvokeMarshalToNativeFunction = defaultMarshalInfoWriter.MarshalToNativeFunctionName;
				pinvokeMarshalFromNativeFunction = defaultMarshalInfoWriter.MarshalFromNativeFunctionName;
				pinvokeMarshalCleanupFunction = defaultMarshalInfoWriter.MarshalCleanupFunctionName;
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C void {pinvokeMarshalToNativeFunction}(void* managedStructure, void* marshaledStructure);");
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C void {pinvokeMarshalFromNativeFunction}(void* marshaledStructure, void* managedStructure);");
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C void {pinvokeMarshalCleanupFunction}(void* marshaledStructure);");
			}
			if (interopData.HasCreateCCWFunction)
			{
				createCCWFunction = context.Global.Services.Naming.ForCreateComCallableWrapperFunction(type);
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C Il2CppIUnknown* {createCCWFunction}(RuntimeObject* obj);");
			}
			if (interopData.HasGuid)
			{
				if (type.Resolve().HasCLSID())
				{
					generatedCodeStream = writer;
					IGeneratedCodeStream generatedCodeStream2 = generatedCodeStream;
					CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(24, 1, generatedCodeStream);
					left.AppendLiteral("const Il2CppGuid ");
					left.AppendFormatted(type.CppName);
					left.AppendLiteral("::CLSID");
					generatedCodeStream2.WriteAssignStatement(ref left, type.Resolve().GetGuid(context).ToInitializer());
					typeGuid = "&" + type.CppName + "::CLSID";
				}
				else if (type.HasIID(context))
				{
					generatedCodeStream = writer;
					IGeneratedCodeStream generatedCodeStream3 = generatedCodeStream;
					CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(22, 1, generatedCodeStream);
					left.AppendLiteral("const Il2CppGuid ");
					left.AppendFormatted(type.CppName);
					left.AppendLiteral("::IID");
					generatedCodeStream3.WriteAssignStatement(ref left, type.GetGuid(context).ToInitializer());
					typeGuid = "&" + type.CppName + "::IID";
				}
				else
				{
					TypeReference windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(context, type);
					if (windowsRuntimeType.IsWindowsRuntimeDelegate(context))
					{
						typeGuid = "&" + context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(type) + "::IID";
					}
					else
					{
						if (windowsRuntimeType == type)
						{
							throw new InvalidOperationException("InteropData says type ('" + type.FullName + "') has a GUID, but no GUID could be found for it.");
						}
						writer.AddIncludeForTypeDefinition(context, windowsRuntimeType);
						typeGuid = "&" + windowsRuntimeType.CppName + "::IID";
					}
				}
				writer.AddIncludeForTypeDefinition(context, type);
			}
			generatedCodeStream = writer;
			generatedCodeStream.WriteLine($"IL2CPP_EXTERN_C_CONST RuntimeType {context.Global.Services.Naming.ForIl2CppType(context, runtimeType)};");
			string serializedType = "&" + context.Global.Services.Naming.ForIl2CppType(context, runtimeType);
			string comment = string.Empty;
			if (context.Global.Parameters.EmitComments)
			{
				comment = " /* " + type.FullName + " */";
			}
			tableEntries.Add($"{{ {delegatePInvokeWrapperFunction}, {pinvokeMarshalToNativeFunction}, {pinvokeMarshalFromNativeFunction}, {pinvokeMarshalCleanupFunction}, {createCCWFunction}, {typeGuid}, {serializedType} }}{comment}");
		}
		return tableEntries;
	}
}
