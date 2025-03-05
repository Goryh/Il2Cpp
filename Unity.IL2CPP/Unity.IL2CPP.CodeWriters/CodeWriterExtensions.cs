using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.CodeWriters;

public static class CodeWriterExtensions
{
	public static void WriteWriteBarrierIfNeeded(this ICodeWriter writer, IRuntimeMetadataAccess metadataAccess, ResolvedTypeInfo valueType, string addressExpression, string valueExpression)
	{
		if (valueType.GetRuntimeStorage(writer.Context).IsVariableSized())
		{
			writer.WriteLine($"Il2CppCodeGenWriteBarrierForClass({metadataAccess.TypeInfoFor(valueType)}, (void**){addressExpression}, (void*){valueExpression});");
		}
		else
		{
			writer.WriteWriteBarrierIfNeeded(valueType.ResolvedType, addressExpression, valueExpression);
		}
	}

	public static void WriteFieldSetter(this ICodeWriter writer, FieldReference fieldReference, string fieldExpression, string valueExpression)
	{
		writer.WriteFieldSetter(fieldReference.DeclaringType, GenericParameterResolver.ResolveFieldTypeIfNeeded(writer.Context.Global.Services.TypeFactory, fieldReference), fieldExpression, valueExpression);
	}

	public static void WriteFieldSetter(this ICodeWriter writer, TypeResolver typeResolver, FieldReference fieldReference, string fieldExpression, string valueExpression)
	{
		writer.WriteFieldSetter(fieldReference.DeclaringType, typeResolver.ResolveFieldType(fieldReference), fieldExpression, valueExpression);
	}

	public static void WriteFieldSetter(this ICodeWriter writer, ResolvedFieldInfo fieldReference, string fieldExpression, string valueExpression)
	{
		writer.WriteFieldSetter(fieldReference.DeclaringType.ResolvedType, fieldReference.FieldType.ResolvedType, fieldExpression, valueExpression);
	}

	private static void WriteFieldSetter(this ICodeWriter writer, TypeReference declaringType, TypeReference fieldType, string fieldExpression, string valueExpression)
	{
		writer.WriteLine($"{fieldExpression} = {valueExpression};");
		if (!declaringType.Resolve().IsByRefLike)
		{
			writer.WriteWriteBarrierIfNeeded(fieldType, Emit.AddressOf(fieldExpression), valueExpression);
		}
	}

	public static bool WriteWriteBarrierIfNeeded(this ICodeWriter writer, TypeReference valueType, string addressExpression, string valueExpression, bool alreadyHasBarrierOnObject = false)
	{
		if (!valueType.IsPointer)
		{
			if (valueType.GetRuntimeStorage(writer.Context).IsVariableSized())
			{
				throw new NotSupportedException("This WriteWriteBarrierIfNeeded overload does not support generating barriers for variable sized types - " + valueType.FullName);
			}
			if (!valueType.IsValueType)
			{
				if (alreadyHasBarrierOnObject)
				{
					writer.WriteLine("#if IL2CPP_ENABLE_STRICT_WRITE_BARRIERS");
				}
				writer.WriteLine($"Il2CppCodeGenWriteBarrier((void**){addressExpression}, (void*){valueExpression});");
				if (alreadyHasBarrierOnObject)
				{
					writer.WriteLine("#endif");
				}
				return true;
			}
			if (valueType.IsValueType && !valueType.IsPrimitive)
			{
				TypeDefinition resolveType = valueType.Resolve();
				if (resolveType.HasFields)
				{
					foreach (FieldDefinition field in resolveType.Fields)
					{
						if (!field.IsStatic)
						{
							TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(valueType);
							FieldReference resolvedField = typeResolver.Resolve(field);
							TypeReference fieldType = typeResolver.ResolveFieldType(resolvedField);
							alreadyHasBarrierOnObject = writer.WriteWriteBarrierIfNeeded(fieldType, $"&(({addressExpression})->{field.CppName})", "NULL", alreadyHasBarrierOnObject);
						}
					}
				}
			}
		}
		return alreadyHasBarrierOnObject;
	}

