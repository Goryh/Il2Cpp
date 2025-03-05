using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public class MetadataUsageWriter : MetadataWriter<ICppCodeWriter>
{
	private readonly SourceWritingContext _context;

	public MetadataUsageWriter(SourceWritingContext context, ICppCodeWriter writer)
		: base(writer)
	{
		_context = context;
	}

	public void WriteMetadataUsage(TableInfo metadataUsagesTable, IMetadataUsageCollectorResults metadataUsages)
	{
		base.Writer.AddCodeGenMetadataIncludes();
		ITinyProfilerService tinyProfiler = _context.Global.Services.TinyProfiler;
		INamingService naming = _context.Global.Services.Naming;
		List<Tuple<string, uint>> items = null;
		if (_context.Global.Parameters.EnableDebugger || _context.Global.Parameters.EnableReload)
		{
			items = new List<Tuple<string, uint>>(metadataUsages.UsageCount);
		}
		ReadOnlyCollection<IIl2CppRuntimeType> sortedIl2cppRuntimeTypes;
		using (tinyProfiler.Section("Sort Il2Cpp Types"))
		{
			sortedIl2cppRuntimeTypes = metadataUsages.GetIl2CppTypes().ToSortedCollection();
		}
		foreach (IIl2CppRuntimeType type in sortedIl2cppRuntimeTypes)
		{
			base.Writer.WriteStatement(BuildMetadataInitStatement(items, "RuntimeType", naming.ForRuntimeIl2CppType(_context, type), MetadataUtils.GetEncodedMetadataUsageIndex((uint)_context.Global.Results.PrimaryWrite.Types.GetIndex(type), Il2CppMetadataUsage.Il2CppType)));
		}
		ReadOnlyCollection<IIl2CppRuntimeType> sortedTypeInfos;
		using (tinyProfiler.Section("Sort Type Infos "))
		{
			sortedTypeInfos = metadataUsages.GetTypeInfos().ToSortedCollection();
		}
		foreach (IIl2CppRuntimeType type2 in sortedTypeInfos)
		{
			base.Writer.WriteStatement(BuildMetadataInitStatement(items, "RuntimeClass", naming.ForRuntimeTypeInfo(_context, type2), MetadataUtils.GetEncodedMetadataUsageIndex((uint)_context.Global.Results.PrimaryWrite.Types.GetIndex(type2), Il2CppMetadataUsage.Il2CppClass)));
		}
		foreach (MethodReference method in _context.Global.Results.SecondaryCollection.SortedMetadata.Methods)
		{
			base.Writer.WriteStatement(BuildMetadataInitStatement(items, "RuntimeMethod", naming.ForRuntimeMethodInfo(_context, method), MetadataUtils.GetEncodedMethodMetadataUsageIndex(method, _context.Global.PrimaryCollectionResults.Metadata, _context.Global.PrimaryWriteResults.GenericMethods)));
		}
		foreach (KeyValuePair<Il2CppRuntimeFieldReference, uint> field in _context.Global.Results.SecondaryCollection.FieldReferenceTable.Items)
		{
			base.Writer.WriteStatement(BuildMetadataInitStatement(items, "RuntimeField", naming.ForRuntimeFieldInfo(_context, field.Key), MetadataUtils.GetEncodedMetadataUsageIndex(field.Value, Il2CppMetadataUsage.FieldInfo)));
			if (field.Key.Field.FieldDef.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(items, "char", naming.ForRuntimeFieldRvaStructStorage(_context, field.Key), MetadataUtils.GetEncodedMetadataUsageIndex(field.Value, Il2CppMetadataUsage.FieldRva)));
			}
		}
		foreach (KeyValuePair<string, uint> stringMetadataToken in _context.Global.Results.SecondaryCollection.StringLiteralTable.Items)
		{
			base.Writer.WriteStatement(BuildMetadataInitStatement(items, "String_t", naming.ForRuntimeUniqueStringLiteralIdentifier(_context, stringMetadataToken.Key), MetadataUtils.GetEncodedMetadataUsageIndex(stringMetadataToken.Value, Il2CppMetadataUsage.StringLiteral)));
		}
		if (metadataUsagesTable.Count > 0)
		{
			base.Writer.WriteTable("void** const", _context.Global.Services.ContextScope.ForMetadataGlobalVar("g_MetadataUsages"), items, (Tuple<string, uint> x) => "(void**)&" + x.Item1, externTable: true);
		}
		if (!_context.Global.Parameters.EnableReload)
		{
			return;
		}
		base.Writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
		TableInfo tokensTable = base.Writer.WriteTable("uint32_t", "s_MetaDataTokenReload", items, (Tuple<string, uint> x) => x.Item2.ToString(), externTable: false);
		int index = 0;
		ReadOnlyCollection<KeyValuePair<string, MethodMetadataUsage>> usagePairs = metadataUsages.GetUsages().ItemsSortedByKey();
		ICppCodeWriter writer;
		foreach (KeyValuePair<string, MethodMetadataUsage> usagePair in usagePairs)
		{
			writer = base.Writer;
			writer.WriteStatement($"extern const uint32_t {usagePair.Key}");
			writer = base.Writer;
			writer.WriteStatement($"const uint32_t {usagePair.Key} = {index++}");
		}
		string methodMetadataInitialized = _context.Global.Services.ContextScope.ForReloadMethodMetadataInitialized();
		writer = base.Writer;
		writer.WriteStatement($"bool {methodMetadataInitialized}[{usagePairs.Count}]");
		base.Writer.WriteLine("void ClearMethodMetadataInitializedFlags()");
		base.Writer.BeginBlock();
		writer = base.Writer;
		writer.WriteStatement($"memset({methodMetadataInitialized}, 0, sizeof({methodMetadataInitialized}))");
		base.Writer.WriteLine();
		writer = base.Writer;
		writer.WriteLine($"for(int32_t i = 0; i < {items.Count}; i++)");
		base.Writer.Indent();
		writer = base.Writer;
		writer.WriteStatement($"*(uintptr_t*){metadataUsagesTable.Name}[i] = (uintptr_t){tokensTable.Name}[i]");
		base.Writer.Dedent();
		base.Writer.EndBlock();
		base.Writer.WriteLine("#endif");
	}

	private static string BuildMetadataInitStatement(List<Tuple<string, uint>> tokens, string type, string name, uint token)
	{
		tokens?.Add(new Tuple<string, uint>(name, token));
		return $"{type}* {name} = ({type}*)(uintptr_t){token}";
	}
}
