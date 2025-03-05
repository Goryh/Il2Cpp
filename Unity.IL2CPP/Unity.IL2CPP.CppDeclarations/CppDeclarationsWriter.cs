using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.DataModel.Awesome.Ordering;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.CppDeclarations;

internal class CppDeclarationsWriter
{
	public static void Write(ICodeStream codeWriter, IDirectWriter writer, ICppDeclarationsBasic declarationsIn)
	{
		using (codeWriter.Context.Global.Services.TinyProfiler.Section("Write CppDeclarations"))
		{
			writer.WriteLine();
			WriteBasicIncludes(writer, declarationsIn);
			writer.WriteLine();
			foreach (string declaration in declarationsIn.RawTypeForwardDeclarations.ToSortedCollection())
			{
				writer.WriteLine(declaration + ";");
			}
			writer.WriteLine();
			writer.WriteLine();
			foreach (string declaration2 in declarationsIn.RawMethodForwardDeclarations)
			{
				writer.WriteLine(declaration2 + ";");
			}
			writer.WriteLine();
			codeWriter.Flush();
		}
	}

	public static void WriteWithInteropGuidCollection(SourceWritingContext context, ICodeStream writer, ICppDeclarations declarationsIn)
	{
		using (context.Global.Services.TinyProfiler.Section("Write CppDeclarations"))
		{
			ReadOnlyCollection<CppDeclarationsData> sorted = Write(context, writer, declarationsIn);
			AddInteropGuids(context, sorted);
		}
	}

	public static void WriteWithoutInteropGuidCollection(ReadOnlyContext context, ICodeStream writer, ICppDeclarations declarationsIn)
	{
		using (context.Global.Services.TinyProfiler.Section("Write CppDeclarations"))
		{
			Write(context, writer, declarationsIn);
		}
	}

	private static ReadOnlyCollection<CppDeclarationsData> Write(ReadOnlyContext context, ICodeStream writer, ICppDeclarations declarationsIn)
	{
		CppDeclarations declarations;
		ReadOnlyCollection<CppDeclarationsData> sorted = CollectDeclarations(context, declarationsIn, out declarations);
		writer.WriteLine();
		WriteIncludes(writer, declarations);
		WriteVirtualMethodDeclaration(writer, writer, declarations.VirtualMethods);
		WriteForwardDeclarations(context, writer, declarations);
		WriteExterns(context, writer, declarations);
		WriteDefinitions(writer, writer, declarationsIn, sorted);
		WriteDeclarations(context, writer, writer, declarations);
		writer.Flush();
		return sorted;
	}

	private static void WriteIncludes(IDirectWriter writer, CppDeclarations declarations)
	{
		foreach (string stmt in declarations.RawFileLevelPreprocessorStmts)
		{
			writer.WriteLine(stmt);
		}
		writer.WriteLine();
		WriteBasicIncludes(writer, declarations);
	}