	public static T WriteIfNotEmpty<T>(this IGeneratedMethodCodeWriter writer, Action<IGeneratedMethodCodeWriter> writePrefixIfNotEmpty, Func<IGeneratedMethodCodeWriter, T> writeContent, Action<IGeneratedMethodCodeWriter> writePostfixIfNotEmpty)
	{
		using InMemoryGeneratedMethodCodeWriter prefixWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context);
		using InMemoryGeneratedMethodCodeWriter inMemoryWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context);
		prefixWriter.Indent(writer.IndentationLevel);
		writePrefixIfNotEmpty(prefixWriter);
		inMemoryWriter.Indent(prefixWriter.IndentationLevel);
		T result = writeContent(inMemoryWriter);
		inMemoryWriter.Dedent(prefixWriter.IndentationLevel);
		prefixWriter.Dedent(writer.IndentationLevel);
		inMemoryWriter.Flush();
		if (!inMemoryWriter.Empty)
		{
			prefixWriter.Flush();
			writer.Write(prefixWriter);
			writer.Write(inMemoryWriter);
			int indentationToChange = prefixWriter.IndentationLevel + inMemoryWriter.IndentationLevel;
			if (indentationToChange > 0)
			{
				writer.Indent(indentationToChange);
			}
			else if (indentationToChange < 0)
			{
				writer.Dedent(-indentationToChange);
			}
			writePostfixIfNotEmpty?.Invoke(writer);
		}
		return result;
	}

	public static void WriteIfNotEmpty(this IGeneratedMethodCodeWriter writer, Action<IGeneratedMethodCodeWriter> writePrefixIfNotEmpty, Action<IGeneratedMethodCodeWriter> writeContent, Action<IGeneratedMethodCodeWriter> writePostfixIfNotEmpty)
	{
		writer.WriteIfNotEmpty(writePrefixIfNotEmpty, delegate(IGeneratedMethodCodeWriter bodyWriter)
		{
			writeContent(bodyWriter);
			return (object)null;
		}, writePostfixIfNotEmpty);
	}

	public static void WriteMethodWithMetadataInitialization(this IGeneratedMethodCodeWriter writer, string methodSignature, Action<IGeneratedMethodCodeWriter, IRuntimeMetadataAccess> writeMethodBody, string uniqueIdentifier, MethodReference methodRef, WritingMethodFor writingMethodFor = WritingMethodFor.Marshalling)
	{
		string identifier = uniqueIdentifier + "_MetadataUsageId";
		MethodMetadataUsage metadataUsage = new MethodMetadataUsage();
		MethodUsage methodUsage = new MethodUsage();
		using (InMemoryGeneratedMethodCodeWriter prologueWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context))
		{
			using InMemoryGeneratedMethodCodeWriter methodBodyWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context);
			methodBodyWriter.Indent(writer.IndentationLevel + 1);
			prologueWriter.Indent(writer.IndentationLevel + 1);
			IRuntimeMetadataAccess runtimeMetaDataAccess = writer.GetDefaultRuntimeMetadataAccess(methodRef, metadataUsage, methodUsage, writingMethodFor);
			writeMethodBody(methodBodyWriter, runtimeMetaDataAccess);
			bool needsGenericMethodInitialization = writingMethodFor == WritingMethodFor.MethodBody && !writer.Context.Global.Parameters.DisableFullGenericSharing && runtimeMetaDataAccess.GetMethodRgctxDataUsage().HasFlag(GenericContextUsage.Method);
			if (metadataUsage.UsesMetadata || needsGenericMethodInitialization)
			{
				WriteMethodMetadataInitialization(writer.Context, prologueWriter, identifier, metadataUsage, needsGenericMethodInitialization);
			}
			foreach (string statement in metadataUsage.GetInitializationStatements())
			{
				prologueWriter.WriteLine(statement);
			}
			methodBodyWriter.Dedent(writer.IndentationLevel + 1);
			prologueWriter.Dedent(writer.IndentationLevel + 1);
			foreach (MethodReference method in methodUsage.GetMethods())
			{
				writer.AddIncludeForMethodDeclaration(method);
			}
			if (metadataUsage.UsesMetadata)
			{
				WriteMethodMetadataInitializationDeclarations(writer.Context, writer, identifier, metadataUsage.GetIl2CppTypes(), metadataUsage.GetTypeInfos(), metadataUsage.GetInflatedMethods(), metadataUsage.GetFieldInfos(), metadataUsage.GetFieldRvaInfos(), from s in metadataUsage.GetStringLiterals()
					select s.Literal);
			}
			using (new OptimizationWriter(writer, methodRef))
			{
				writer.WriteLine(methodSignature);
				using (new BlockWriter(writer))
				{
					writer.Write(prologueWriter);
					writer.Write(methodBodyWriter);
				}
			}
		}
		if (metadataUsage.UsesMetadata)
		{
			writer.AddMetadataUsage(identifier, metadataUsage);
		}
	}

	public static void AddCurrentCodeGenModuleForwardDeclaration(this ICppCodeWriter writer, ReadOnlyContext context)
	{
		string codeGenModuleName = context.Global.Services.ContextScope.ForCurrentCodeGenModuleVar();
		if (codeGenModuleName != null)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppCodeGenModule " + codeGenModuleName);
		}
	}

	private static void WriteMethodMetadataInitialization(ReadOnlyContext context, ICppCodeWriter writer, string identifier, MethodMetadataUsage metadataUsage, bool needsGenericMethodInitialization)
	{
		INamingService naming = context.Global.Services.Naming;
		List<string> runtimeMetadataNames = new List<string>(metadataUsage.UsageCount);
		runtimeMetadataNames.AddRange(from t in metadataUsage.GetTypeInfosNeedingInit()
			select naming.ForRuntimeTypeInfo(context, t));
		runtimeMetadataNames.AddRange(from t in metadataUsage.GetIl2CppTypesNeedingInit()
			select naming.ForRuntimeIl2CppType(context, t));
		runtimeMetadataNames.AddRange(from method in metadataUsage.GetInflatedMethodsNeedingInit()
			select naming.ForRuntimeMethodInfo(context, method));
		runtimeMetadataNames.AddRange(from t in metadataUsage.GetFieldInfosNeedingInit()
			select naming.ForRuntimeFieldInfo(context, t));
		runtimeMetadataNames.AddRange(from t in metadataUsage.GetFieldRvaInfosNeedingInit()
			select naming.ForRuntimeFieldRvaStructStorage(context, t));
		runtimeMetadataNames.AddRange(from s in metadataUsage.GetStringLiteralsNeedingInit()
			select naming.ForRuntimeUniqueStringLiteralIdentifier(context, s.Literal));
		if (runtimeMetadataNames.Count == 0)
		{
			if (needsGenericMethodInitialization)
			{
				writer.WriteStatement("il2cpp_rgctx_method_init(method)");
			}
			return;
		}
		string initializerVariableName;
		string initializerSetterExpression;
		ICppCodeWriter cppCodeWriter;
		if (needsGenericMethodInitialization)
		{
			initializerVariableName = "il2cpp_rgctx_is_initialized(method)";
			initializerSetterExpression = "il2cpp_rgctx_method_init(method)";
		}
		else if (context.Global.Parameters.EnableReload)
		{
			initializerVariableName = context.Global.Services.ContextScope.ForReloadMethodMetadataInitialized() + "[" + identifier + "]";
			initializerSetterExpression = Emit.Assign(initializerVariableName, "true");
		}
		else
		{
			initializerVariableName = "s_Il2CppMethodInitialized";
			cppCodeWriter = writer;
			cppCodeWriter.WriteStatement($"static bool {initializerVariableName}");
			initializerSetterExpression = Emit.Assign(initializerVariableName, "true");
		}
		cppCodeWriter = writer;
		cppCodeWriter.WriteLine($"if (!{initializerVariableName})");
		writer.BeginBlock();
		foreach (string runtimeMetadataName in runtimeMetadataNames.ToSortedCollection())
		{
			cppCodeWriter = writer;
			cppCodeWriter.WriteStatement($"il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&{runtimeMetadataName})");
		}
		writer.WriteStatement(initializerSetterExpression);
		writer.EndBlock();
	}

	public static void WriteMethodMetadataInitializationDeclarations(ReadOnlyContext context, ICppCodeWriter writer, string identifier, IEnumerable<IIl2CppRuntimeType> types, IEnumerable<IIl2CppRuntimeType> typeInfos, IEnumerable<MethodReference> methods, IEnumerable<Il2CppRuntimeFieldReference> fields, IEnumerable<Il2CppRuntimeFieldReference> fieldRvas, IEnumerable<string> stringLiterals)
	{
		foreach (IIl2CppRuntimeType type in types)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C const RuntimeType* " + writer.Context.Global.Services.Naming.ForRuntimeIl2CppType(context, type));
		}
		foreach (IIl2CppRuntimeType type2 in typeInfos)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C RuntimeClass* " + writer.Context.Global.Services.Naming.ForRuntimeTypeInfo(context, type2));
		}
		foreach (MethodReference inflatedMethod in methods)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C const RuntimeMethod* " + writer.Context.Global.Services.Naming.ForRuntimeMethodInfo(context, inflatedMethod));
		}
		foreach (Il2CppRuntimeFieldReference field in fields)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C RuntimeField* " + writer.Context.Global.Services.Naming.ForRuntimeFieldInfo(context, field));
		}
		foreach (Il2CppRuntimeFieldReference field2 in fieldRvas)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C const char* " + writer.Context.Global.Services.Naming.ForRuntimeFieldRvaStructStorage(context, field2));
		}
		foreach (string stringLiteral in stringLiterals)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C String_t* " + writer.Context.Global.Services.Naming.ForRuntimeUniqueStringLiteralIdentifier(context, stringLiteral));
		}
		if (context.Global.Parameters.EnableReload)
		{
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C const uint32_t " + identifier);
			writer.AddForwardDeclaration("IL2CPP_EXTERN_C bool " + context.Global.Services.ContextScope.ForReloadMethodMetadataInitialized() + "[];");
		}
	}

	public static IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(this IGeneratedMethodCodeWriter writer, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage, WritingMethodFor writingMethodFor)
	{
		return writer.Context.Global.Services.Factory.GetDefaultRuntimeMetadataAccess(writer.Context, method, methodMetadataUsage, methodUsage, writingMethodFor);
	}

	public static void WriteReturnStatement(this IGeneratedMethodCodeWriter writer, string returnExpression = null)
	{
		if (string.IsNullOrEmpty(returnExpression))
		{
			writer.WriteStatement("return");
			return;
		}
		writer.WriteStatement($"return {returnExpression}");
	}

	public static void WriteMethodCallWithReturnValueStatementIfNeeded(this IGeneratedMethodCodeWriter writer, MethodReference method, string returnValueStatement, string callExpression)
	{
		if (!method.ReturnType.IsVoid && !method.ReturnValueIsByRef(writer.Context))
		{
			writer.Write(returnValueStatement);
		}
		writer.WriteStatement(callExpression);
	}

	public static void WriteMethodCallStatement(this IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, params string[] args)
	{
		WriteMethodCallStatementInternal(writer, metadataAccess, thisVariableName, callingMethod, method, methodCallType, null, args);
	}

	public static void WriteMethodCallWithResultStatement(this IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, string returnVariable, params string[] args)
	{
		WriteMethodCallStatementInternal(writer, metadataAccess, thisVariableName, callingMethod, method, methodCallType, returnVariable, args);
	}

	private static void WriteMethodCallStatementInternal(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, string returnVariable, params string[] args)
	{
		List<string> arguments = new List<string>();
		if (method.HasThis)
		{
			arguments.Add(thisVariableName);
		}
		if (args.Length != 0)
		{
			arguments.AddRange(args);
		}
		IVTableBuilderService vtableBuilder = ((methodCallType == MethodCallType.Virtual) ? writer.Context.Global.Services.VTable : null);
		MethodBodyWriter.WriteMethodCallExpression(returnVariable, writer, callingMethod, method, writer.Context.Global.Services.TypeFactory.EmptyResolver(), methodCallType, metadataAccess.MethodMetadataFor(method), vtableBuilder, arguments, useArrayBoundsCheck: false);
	}

	public static void WriteClangWarningDisables(this IDirectWriter writer)
	{
		writer.WriteLine("#ifdef __clang__");
		writer.WriteLine("#pragma clang diagnostic push");
		writer.WriteLine("#pragma clang diagnostic ignored \"-Winvalid-offsetof\"");
		writer.WriteLine("#pragma clang diagnostic ignored \"-Wunused-variable\"");
		writer.WriteLine("#endif");
	}

	public static void WriteClangWarningEnables(this IDirectWriter writer)
	{
		writer.WriteLine("#ifdef __clang__");
		writer.WriteLine("#pragma clang diagnostic pop");
		writer.WriteLine("#endif");
	}

	public static void AddCodeGenMetadataIncludes(this ICppCodeWriter writer)
	{
		writer.AddInclude("il2cpp-config.h");
		writer.AddInclude("codegen/il2cpp-codegen-metadata.h");
	}

	public static TableInfo WriteArrayInitializer(this ICodeWriter writer, string type, string variableName, IEnumerable<string> values, bool externArray, bool nullTerminate = true)
	{
		values = (nullTerminate ? values.Concat(new string[1] { "NULL" }) : values);
		string[] valueArray = values.ToArray();
		TableInfo tableInfo = new TableInfo(valueArray.Length, type, variableName, externArray);
		if (externArray)
		{
			writer.WriteLine(tableInfo.GetDeclaration());
		}
		string count = ((valueArray.Length == 0) ? "1" : valueArray.Length.ToString());
		writer.WriteLine($"{type} {variableName}[{count}] = ");
		writer.WriteFieldInitializer(valueArray);
		return tableInfo;
	}

	public static TableInfo WriteArrayInitializer<T>(this ICodeWriter writer, string type, string variableName, ICollection<T> values, Func<T, string> map, bool externArray)
	{
		TableInfo tableInfo = new TableInfo(values.Count, type, variableName, externArray);
		if (externArray)
		{
			writer.WriteLine(tableInfo.GetDeclaration());
		}
		string count = ((values.Count == 0) ? "1" : values.Count.ToString());
		writer.WriteLine($"{type} {variableName}[{count}] = ");
		writer.WriteFieldInitializer(values, map);
		return tableInfo;
	}

	public static void WriteStructInitializer(this ICodeWriter writer, string type, string variableName, IEnumerable<string> values, bool externStruct)
	{
		string typeSpaceName = type + " " + variableName;
		ICodeWriter codeWriter;
		if (externStruct)
		{
			codeWriter = writer;
			codeWriter.WriteLine($"IL2CPP_EXTERN_C {typeSpaceName};");
		}
		codeWriter = writer;
		codeWriter.WriteLine($"{typeSpaceName} = ");
		writer.WriteFieldInitializer(values);
	}

	private static void WriteFieldInitializer(this ICodeWriter writer, IEnumerable<string> values)
	{
		writer.BeginBlock();
		foreach (string value in values)
		{
			writer.Write(value);
			writer.WriteLine(",");
		}
		writer.EndBlock(semicolon: true);
	}

	private static void WriteFieldInitializer<T>(this ICodeWriter writer, IEnumerable<T> values, Func<T, string> map)
	{
		writer.BeginBlock();
		foreach (T value in values)
		{
			writer.Write(map(value));
			writer.WriteLine(",");
		}
		writer.EndBlock(semicolon: true);
	}
}
