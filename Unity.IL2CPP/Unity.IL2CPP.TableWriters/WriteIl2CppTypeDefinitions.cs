using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.TableWriters;

internal class WriteIl2CppTypeDefinitions : GeneratedCodeTableWriterBaseChunkedDualDeclarations<IIl2CppRuntimeType>
{
	public enum ArrayTerminator
	{
		None,
		Null
	}

	private readonly IMetadataCollectionResults _metadataCollection;

	protected override string TableName => "Il2CppTypeDefinitions";

	protected override string CodeTableType => "const Il2CppType* const ";

	protected override bool ExternTable => true;

	public WriteIl2CppTypeDefinitions(IMetadataCollectionResults metadataCollection)
	{
		_metadataCollection = metadataCollection;
	}

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.PrimaryWrite.Types.SortedItems, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override string FileName(ReadOnlyContext context)
	{
		return TableName + ".c";
	}

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppTypeTable");
	}

	private void WriteDeclarationsFor(SourceWritingContext context, IGeneratedCodeStream writer, IEnumerable<IGrouping<TypeReference, IIl2CppRuntimeType>> groups, Func<TypeReference, bool> skip)
	{
		foreach (IGrouping<TypeReference, IIl2CppRuntimeType> group in groups)
		{
			if (skip(group.Key))
			{
				continue;
			}
			writer.WriteLine();
			TypeReference type = group.Key;
			IIl2CppRuntimeType il2CppRuntimeType = group.First((IIl2CppRuntimeType t) => t.Type == type);
			GenericParameter genericParameter = type as GenericParameter;
			Il2CppGenericInstanceRuntimeType genericInstanceType = il2CppRuntimeType as Il2CppGenericInstanceRuntimeType;
			Il2CppArrayRuntimeType arrayType = il2CppRuntimeType as Il2CppArrayRuntimeType;
			Il2CppPtrRuntimeType pointerType = il2CppRuntimeType as Il2CppPtrRuntimeType;
			FunctionPointerType functionPointerType = type as FunctionPointerType;
			string il2CppTypeData = ((genericParameter != null) ? GetMetadataIndex(context, genericParameter, _metadataCollection.GetGenericParameterIndex) : ((genericInstanceType != null) ? WriteGenericInstanceTypeDataValue(writer, genericInstanceType) : ((arrayType != null) ? WriteArrayDataValue(writer, arrayType) : ((pointerType != null) ? WritePointerDataValue(writer, pointerType) : ((functionPointerType == null) ? GetMetadataIndex(context, type.Resolve(), _metadataCollection.GetTypeInfoIndex) : GetMetadataIndex(context, context.Global.Services.TypeProvider.SystemIntPtr, _metadataCollection.GetTypeInfoIndex))))));
			foreach (IIl2CppRuntimeType item in group)
			{
				if (!IncludeTypeDefinitionInContext(context, item))
				{
					writer.WriteExternForIl2CppType(item);
					continue;
				}
				string typeName = context.Global.Services.Naming.ForIl2CppType(context, item);
				string declaration = Il2CppTypeSupport.DeclarationFor(item.Type);
				IGeneratedCodeStream generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"extern {declaration} {typeName};");
				generatedCodeStream = writer;
				generatedCodeStream.WriteLine($"{declaration} {typeName} = {{ {il2CppTypeData}, {item.Attrs.ToString(CultureInfo.InvariantCulture)}, {Il2CppTypeSupport.NameFor(item.Type)}, {"0"}, {(item.Type.IsByReference ? "1" : "0")}, {(item.Type.IsPinned ? "1" : "0")}, {(item.Type.IsValueType ? "1" : "0")} }};");
			}
		}
	}

	protected override void WriteDeclarationsHeader(SourceWritingContext context, IGeneratedCodeStream writer)
	{
		writer.AddCodeGenMetadataIncludes();
	}

	protected override void WriteDeclarationsPart1(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<IIl2CppRuntimeType> allItems)
	{
		WriteDeclarationsFor(context, writer, context.Global.Results.SecondaryCollection.SortedMetadata.GroupedTypes, (TypeReference key) => key.IsGenericInstance);
	}

	protected override void WriteDeclarationsPart2(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<IIl2CppRuntimeType> allItems)
	{
		WriteDeclarationsFor(context, writer, context.Global.Results.SecondaryCollection.SortedMetadata.GroupedTypes, (TypeReference key) => !key.IsGenericInstance);
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, IIl2CppRuntimeType item)
	{
		writer.Write("&" + context.Global.Services.Naming.ForIl2CppType(context, item));
	}

	private static string GetMetadataIndex<T>(ReadOnlyContext context, T type, Func<T, int> getIndex) where T : TypeReference
	{
		if (!context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(type))
		{
			return "0";
		}
		return "(void*)" + getIndex(type);
	}

	private static bool IncludeTypeDefinitionInContext(ReadOnlyContext context, IIl2CppRuntimeType runtimeType)
	{
		if (runtimeType.Attrs == 0 || runtimeType.Type.GetNonPinnedAndNonByReferenceType().IsGenericParameter)
		{
			return context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(runtimeType.Type);
		}
		return true;
	}

	private static string WritePointerDataValue(IGeneratedCodeWriter writer, Il2CppPtrRuntimeType pointerType)
	{
		writer.WriteExternForIl2CppType(pointerType.ElementType);
		return "(void*)&" + writer.Context.Global.Services.Naming.ForIl2CppType(writer.Context, pointerType.ElementType);
	}

	private static string WriteGenericInstanceTypeDataValue(IGeneratedCodeWriter writer, Il2CppGenericInstanceRuntimeType genericInstanceRuntimeType)
	{
		new GenericClassWriter(writer).WriteDefinition(writer.Context, genericInstanceRuntimeType);
		return "&" + writer.Context.Global.Services.Naming.ForGenericClass(writer.Context, genericInstanceRuntimeType.Type);
	}

	private static string WriteArrayDataValue(IGeneratedCodeWriter writer, Il2CppArrayRuntimeType arrayType)
	{
		writer.WriteExternForIl2CppType(arrayType.ElementType);
		SourceWritingContext context = writer.Context;
		if (arrayType.Type.Rank == 1)
		{
			return "(void*)&" + context.Global.Services.Naming.ForIl2CppType(context, arrayType.ElementType);
		}
		string arrayTypeName = context.Global.Services.Naming.ForRuntimeArrayType(context, arrayType);
		writer.WriteLine($"Il2CppArrayType {arrayTypeName} = ");
		WriteArrayInitializer(writer, new string[6]
		{
			"&" + context.Global.Services.Naming.ForIl2CppType(context, arrayType.ElementType),
			arrayType.Type.Rank.ToString(),
			"0",
			"0",
			"NULL",
			"NULL"
		});
		return "&" + arrayTypeName;
	}

	private static void WriteArrayInitializer(IGeneratedCodeWriter writer, IEnumerable<string> initializers, ArrayTerminator terminator = ArrayTerminator.None)
	{
		writer.BeginBlock();
		foreach (string initializer in initializers)
		{
			writer.WriteLine($"{initializer},");
		}
		if (terminator == ArrayTerminator.Null)
		{
			writer.WriteLine("NULL");
		}
		writer.EndBlock(semicolon: true);
	}
}