	private static void WriteBasicIncludes(IDirectWriter writer, ICppDeclarationsBasic declarations)
	{
		string[] includesToSkip = new string[3] { "\"il2cpp-config.h\"", "<alloca.h>", "<malloc.h>" };
		foreach (string include in declarations.Includes.Where((string i) => !includesToSkip.Contains(i) && i.StartsWith("<")))
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"#include {include}");
		}
		writer.WriteLine();
		foreach (string include2 in declarations.Includes.Where((string i) => !includesToSkip.Contains(i) && !i.StartsWith("<")))
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"#include {include2}");
		}
		writer.WriteLine();
	}

	private static void WriteForwardDeclarations(ReadOnlyContext context, IDirectWriter writer, CppDeclarations declarations)
	{
		foreach (TypeReference type in declarations.ForwardDeclarations.ToSortedCollection())
		{
			if (!type.IsSystemObject && !type.IsSystemArray && type != context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference)
			{
				if (context.Global.Parameters.EmitComments)
				{
					writer.WriteLine(Emit.Comment(type.FullName));
				}
				if (type.GetRuntimeStorage(context) == RuntimeStorageKind.VariableSizedValueType)
				{
					IDirectWriter directWriter = writer;
					directWriter.WriteLine($"typedef {"Il2CppFullySharedGenericStruct"} {context.Global.Services.Naming.ForType(type)};");
				}
				else
				{
					IDirectWriter directWriter = writer;
					directWriter.WriteLine($"struct {context.Global.Services.Naming.ForType(type)};");
				}
			}
		}
		writer.WriteLine();
		foreach (string declaration in declarations.RawTypeForwardDeclarations.ToSortedCollection())
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"{declaration};");
		}
		writer.WriteLine();
		foreach (ArrayType arrayType in declarations.ArrayTypes.ToSortedCollection())
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"struct {context.Global.Services.Naming.ForType(arrayType)};");
		}
		writer.WriteLine();
	}

	private static void WriteDefinitions(IDirectWriter writer, ICodeWriter codeWriter, ICppDeclarations declarationsIn, ReadOnlyCollection<CppDeclarationsData> sorted)
	{
		if (sorted.Count <= 0)
		{
			return;
		}
		writer.WriteClangWarningDisables();
		foreach (CppDeclarationsData type in sorted)
		{
			codeWriter.Write(type.Instance.Definition);
		}
		foreach (CppDeclarationsData type2 in sorted)
		{
			if (declarationsIn.TypeIncludes.Contains(type2.Type))
			{
				if (type2.Static.Definition.Length > 0)
				{
					codeWriter.Write(type2.Static.Definition);
				}
				if (type2.ThreadStatic.Definition.Length > 0)
				{
					codeWriter.Write(type2.ThreadStatic.Definition);
				}
			}
		}
		writer.WriteClangWarningEnables();
	}

	private static void AddInteropGuids(SourceWritingContext context, ReadOnlyCollection<CppDeclarationsData> sorted)
	{
		foreach (CppDeclarationsData type in sorted)
		{
			if (type.TypesRequiringInteropGuids != null)
			{
				context.Global.Collectors.InteropGuids.Add(context, type.TypesRequiringInteropGuids);
			}
		}
	}

	private static void WriteDeclarations(ReadOnlyContext context, IDirectWriter writer, ICodeWriter codeWriter, CppDeclarations declarations)
	{
		foreach (ArrayType arrayType in declarations.ArrayTypes)
		{
			TypeDefinitionWriter.WriteArrayTypeDefinition(context, arrayType, codeWriter);
		}
		writer.WriteLine();
		foreach (string declaration in declarations.RawMethodForwardDeclarations)
		{
			writer.WriteLine($"{declaration};");
		}
		writer.WriteLine();
		foreach (MethodReference sharedMethod in declarations.SharedMethods)
		{
			WriteSharedMethodDeclaration(context, sharedMethod, writer);
		}
		writer.WriteLine();
		foreach (MethodReference method in declarations.Methods)
		{
			WriteMethodDeclaration(context, method, writer);
		}
		foreach (string internalPInvokeDeclaration in declarations.InternalPInvokeMethodDeclarations.Values)
		{
			writer.Write(internalPInvokeDeclaration);
		}
		foreach (string internalPInvokeDeclaration2 in declarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Values)
		{
			writer.Write(internalPInvokeDeclaration2);
		}
	}

	private static void WriteExterns(ReadOnlyContext context, IDirectWriter writer, CppDeclarations declarations)
	{
		writer.WriteLine("IL2CPP_EXTERN_C_BEGIN");
		foreach (IIl2CppRuntimeType type in declarations.TypeExterns)
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"extern {Il2CppTypeSupport.DeclarationFor(type.Type)} {context.Global.Services.Naming.ForIl2CppType(context, type)};");
		}
		foreach (IIl2CppRuntimeType[] args in declarations.GenericInstExterns)
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"extern const Il2CppGenericInst {context.Global.Services.Naming.ForGenericInst(context, args)};");
		}
		foreach (TypeReference type2 in declarations.GenericClassExterns)
		{
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"extern Il2CppGenericClass {context.Global.Services.Naming.ForGenericClass(context, type2)};");
		}
		writer.WriteLine("IL2CPP_EXTERN_C_END");
		writer.WriteLine();
	}

	private static ReadOnlyCollection<CppDeclarationsData> CollectDeclarations(ReadOnlyContext context, ICppDeclarations declarationsIn, out CppDeclarations declarations)
	{
		List<CppDeclarationsData> list = declarationsIn.TypeIncludes.GetCppDeclarations(context).ToList();
		List<CppDeclarationsData> allIncludeTypeData = new List<CppDeclarationsData>(list);
		HashSet<TypeReference> allIncludes = new HashSet<TypeReference>(declarationsIn.TypeIncludes);
		foreach (CppDeclarationsData depCacheData in list.GetCppDeclarationsDependencies(context))
		{
			if (allIncludes.Add(depCacheData.Type))
			{
				allIncludeTypeData.Add(depCacheData);
			}
		}
		allIncludeTypeData.Sort(new CppDeclarationsComparer(context));
		declarations = new CppDeclarations();
		declarations.Add(declarationsIn);
		foreach (CppDeclarationsData item in allIncludeTypeData)
		{
			declarations.Add(item.Instance.Declarations);
		}
		foreach (CppDeclarationsData item2 in allIncludeTypeData)
		{
			declarations.Add(item2.Static.Declarations);
			declarations.Add(item2.ThreadStatic.Declarations);
		}
		return allIncludeTypeData.AsReadOnly();
	}

	private static void WriteVirtualMethodDeclaration(IDirectWriter writer, ICodeWriter codeWriter, IEnumerable<VirtualMethodDeclarationData> virtualMethodDeclarationData)
	{
		HashSet<InvokerData> virtuals = new HashSet<InvokerData>();
		HashSet<InvokerData> virtualGenerics = new HashSet<InvokerData>();
		HashSet<InvokerData> interfaces = new HashSet<InvokerData>();
		HashSet<InvokerData> interfaceGenerics = new HashSet<InvokerData>();
		HashSet<InvokerData> invokerCall = new HashSet<InvokerData>();
		HashSet<InvokerData> constrainedCall = new HashSet<InvokerData>();
		foreach (VirtualMethodDeclarationData virtualMethodDeclaration in virtualMethodDeclarationData)
		{
			HashSet<InvokerData> methodList = null;
			switch (virtualMethodDeclaration.CallType)
			{
			case VirtualMethodCallType.Interface:
				methodList = interfaces;
				break;
			case VirtualMethodCallType.GenericInterface:
				methodList = interfaceGenerics;
				break;
			case VirtualMethodCallType.Virtual:
				methodList = virtuals;
				break;
			case VirtualMethodCallType.GenericVirtual:
				methodList = virtualGenerics;
				break;
			case VirtualMethodCallType.InvokerCall:
				methodList = invokerCall;
				break;
			case VirtualMethodCallType.ConstrainedInvokerCall:
				methodList = constrainedCall;
				break;
			}
			methodList.Add(new InvokerData(virtualMethodDeclaration.ReturnsVoid, virtualMethodDeclaration.DoCallViaInvoker, virtualMethodDeclaration.Parameters));
		}
		foreach (InvokerData[] invokerGroup in GetInvokerGroup(virtuals))
		{
			InterfaceAndVirtualInvokeWriter.WriteVirtual(codeWriter, invokerGroup);
		}
		foreach (InvokerData[] invokerGroup2 in GetInvokerGroup(virtualGenerics))
		{
			InterfaceAndVirtualInvokeWriter.WriteGenericVirtual(codeWriter, invokerGroup2);
		}
		foreach (InvokerData[] invokerGroup3 in GetInvokerGroup(interfaces))
		{
			InterfaceAndVirtualInvokeWriter.WriteInterface(codeWriter, invokerGroup3);
		}
		foreach (InvokerData[] invokerGroup4 in GetInvokerGroup(interfaceGenerics))
		{
			InterfaceAndVirtualInvokeWriter.WriteGenericInterface(codeWriter, invokerGroup4);
		}
		foreach (InvokerData[] invokerGroup5 in GetInvokerGroup(invokerCall))
		{
			InterfaceAndVirtualInvokeWriter.WriteInvokerCall(codeWriter, invokerGroup5);
		}
		foreach (InvokerData[] invokerGroup6 in GetInvokerGroup(constrainedCall))
		{
			InterfaceAndVirtualInvokeWriter.WriteConstrainedCall(codeWriter, invokerGroup6);
		}
		writer.WriteLine();
	}

	private static IEnumerable<InvokerData[]> GetInvokerGroup(IEnumerable<InvokerData> invokers)
	{
		return from g in invokers
			group g by (VoidReturn: g.VoidReturn, DoCallViaInvoker: g.DoCallViaInvoker, Count: g.Parameters.Count) into g
			orderby g.Key.VoidReturn descending, g.Key.Count, g.Key.DoCallViaInvoker
			select g.ToArray();
	}

	private static void WriteSharedMethodDeclaration(ReadOnlyContext context, MethodReference method, IDirectWriter writer)
	{
		bool num = method.ShouldInline(context.Global.Parameters);
		if (!method.IsSharedMethod(context))
		{
			throw new InvalidOperationException();
		}
		if (context.Global.Parameters.EmitComments)
		{
			writer.WriteLine(Emit.Comment(method.FullName));
		}
		if (num)
		{
			writer.Write(MethodSignatureWriter.GetSharedMethodSignatureRawInline(context, method));
		}
		else
		{
			writer.Write(MethodSignatureWriter.GetSharedMethodSignatureRaw(context, method));
		}
		writer.WriteLine(";");
	}

	private static void WriteMethodDeclaration(ReadOnlyContext context, MethodReference method, IDirectWriter writer)
	{
		bool shouldInline = method.ShouldInline(context.Global.Parameters);
		if (GenericsUtilities.CheckForMaximumRecursion(context, method.DeclaringType) && context.Global.Parameters.DisableFullGenericSharing)
		{
			string attributes = MethodSignatureWriter.BuildMethodAttributes(context, method);
			string inlineSuffix = (shouldInline ? "_inline" : "");
			writer.WriteLine(MethodSignatureWriter.GetMethodSignature(method.CppName + inlineSuffix, MethodSignatureWriter.FormatReturnType(context, method.GetResolvedReturnType(context)), MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "inline", attributes));
			writer.WriteLine("{");
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"\t{Emit.RaiseManagedException("il2cpp_codegen_get_maximum_nested_generics_exception()")};");
			writer.WriteLine("}");
			return;
		}
		if (context.Global.Parameters.EmitComments)
		{
			writer.WriteLine(Emit.Comment(method.FullName));
		}
		if (method.CanShare(context))
		{
			bool callingMethodReturnsByRef = method.GetResolvedReturnType(context).IsReturnedByRef(context);
			MethodReference sharedMethod = method.GetSharedMethod(context);
			TypeReference sharedResolvedReturnType = GenericParameterResolver.ResolveReturnTypeIfNeeded(context.Global.Services.TypeFactory, sharedMethod);
			bool sharedMethodReturnsByRef = sharedResolvedReturnType.IsReturnedByRef(context);
			bool hasFullGenericSharingSignature = sharedMethod.HasFullGenericSharingSignature(context);
			string inlineSuffix2 = (shouldInline ? "_inline" : "");
			IDirectWriter directWriter = writer;
			directWriter.WriteLine($"inline {MethodSignatureWriter.FormatReturnType(context, method.GetResolvedReturnType(context))} {method.CppName + inlineSuffix2} ({MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true)})");
			writer.WriteLine("{");
			writer.Write("\t");
			if (!method.ReturnType.IsVoid && !callingMethodReturnsByRef)
			{
				if (sharedMethodReturnsByRef)
				{
					directWriter = writer;
					directWriter.WriteLine($"{method.GetResolvedReturnType(context).CppNameForVariable} {"il2cppRetVal"};");
					writer.Write("\t");
				}
				else if (hasFullGenericSharingSignature && method.GetResolvedReturnType(context) != sharedResolvedReturnType)
				{
					directWriter = writer;
					directWriter.Write($"{sharedResolvedReturnType.CppNameForVariable} {"il2cppRetVal"} = ");
				}
				else
				{
					writer.Write("return ");
				}
			}
			string parameters = MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithName, includeHiddenMethodInfo: true);
			if (hasFullGenericSharingSignature)
			{
				parameters = FormatParametersForCallToFullySharedMethod(context, method, sharedMethod, parameters, sharedMethodReturnsByRef && !callingMethodReturnsByRef);
			}
			directWriter = writer;
			directWriter.WriteLine($"{$"(({MethodSignatureWriter.GetMethodPointer(context, hasFullGenericSharingSignature ? sharedMethod : method)}){sharedMethod.CppName + "_gshared" + inlineSuffix2})"}({parameters});");
			if (!callingMethodReturnsByRef)
			{
				if (sharedMethodReturnsByRef)
				{
					writer.WriteLine("\treturn il2cppRetVal;");
				}
				else if (hasFullGenericSharingSignature && method.GetResolvedReturnType(context) != sharedResolvedReturnType)
				{
					if (method.GetResolvedReturnType(context).IsUserDefinedStruct())
					{
						directWriter = writer;
						directWriter.WriteLine($"\treturn il2cpp_codegen_cast_struct<{method.GetResolvedReturnType(context).CppNameForVariable}, {sharedResolvedReturnType.CppNameForVariable}>(&{"il2cppRetVal"});");
					}
					else
					{
						directWriter = writer;
						directWriter.WriteLine($"\treturn ({method.GetResolvedReturnType(context).CppNameForVariable}){"il2cppRetVal"};");
					}
				}
			}
			writer.WriteLine("}");
		}
		else
		{
			if (shouldInline)
			{
				MethodSignatureWriter.WriteMethodSignatureRawInline(context, writer, method);
			}
			else
			{
				MethodSignatureWriter.WriteMethodSignatureRaw(context, writer, method);
			}
			writer.WriteLine(";");
		}
	}

	private static string FormatParametersForCallToFullySharedMethod(ReadOnlyContext context, MethodReference method, MethodReference sharedMethod, string parameters, bool addReturnAsByRefParameter)
	{
		List<string> parameterList = parameters.Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			TypeReference parameterType = GenericParameterResolver.ResolveParameterTypeIfNeeded(context.Global.Services.TypeFactory, method, method.Parameters[i]);
			TypeReference sharedParameterType = GenericParameterResolver.ResolveParameterTypeIfNeeded(context.Global.Services.TypeFactory, sharedMethod, sharedMethod.Parameters[i]);
			RuntimeStorageKind parameterRuntimeStorage = parameterType.GetRuntimeStorage(context);
			RuntimeStorageKind sharedParameterRuntimeStorage = sharedParameterType.GetRuntimeStorage(context);
			int listIndex = (method.HasThis ? (i + 1) : i);
			if (parameterRuntimeStorage == RuntimeStorageKind.ValueType && sharedParameterRuntimeStorage.IsVariableSized())
			{
				parameterList[listIndex] = "(" + sharedParameterType.CppNameForVariable + ")&" + parameterList[listIndex];
			}
			else if (parameterType != sharedParameterType)
			{
				if (parameterRuntimeStorage == RuntimeStorageKind.ValueType)
				{
					parameterList[listIndex] = $"il2cpp_codegen_cast_struct<{sharedParameterType.CppNameForVariable}, {parameterType.CppNameForVariable}>(&{parameterList[listIndex]})";
				}
				else
				{
					parameterList[listIndex] = "(" + sharedParameterType.CppNameForVariable + ")" + parameterList[listIndex];
				}
			}
		}
		if (method.HasThis)
		{
			TypeReference thisType = (sharedMethod.DeclaringType.IsValueType ? sharedMethod.DeclaringType.CreatePointerType(context) : sharedMethod.DeclaringType);
			parameterList[0] = "(" + thisType.CppNameForVariable + ")" + parameterList[0];
		}
		if (!method.ReturnType.IsVoid && addReturnAsByRefParameter)
		{
			TypeReference returnType = GenericParameterResolver.ResolveReturnTypeIfNeeded(context.Global.Services.TypeFactory, sharedMethod);
			string returnParam = "(" + returnType.CppNameForPointerToVariable + ")&il2cppRetVal";
			parameterList.Insert(parameterList.Count - 1, returnParam);
		}
		return parameterList.AggregateWithComma(context);
	}
}
