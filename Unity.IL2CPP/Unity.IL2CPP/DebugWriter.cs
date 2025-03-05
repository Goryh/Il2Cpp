using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP;

internal static class DebugWriter
{
	internal static void WriteDebugMetadata(SourceWritingContext sourceWritingContext, AssemblyDefinition assembly, SequencePointCollector sequencePointCollector, ICatchPointProvider catchPointProvider)
	{
		MethodDefinition[] methods = (from m in assembly.GetAllTypes().SelectMany((TypeDefinition t) => t.Methods)
			orderby m.MetadataToken.RID
			select m).ToArray();
		using ICppCodeStream writer = sourceWritingContext.CreateProfiledSourceWriterInOutputDirectory(FileCategory.Debugger, sourceWritingContext.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "Debugger.c"));
		if (ShouldEmitDebugInformation(sourceWritingContext.Global.InputData, assembly))
		{
			writer.AddCodeGenMetadataIncludes();
			WriteVariables(sequencePointCollector, writer, sourceWritingContext.Global.Results.PrimaryWrite.Types);
			WriteStringTable(sequencePointCollector, writer);
			WriteVariableRanges(sequencePointCollector, writer, methods);
			string sequencePointVariable = GetSequencePointName(sourceWritingContext, assembly);
			WriteSequencePoints(sourceWritingContext, sequencePointVariable, sequencePointCollector, writer);
			WriteCatchPoints(sourceWritingContext, catchPointProvider, writer);
			WriteSourceFileTable(sequencePointCollector, writer);
			int typeSourcePairsCount = WriteTypeSourceMap(sourceWritingContext, sequencePointCollector, writer);
			WriteScopes(sequencePointCollector, writer);
			WriteScopeMap(sequencePointCollector, writer, methods);
			writer.WriteStructInitializer("const Il2CppDebuggerMetadataRegistration", "g_DebuggerMetadataRegistration" + assembly.CleanFileName, new string[12]
			{
				"(Il2CppMethodExecutionContextInfo*)g_methodExecutionContextInfos",
				"(Il2CppMethodExecutionContextInfoIndex*)g_methodExecutionContextInfoIndexes",
				"(Il2CppMethodScope*)g_methodScopes",
				"(Il2CppMethodHeaderInfo*)g_methodHeaderInfos",
				"(Il2CppSequencePointSourceFile*)g_sequencePointSourceFiles",
				$"{((ISequencePointCollector)sequencePointCollector).NumSeqPoints}",
				"(Il2CppSequencePoint*)" + sequencePointVariable,
				$"{catchPointProvider.NumCatchPoints}",
				"(Il2CppCatchPoint*)g_catchPoints",
				$"{typeSourcePairsCount}",
				"(Il2CppTypeSourceFilePair*)g_typeSourceFiles",
				"(const char**)g_methodExecutionContextInfoStrings"
			}, externStruct: true);
		}
	}

	public static bool ShouldEmitDebugInformation(AssemblyConversionInputData inputData, AssemblyDefinition assembly)
	{
		string assemblyFileName = Path.GetFileNameWithoutExtension(assembly.MainModule.GetModuleFileName());
		if (!inputData.DebugAssemblyName.Any())
		{
			return true;
		}
		return inputData.DebugAssemblyName.Any((string debugAssemblyName) => debugAssemblyName.Contains(assemblyFileName));
	}

	internal static string GetSequencePointName(ReadOnlyContext context, AssemblyDefinition assembly)
	{
		return "g_sequencePoints" + assembly.CleanFileName;
	}

	private static void WriteScopeMap(SequencePointCollector sequencePointCollector, ICppCodeStream writer, MethodDefinition[] methods)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		if (methods.Length != 0)
		{
			writer.WriteArrayInitializer("static const Il2CppMethodHeaderInfo", "g_methodHeaderInfos", methods.Select((MethodDefinition method) => GetScopeRange(writer.Context, sequencePointCollector, method)), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const Il2CppMethodHeaderInfo g_methodHeaderInfos[1] = { { 0, 0, 0 } };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppMethodHeaderInfo g_methodHeaderInfos[1] = { { 0, 0, 0 } };");
		writer.WriteLine("#endif");
	}

	private static string GetScopeRange(ReadOnlyContext context, ISequencePointCollector sequencePointCollector, MethodDefinition method)
	{
		StringBuilder builder = new StringBuilder();
		if (sequencePointCollector.TryGetScopeRange(method, out var range1))
		{
			StringBuilder stringBuilder = builder;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
			handler.AppendLiteral("{ ");
			handler.AppendFormatted(method.Body.CodeSize);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(range1.Start);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(range1.Length);
			handler.AppendLiteral(" }");
			stringBuilder2.Append(ref handler);
		}
		else
		{
			builder.Append("{ 0, 0, 0 }");
		}
		if (context.Global.Parameters.EmitComments)
		{
			StringBuilder stringBuilder = builder;
			StringBuilder stringBuilder3 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder);
			handler.AppendLiteral(" /* ");
			handler.AppendFormatted(method.FullName);
			handler.AppendLiteral(" */");
			stringBuilder3.Append(ref handler);
		}
		return builder.ToString();
	}

	private static void WriteScopes(SequencePointCollector sequencePointCollector, ICppCodeStream writer)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		ReadOnlyCollection<Unity.IL2CPP.Debugger.Range> scopes = sequencePointCollector.GetScopes();
		if (scopes.Count > 0)
		{
			writer.WriteArrayInitializer("static const Il2CppMethodScope", "g_methodScopes", scopes.Select((Unity.IL2CPP.Debugger.Range scope) => $"{{ {scope.Start}, {scope.Length} }}"), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const Il2CppMethodScope g_methodScopes[1] = { { 0, 0 } };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppMethodScope g_methodScopes[1] = { { 0, 0 } };");
		writer.WriteLine("#endif");
	}

	private static int WriteTypeSourceMap(SourceWritingContext context, SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
	{
		ReadOnlyCollection<SequencePointInfo> allSequencePoints = ((ISequencePointCollector)sequencePointCollector).GetAllSequencePoints();
		Dictionary<TypeDefinition, List<string>> typeToSourceFileMap = new Dictionary<TypeDefinition, List<string>>();
		int numEntries = 0;
		foreach (SequencePointInfo seqPoint1 in allSequencePoints)
		{
			if (string.IsNullOrEmpty(seqPoint1.SourceFile))
			{
				continue;
			}
			if (!typeToSourceFileMap.TryGetValue(seqPoint1.Method.DeclaringType, out var files))
			{
				files = new List<string>();
				typeToSourceFileMap.Add(seqPoint1.Method.DeclaringType, files);
			}
			bool found = false;
			foreach (string item in files)
			{
				if (item == seqPoint1.SourceFile)
				{
					found = true;
				}
			}
			if (!found)
			{
				files.Add(seqPoint1.SourceFile);
				numEntries++;
			}
		}
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		if (numEntries > 0)
		{
			writer.WriteArrayInitializer("static const Il2CppTypeSourceFilePair", "g_typeSourceFiles", typeToSourceFileMap.SelectMany((KeyValuePair<TypeDefinition, List<string>> kvp) => kvp.Value.Select((string file) => $"{{ {context.Global.Results.PrimaryCollection.Metadata.GetTypeInfoIndex(kvp.Key)}, {((ISequencePointCollector)sequencePointCollector).GetSourceFileIndex(file)} }}")), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const Il2CppTypeSourceFilePair g_typeSourceFiles[1] = { { 0, 0 } };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppTypeSourceFilePair g_typeSourceFiles[1] = { { 0, 0 } };");
		writer.WriteLine("#endif");
		return numEntries;
	}

	private static void WriteSourceFileTable(SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
	{
		ReadOnlyCollection<ISequencePointSourceFileData> sourceFiles = ((ISequencePointCollector)sequencePointCollector).GetAllSourceFiles();
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		if (sourceFiles.Count == 0)
		{
			writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[1] = { NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };");
		}
		else
		{
			writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[] = {");
			int index = 0;
			foreach (ISequencePointSourceFileData fileData in sourceFiles)
			{
				ICppCodeWriter cppCodeWriter = writer;
				cppCodeWriter.Write($"{{ {Formatter.AsUTF8CppStringLiteral(fileData.File)}, {{ ");
				if (fileData.Hash != null && fileData.Hash.Length >= 16)
				{
					for (int i3 = 0; i3 < 16; i3++)
					{
						writer.Write(fileData.Hash[i3].ToString());
						if (i3 != 15)
						{
							writer.Write(", ");
						}
					}
				}
				else
				{
					writer.Write("0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0");
				}
				writer.Write("} },");
				if (writer.Context.Global.Parameters.EmitComments)
				{
					cppCodeWriter = writer;
					cppCodeWriter.Write($" //{index} ");
				}
				writer.WriteLine();
				index++;
			}
			writer.WriteLine("};");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppSequencePointSourceFile g_sequencePointSourceFiles[1] = { NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };");
		writer.WriteLine("#endif");
	}

	private static void WriteSequencePoints(SourceWritingContext context, string variableName, SequencePointCollector sequencePointCollector, ICppCodeWriter writer)
	{
		ReadOnlyCollection<SequencePointInfo> seqPoints = ((ISequencePointCollector)sequencePointCollector).GetAllSequencePoints();
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		ICppCodeWriter cppCodeWriter;
		if (seqPoints.Count > 0)
		{
			writer.WriteArrayInitializer("Il2CppSequencePoint", variableName, seqPoints.Select((SequencePointInfo seqPoint, int index) => GetSequencePoint(context, sequencePointCollector, context.Global.Results.PrimaryCollection.Metadata, seqPoint, index)), externArray: true, nullTerminate: false);
		}
		else
		{
			cppCodeWriter = writer;
			cppCodeWriter.WriteLine($"extern Il2CppSequencePoint {variableName}[];");
			cppCodeWriter = writer;
			cppCodeWriter.WriteLine($"Il2CppSequencePoint {variableName}[1] = {{ {{ 0, 0, 0, 0, 0, 0, 0, kSequencePointKind_Normal, 0, 0, }} }};");
		}
		writer.WriteLine("#else");
		cppCodeWriter = writer;
		cppCodeWriter.WriteLine($"extern Il2CppSequencePoint {variableName}[];");
		cppCodeWriter = writer;
		cppCodeWriter.WriteLine($"Il2CppSequencePoint {variableName}[1] = {{ {{ 0, 0, 0, 0, 0, 0, 0, kSequencePointKind_Normal, 0, 0, }} }};");
		writer.WriteLine("#endif");
	}

	private static void WriteCatchPoints(SourceWritingContext context, ICatchPointProvider catchPointProvider, ICppCodeWriter writer)
	{
		IEnumerable<CatchPointInfo> catchPoints = catchPointProvider.AllCatchPoints;
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		if (catchPointProvider.NumCatchPoints > 0)
		{
			writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[] = {");
			foreach (CatchPointInfo catchPoint in catchPoints)
			{
				writer.WriteLine($"{GetCatchPoint(context, catchPoint)},");
			}
			writer.WriteLine("};");
		}
		else
		{
			writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[1] = { { 0, 0, 0, 0, } };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppCatchPoint g_catchPoints[1] = { { 0, 0, 0, 0, } };");
		writer.WriteLine("#endif");
	}

	private static string GetSequencePoint(ReadOnlyContext context, ISequencePointCollector sequencePointCollector, IMetadataCollectionResults metadataCollector, SequencePointInfo seqPoint, int index)
	{
		string sequencePointKind = seqPoint.Kind switch
		{
			SequencePointKind.Normal => "kSequencePointKind_Normal", 
			SequencePointKind.StepOut => "kSequencePointKind_StepOut", 
			_ => throw new NotSupportedException(), 
		};
		StringBuilder builder = new StringBuilder();
		StringBuilder stringBuilder = builder;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder);
		handler.AppendLiteral("{ ");
		handler.AppendFormatted(metadataCollector.GetMethodIndex(seqPoint.Method));
		handler.AppendLiteral(", ");
		stringBuilder2.Append(ref handler);
		stringBuilder = builder;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder);
		handler.AppendFormatted(sequencePointCollector.GetSourceFileIndex(seqPoint.SourceFile));
		handler.AppendLiteral(", ");
		stringBuilder3.Append(ref handler);
		stringBuilder = builder;
		StringBuilder stringBuilder4 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder);
		handler.AppendFormatted(seqPoint.StartLine);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(seqPoint.EndLine);
		handler.AppendLiteral(", ");
		stringBuilder4.Append(ref handler);
		stringBuilder = builder;
		StringBuilder stringBuilder5 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder);
		handler.AppendFormatted(seqPoint.StartColumn);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(seqPoint.EndColumn);
		handler.AppendLiteral(", ");
		stringBuilder5.Append(ref handler);
		stringBuilder = builder;
		StringBuilder stringBuilder6 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 3, stringBuilder);
		handler.AppendFormatted(seqPoint.IlOffset);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(sequencePointKind);
		handler.AppendLiteral(", 0, ");
		handler.AppendFormatted(sequencePointCollector.GetSeqPointIndex(seqPoint));
		handler.AppendLiteral(" }");
		stringBuilder6.Append(ref handler);
		if (context.Global.Parameters.EmitComments)
		{
			stringBuilder = builder;
			StringBuilder stringBuilder7 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder);
			handler.AppendLiteral(" /* seqPointIndex: ");
			handler.AppendFormatted(index);
			handler.AppendLiteral(" */");
			stringBuilder7.Append(ref handler);
		}
		return builder.ToString();
	}

	private static string GetCatchPoint(SourceWritingContext context, CatchPointInfo catchPoint)
	{
		int catchTypeIndex = ((catchPoint.CatchHandler == null || catchPoint.CatchHandler.CatchType == null) ? (-1) : context.Global.Results.PrimaryWrite.Types.GetIndex(catchPoint.RuntimeType));
		return $"{{ {context.Global.Results.PrimaryCollection.Metadata.GetMethodIndex(catchPoint.Method)}, {catchTypeIndex}, {catchPoint.IlOffset}, {catchPoint.TryId}, {catchPoint.ParentTryId} }}";
	}

	private static void WriteVariableRanges(SequencePointCollector sequencePointCollector, ICppCodeWriter writer, MethodDefinition[] methods)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		if (methods.Length != 0)
		{
			writer.WriteArrayInitializer("static const Il2CppMethodExecutionContextInfoIndex", "g_methodExecutionContextInfoIndexes", methods.Select((MethodDefinition method) => GetVariableRange(writer.Context, sequencePointCollector, method)), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const Il2CppMethodExecutionContextInfoIndex g_methodExecutionContextInfoIndexes[1] = { { 0, 0} };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppMethodExecutionContextInfoIndex g_methodExecutionContextInfoIndexes[1] = { { 0, 0} };");
		writer.WriteLine("#endif");
	}

	private static string GetVariableRange(ReadOnlyContext context, ISequencePointCollector sequencePointCollector, MethodDefinition method)
	{
		sequencePointCollector.TryGetVariableRange(method, out var range);
		StringBuilder builder = new StringBuilder();
		StringBuilder stringBuilder = builder;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder);
		handler.AppendLiteral("{ ");
		handler.AppendFormatted(range.Start);
		handler.AppendLiteral(", ");
		handler.AppendFormatted(range.Length);
		handler.AppendLiteral(" }");
		stringBuilder2.Append(ref handler);
		if (context.Global.Parameters.EmitComments)
		{
			stringBuilder = builder;
			StringBuilder stringBuilder3 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder);
			handler.AppendLiteral(" /* 0x");
			handler.AppendFormatted(method.MetadataToken.ToUInt32(), "X8");
			handler.AppendLiteral(" ");
			handler.AppendFormatted(method.FullName);
			handler.AppendLiteral(" */");
			stringBuilder3.Append(ref handler);
		}
		return builder.ToString();
	}

	private static void WriteStringTable(SequencePointCollector sequencePointCollector, ICppCodeStream writer)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		ReadOnlyCollection<string> strings = ((ISequencePointCollector)sequencePointCollector).GetAllContextInfoStrings();
		if (strings.Count > 0)
		{
			writer.WriteArrayInitializer("static const char*", "g_methodExecutionContextInfoStrings", strings.Select((string str) => Formatter.AsUTF8CppStringLiteral(str) ?? ""), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const char* g_methodExecutionContextInfoStrings[1] = { NULL };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const char* g_methodExecutionContextInfoStrings[1] = { NULL };");
		writer.WriteLine("#endif");
	}

	private static void WriteVariables(SequencePointCollector sequencePointCollector, ICppCodeWriter writer, ITypeCollectorResults types)
	{
		writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
		ReadOnlyCollection<VariableData> variables = ((ISequencePointCollector)sequencePointCollector).GetVariables();
		if (variables.Count > 0)
		{
			writer.WriteArrayInitializer("static const Il2CppMethodExecutionContextInfo", "g_methodExecutionContextInfos", variables.Select((VariableData variable, int index) => FormatVariable(writer.Context, types, variable, index)), externArray: false, nullTerminate: false);
		}
		else
		{
			writer.WriteLine("static const Il2CppMethodExecutionContextInfo g_methodExecutionContextInfos[1] = { { 0, 0, 0 } };");
		}
		writer.WriteLine("#else");
		writer.WriteLine("static const Il2CppMethodExecutionContextInfo g_methodExecutionContextInfos[1] = { { 0, 0, 0 } };");
		writer.WriteLine("#endif");
	}

	private static string FormatVariable(ReadOnlyContext context, ITypeCollectorResults types, VariableData variable, int index)
	{
		StringBuilder builder = new StringBuilder();
		StringBuilder stringBuilder = builder;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 3, stringBuilder);
		handler.AppendLiteral("{ ");
		handler.AppendFormatted(types.GetIndex(variable.type));
		handler.AppendLiteral(", ");
		handler.AppendFormatted(variable.NameIndex);
		handler.AppendLiteral(",  ");
		handler.AppendFormatted(variable.ScopeIndex);
		handler.AppendLiteral(" }");
		stringBuilder2.Append(ref handler);
		if (context.Global.Parameters.EmitComments)
		{
			stringBuilder = builder;
			StringBuilder stringBuilder3 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder);
			handler.AppendLiteral(" /*tableIndex: ");
			handler.AppendFormatted(index);
			handler.AppendLiteral(" */");
			stringBuilder3.Append(ref handler);
		}
		return builder.ToString();
	}
}
