using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public static class MethodWriter
{
	private class NullableAdjustorThunkData
	{
		private const string ValueFieldName = "value";

		private const string HasValueFieldName = "hasValue";

		public readonly FieldReference ValueField;

		public readonly FieldReference HasValueField;

		public readonly GenericInstanceType NullableType;

		public readonly bool IsVariableSized;

		public NullableAdjustorThunkData(MethodWriteContext context)
		{
			NullableType = (GenericInstanceType)context.MethodReference.DeclaringType;
			TypeDefinition nullableTypeDefinition = NullableType.Resolve();
			ValueField = nullableTypeDefinition.Fields.Single((FieldDefinition f) => f.Name == "value");
			HasValueField = nullableTypeDefinition.Fields.Single((FieldDefinition f) => f.Name == "hasValue");
			IsVariableSized = context.MethodReference.DeclaringType.GetRuntimeStorage(context).IsVariableSized();
			if (IsVariableSized)
			{
				TypeDefinition declaringType = context.MethodDefinition.DeclaringType;
				TypeReference[] genericArguments = context.MethodDefinition.DeclaringType.GenericParameters.ToArray();
				NullableType = declaringType.CreateGenericInstanceType(context, genericArguments);
			}
		}
	}

	public static void WriteMethodDefinition(AssemblyWriteContext context, IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		if (!MethodNeedsWritten(context, method))
		{
			return;
		}
		MethodDefinition methodDefinition = method.Resolve();
		context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
		MethodWriteContext methodContext = new MethodWriteContext(context, method);
		if (methodDefinition.IsPInvokeImpl)
		{
			WriteExternMethodeDeclarationForInternalPInvokeImpl(writer, methodDefinition);
		}
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(method.FullName);
		}
		EmitMethodDefinitionIndexForSourceMapping(context, writer, methodDefinition);
		if (method.ShouldNotOptimize())
		{
			writer.WriteLine("IL2CPP_DISABLE_OPTIMIZATIONS");
		}
		string methodSignature;
		if (method.CanShare(context))
		{
			context.Global.Collectors.Stats.RecordSharableMethod(method);
			if (!method.IsSharedMethod(context))
			{
				return;
			}
			methodSignature = MethodSignatureWriter.GetSharedMethodSignature(methodContext, writer);
		}
		else
		{
			methodSignature = MethodSignatureWriter.GetMethodSignatureForDefinition(methodContext, writer);
		}
		context.Global.Collectors.Stats.RecordMethod(method);
		writer.AddIncludeForTypeDefinition(methodContext, methodContext.ResolvedReturnType);
		AddIncludesForParameterTypeDefinitions(methodContext, writer);
		if (methodDefinition.IsUnmanagedCallersOnly && !methodDefinition.UnmanagedCallersOnlyInfo.IsValid)
		{
			writer.WriteMethodWithMetadataInitialization(methodSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				UnmanagedCallersOnlyUtils.WriteCallToRaiseInvalidCallingConvsIfNeeded(bodyWriter, metadataAccess, method);
			}, method.CppName, method, WritingMethodFor.MethodBody);
			return;
		}
		writer.WriteMethodWithMetadataInitialization(methodSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WritePrologue(method, bodyWriter, metadataAccess);
			WriteMethodBody(methodContext, bodyWriter, metadataAccess);
		}, method.CppName, method, WritingMethodFor.MethodBody);
		if (method.ShouldNotOptimize())
		{
			writer.WriteLine("IL2CPP_ENABLE_OPTIMIZATIONS");
		}
		if (HasAdjustorThunk(method))
		{
			WriteAdjustorThunk(methodContext, writer);
		}
		if (methodDefinition.IsUnmanagedCallersOnly)
		{
			context.Global.Collectors.ReversePInvokeWrappers.AddReversePInvokeWrapper(method);
		}
	}

	private static void EmitMethodDefinitionIndexForSourceMapping(AssemblyWriteContext context, IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition)
	{
		if (writer.Context.Global.Parameters.EmitSourceMapping && context.Global.Results.PrimaryCollection.Metadata != null)
		{
			writer.WriteCommentedLine("Method Definition Index: " + context.Global.Results.PrimaryCollection.Metadata.GetMethodIndex(methodDefinition));
		}
	}

	private static void AddIncludesForParameterTypeDefinitions(MethodWriteContext context, IGeneratedMethodCodeWriter writer)
	{
		MethodReference method = context.MethodReference;
		TypeResolver typeResolver = context.TypeResolver;
		foreach (ParameterDefinition parameter in method.Parameters)
		{
			TypeReference resolvedParameterType = GenericParameterResolver.ResolveParameterTypeIfNeeded(context.Global.Services.TypeFactory, method, parameter);
			if (ShouldWriteIncludeForParameter(resolvedParameterType))
			{
				writer.AddIncludeForTypeDefinition(context, typeResolver.Resolve(resolvedParameterType));
			}
		}
	}

	private static void WriteInlineMethodDefinition(MethodWriteContext context, IGeneratedMethodCodeWriter writer, MethodReference method, string usage)
	{
		context.Global.Services.ErrorInformation.CurrentMethod = method.Resolve();
		string methodSignature;
		if (method.CanShare(context))
		{
			if (!method.IsSharedMethod(context))
			{
				return;
			}
			methodSignature = MethodSignatureWriter.GetSharedMethodSignatureInline(context, writer);
		}
		else
		{
			methodSignature = MethodSignatureWriter.GetInlineMethodSignature(context, writer);
		}
		writer.AddIncludeForTypeDefinition(context, context.ResolvedReturnType);
		AddIncludesForParameterTypeDefinitions(context, writer);
		EmitMethodDefinitionIndexForSourceMapping(context.Assembly, writer, method.Resolve());
		writer.WriteMethodWithMetadataInitialization(methodSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			WriteMethodBody(context, bodyWriter, metadataAccess);
		}, method.CppName + usage, method, WritingMethodFor.MethodBody);
	}

	public static void WriteInlineMethodDefinitions(SourceWritingContext context, string usage, IGeneratedMethodCodeWriter writer)
	{
		HashSet<MethodReference> methods = new HashSet<MethodReference>(writer.Declarations.Methods);
		HashSet<MethodReference> sharedMethods = new HashSet<MethodReference>(writer.Declarations.SharedMethods);
		HashSet<MethodReference> allMethods = new HashSet<MethodReference>(writer.Declarations.Methods);
		HashSet<MethodReference> allSharedMethods = new HashSet<MethodReference>(writer.Declarations.SharedMethods);
		string cleanUsage = writer.Context.Global.Services.Naming.Clean(writer.Context, usage);
		while (methods.Count > 0 || sharedMethods.Count > 0)
		{
			foreach (MethodReference inlineMethod in methods.Where((MethodReference m) => m.ShouldInline(context.Global.Parameters)))
			{
				if (!inlineMethod.CanShare(context))
				{
					WriteInlineMethodDefinition(context.CreateMethodWritingContext(inlineMethod), writer, inlineMethod, cleanUsage);
				}
			}
			foreach (MethodReference inlineMethod2 in sharedMethods.Where((MethodReference m) => m.ShouldInline(context.Global.Parameters)))
			{
				WriteInlineMethodDefinition(context.CreateMethodWritingContext(inlineMethod2), writer, inlineMethod2, cleanUsage);
			}
			methods = new HashSet<MethodReference>(writer.Declarations.Methods);
			methods.ExceptWith(allMethods);
			allMethods.UnionWith(methods);
			sharedMethods = new HashSet<MethodReference>(writer.Declarations.SharedMethods);
			sharedMethods.ExceptWith(allSharedMethods);
			allSharedMethods.UnionWith(sharedMethods);
		}
	}

	internal static bool HasAdjustorThunk(MethodReference method)
	{
		if (method.HasThis && method.DeclaringType.IsValueType)
		{
			return !method.DeclaringType.IsByRefLike;
		}
		return false;
	}

	internal static void CollectSequencePoints(PrimaryCollectionContext context, MethodDefinition method, SequencePointCollector sequencePointCollector)
	{
		if (!method.HasBody)
		{
			return;
		}
		try
		{
			context.Global.Services.ErrorInformation.CurrentMethod = method;
			if (!method.DebugInformation.HasSequencePoints)
			{
				sequencePointCollector.AddPausePoint(method, -1);
				{
					foreach (Instruction instruction in method.Body.Instructions)
					{
						if (instruction.Operand is Instruction)
						{
							Instruction targetIns = instruction.Operand as Instruction;
							if (targetIns.Offset < instruction.Offset)
							{
								sequencePointCollector.AddPausePoint(method, targetIns.Offset);
							}
						}
					}
					return;
				}
			}
			sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.Normal, string.Empty, null, 0, 0, 0, 0, -1));
			sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.Normal, string.Empty, null, 0, 0, 0, 0, 16777215));
			foreach (Instruction instruction2 in method.Body.Instructions)
			{
				SequencePoint currentSequencePoint = instruction2.SequencePoint;
				if (currentSequencePoint != null)
				{
					sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, currentSequencePoint));
				}
				if (instruction2.IsCallInstruction())
				{
					Instruction iterInst = instruction2.Previous;
					while (currentSequencePoint == null && iterInst != null)
					{
						currentSequencePoint = iterInst.SequencePoint;
						iterInst = iterInst.Previous;
					}
					if (currentSequencePoint != null)
					{
						sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.StepOut, currentSequencePoint.Document.Url, currentSequencePoint.Document.Hash, currentSequencePoint.StartLine, currentSequencePoint.EndLine, currentSequencePoint.StartColumn, currentSequencePoint.EndColumn, instruction2.Offset));
					}
					else
					{
						sequencePointCollector.AddSequencePoint(SequencePointInfo.Empty(method, SequencePointKind.StepOut, instruction2.Offset));
					}
				}
			}
			sequencePointCollector.AddVariables(context, method);
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException("Error while processing debug information. This often indicates that debug information in a .pdb file is not correct.\nCheck the debug information corresponding to the assembly '" + (context.Global.Services.ErrorInformation.CurrentMethod.DeclaringType.Module.FileName ?? context.Global.Services.ErrorInformation.CurrentMethod.DeclaringType.Module.Name) + "'.", innerException);
		}
	}

	internal static bool MethodNeedsWritten(ReadOnlyContext context, MethodReference method)
	{
		if (IsGetOrSetGenericValueOnArray(method))
		{
			return false;
		}
		if (GenericsUtilities.IsGenericInstanceOfCompareExchange(method))
		{
			return false;
		}
		if (GenericsUtilities.IsGenericInstanceOfExchange(method))
		{
			return false;
		}
		if (method.IsStripped)
		{
			return false;
		}
		if (method.DeclaringType.IsComOrWindowsRuntimeInterface(context) && ComInterfaceWriter.IsVTableGapMethod(method))
		{
			return false;
		}
		return MethodCanBeDirectlyCalled(context, method);
	}

	private static void WriteAdjustorThunk(MethodWriteContext context, IGeneratedMethodCodeWriter writer)
	{
		MethodReference method = context.MethodReference;
		string signature = WriteAdjustorThunkMethodSignature(context, method, context.TypeResolver);
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			string cppNameForVariable = method.DeclaringType.CppNameForVariable;
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = bodyWriter;
			generatedMethodCodeWriter.WriteLine($"{cppNameForVariable}* {"_thisAdjusted"};");
			NullableAdjustorThunkData nullableAdjustorThunkData = null;
			if (method.DeclaringType.IsNullableGenericInstance)
			{
				nullableAdjustorThunkData = new NullableAdjustorThunkData(context);
			}
			if (nullableAdjustorThunkData != null)
			{
				WriteNullableAdjustorThunkStart(context, bodyWriter, metadataAccess, nullableAdjustorThunkData, cppNameForVariable, "_thisAdjusted");
			}
			else
			{
				WriteCalcAdjustorThunkOffset(context, bodyWriter, cppNameForVariable, "_thisAdjusted");
			}
			List<string> list = new List<string>(method.Parameters.Count + 1) { "_thisAdjusted" };
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				list.Add(method.Parameters[i].CppName);
			}
			string text = "";
			if (method.ReturnType.IsNotVoid)
			{
				if (method.ReturnValueIsByRef(context))
				{
					list.Add("il2cppRetVal");
				}
				else
				{
					text = "_returnValue";
					generatedMethodCodeWriter = bodyWriter;
					generatedMethodCodeWriter.WriteLine($"{context.TypeResolver.Resolve(method.ReturnType).CppNameForVariable} {text};");
				}
			}
			MethodBodyWriter.WriteMethodCallExpression(text, bodyWriter, method, method, context.Global.Services.TypeFactory.EmptyResolver(), MethodCallType.Normal, metadataAccess.MethodMetadataFor(method).ForAdjustorThunk().OverrideHiddenMethodInfo("method"), null, list, useArrayBoundsCheck: true);
			if (nullableAdjustorThunkData != null)
			{
				WriteNullableAdjustorThunkEnd(context, bodyWriter, metadataAccess, nullableAdjustorThunkData, method, "_thisAdjusted");
			}
			if (method.ReturnType.IsNotVoid)
			{
				bodyWriter.WriteReturnStatement(text);
			}
		}, method.NameForAdjustorThunk(), method);
	}

	private static void WriteNullableAdjustorThunkStart(MethodWriteContext context, IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess runtimeMetadataAccess, NullableAdjustorThunkData thunkData, string thisType, string thisArgument)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter;
		if (!thunkData.IsVariableSized)
		{
			generatedMethodCodeWriter = bodyWriter;
			generatedMethodCodeWriter.WriteLine($"{thisType} {"_nullable"};");
		}
		if (!thunkData.IsVariableSized)
		{
			bodyWriter.WriteFieldSetter(context.TypeResolver, thunkData.ValueField, "_nullable." + thunkData.ValueField.CppName, $"*reinterpret_cast<{thunkData.NullableType.GenericArguments[0].CppNameForVariable}*>({"__this"} + 1)");
			bodyWriter.WriteFieldSetter(context.TypeResolver, thunkData.HasValueField, "_nullable." + thunkData.HasValueField.CppName, "true");
			generatedMethodCodeWriter = bodyWriter;
			generatedMethodCodeWriter.WriteLine($"{thisArgument} = &{"_nullable"};");
			return;
		}
		generatedMethodCodeWriter = bodyWriter;
		generatedMethodCodeWriter.WriteLine($"uint32_t _sizeOfThis = il2cpp_codegen_sizeof({runtimeMetadataAccess.TypeInfoFor(thunkData.NullableType)});");
		generatedMethodCodeWriter = bodyWriter;
		generatedMethodCodeWriter.WriteLine($"uint32_t _sizeOfValue = il2cpp_codegen_sizeof({runtimeMetadataAccess.TypeInfoFor(thunkData.ValueField.FieldType)});");
		generatedMethodCodeWriter = bodyWriter;
		generatedMethodCodeWriter.WriteLine($"{thisArgument} = ({thisType}*)alloca(_sizeOfThis);");
		generatedMethodCodeWriter = bodyWriter;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_write_instance_field_data(_thisAdjusted, {runtimeMetadataAccess.FieldInfo(thunkData.ValueField, thunkData.NullableType)}, {"__this"}+1, _sizeOfValue);");
		generatedMethodCodeWriter = bodyWriter;
		generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_write_instance_field_data<bool>(_thisAdjusted, {runtimeMetadataAccess.FieldInfo(thunkData.HasValueField, thunkData.NullableType)}, true);");
	}

	private static void WriteNullableAdjustorThunkEnd(MethodWriteContext context, IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess, NullableAdjustorThunkData thunkData, MethodReference method, string thisArgument)
	{
		if (method.Name == ".ctor")
		{
			if (method.DeclaringType.GetRuntimeStorage(context).IsVariableSized())
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = bodyWriter;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_memcpy(\n                            {"__this"}+1,\n                            il2cpp_codegen_get_instance_field_data_pointer({thisArgument}, {metadataAccess.FieldInfo(thunkData.ValueField, thunkData.NullableType)}),\n                            _sizeOfValue);");
			}
			else
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = bodyWriter;
				generatedMethodCodeWriter.WriteLine($"*reinterpret_cast<{thunkData.NullableType.GenericArguments[0].CppNameForVariable}*>({"__this"} + 1) = _thisAdjusted->{thunkData.ValueField.CppName};");
			}
		}
	}

	private static void WriteCalcAdjustorThunkOffset(ReadOnlyContext context, IGeneratedMethodCodeWriter bodyWriter, string thisType, string thisArgument)
	{
		bodyWriter.WriteStatement("int32_t _offset = 1");
		bodyWriter.WriteLine($"{thisArgument} = reinterpret_cast<{thisType}*>({"__this"} + _offset);");
	}

	public static string WriteAdjustorThunkMethodSignature(ReadOnlyContext context, MethodReference method, TypeResolver typeResolver)
	{
		string parameters = MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndNameThisObject, includeHiddenMethodInfo: true);
		return MethodSignatureWriter.GetMethodSignature(method.NameForAdjustorThunk(), MethodSignatureWriter.FormatReturnType(context, typeResolver.ResolveReturnType(method)), parameters, "IL2CPP_EXTERN_C");
	}

	private static bool ShouldWriteIncludeForParameter(TypeReference resolvedParameterType)
	{
		resolvedParameterType = resolvedParameterType.WithoutModifiers();
		if (resolvedParameterType is ByReferenceType byRefType)
		{
			return ShouldWriteIncludeForParameter(byRefType.ElementType);
		}
		if (resolvedParameterType is PointerType pointerType)
		{
			return ShouldWriteIncludeForParameter(pointerType.ElementType);
		}
		if (!(resolvedParameterType is TypeSpecification) || resolvedParameterType is GenericInstanceType || resolvedParameterType is ArrayType)
		{
			return !resolvedParameterType.IsGenericParameter;
		}
		return false;
	}

	private static void WriteMethodBodyForComOrWindowsRuntimeMethod(SourceWritingContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition.IsConstructor)
		{
			if (methodDefinition.DeclaringType.IsImport && !methodDefinition.DeclaringType.IsWindowsRuntimeProjection)
			{
				WriteMethodBodyForComObjectConstructor(method, writer);
			}
			else
			{
				WriteMethodBodyForWindowsRuntimeObjectConstructor(context, method, writer, metadataAccess);
			}
		}
		else if (methodDefinition.IsFinalizerMethod)
		{
			WriteMethodBodyForComOrWindowsRuntimeFinalizer(methodDefinition, writer, metadataAccess);
		}
		else if (method.DeclaringType.Is(Il2CppCustomType.Il2CppComObject) && method.Name == "ToString")
		{
			WriteMethodBodyForIl2CppComObjectToString(context, methodDefinition, writer, metadataAccess);
		}
		else if (method.HasThis)
		{
			WriteMethodBodyForDirectComOrWindowsRuntimeCall(context, method, writer, metadataAccess);
		}
		else
		{
			new ComStaticMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
		}
	}

	private static void WriteMethodBodyForComObjectConstructor(MethodReference method, IGeneratedMethodCodeWriter writer)
	{
		writer.WriteLine($"il2cpp_codegen_com_create_instance({method.DeclaringType.CppName}::CLSID, &{"__this"}->{writer.Context.Global.Services.Naming.ForIl2CppComObjectIdentityField()});");
		writer.WriteLine("il2cpp_codegen_com_register_rcw(__this);");
	}

	private static void WriteMethodBodyForWindowsRuntimeObjectConstructor(MinimalContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (method.Resolve().HasGenericParameters)
		{
			throw new InvalidOperationException("Cannot construct generic Windows Runtime objects.");
		}
		if (IsUnconstructibleWindowsRuntimeClass(context, method.DeclaringType, out var errorMessage))
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_invalid_operation_exception(\"" + errorMessage + "\")"));
		}
		else
		{
			new WindowsRuntimeConstructorMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
		}
	}

	private static bool IsUnconstructibleWindowsRuntimeClass(ReadOnlyContext context, TypeReference type, out string errorMessage)
	{
		if (type.IsAttribute)
		{
			errorMessage = "Cannot construct type '" + type.FullName + "'. Windows Runtime attribute types are not constructable.";
			return true;
		}
		TypeReference projectedType = context.Global.Services.WindowsRuntime.ProjectToCLR(type);
		if (projectedType != type)
		{
			errorMessage = $"Cannot construct type '{type.FullName}'. It has no managed representation. Instead, use '{projectedType.FullName}'.";
			return true;
		}
		errorMessage = null;
		return false;
	}

	private static void WriteMethodBodyForComOrWindowsRuntimeFinalizer(MethodDefinition finalizer, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (finalizer.DeclaringType.Is(Il2CppCustomType.Il2CppComObject))
		{
			ReleaseIl2CppObjectIdentity(writer);
		}
		CallBaseTypeFinalizer(finalizer, writer, metadataAccess);
	}

	private static void ReleaseIl2CppObjectIdentity(IGeneratedMethodCodeWriter writer)
	{
		string fieldName = writer.Context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({"__this"}->{fieldName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("il2cpp_codegen_il2cpp_com_object_cleanup(__this);");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{"__this"}->{fieldName}->Release();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{"__this"}->{fieldName} = {"NULL"};");
		}
		writer.WriteLine();
	}

	private static void CallBaseTypeFinalizer(MethodDefinition finalizer, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReference baseTypeFinalizer = null;
		TypeReference baseType = finalizer.DeclaringType.BaseType;
		while (baseType != null)
		{
			foreach (LazilyInflatedMethod method in baseType.IterateLazilyInflatedMethods(writer.Context))
			{
				if (method.Definition.IsFinalizerMethod)
				{
					baseTypeFinalizer = method.InflatedMethod;
					goto end_IL_0063;
				}
			}
			baseType = baseType.GetBaseType(writer.Context);
			continue;
			end_IL_0063:
			break;
		}
		if (baseTypeFinalizer != null)
		{
			List<string> args = new List<string>(2) { "__this" };
			TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(finalizer.DeclaringType);
			MethodBodyWriter.WriteMethodCallExpression("", writer, finalizer, typeResolver.Resolve(baseTypeFinalizer), typeResolver, MethodCallType.Normal, metadataAccess.MethodMetadataFor(baseTypeFinalizer), writer.Context.Global.Services.VTable, args, useArrayBoundsCheck: false);
		}
	}

	private static void WriteMethodBodyForIl2CppComObjectToString(SourceWritingContext context, MethodDefinition methodDefinition, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		string iStringableVariableName = context.Global.Services.Naming.ForInteropInterfaceVariable(context.Global.Services.TypeProvider.IStringableType);
		string iStringableTypeName = context.Global.Services.TypeProvider.IStringableType.CppName;
		MethodDefinition iStringableToString = context.Global.Services.TypeProvider.IStringableType.Methods.Single((MethodDefinition m) => m.Name == "ToString");
		writer.AddIncludeForTypeDefinition(context, context.Global.Services.TypeProvider.IStringableType);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{iStringableTypeName}* {iStringableVariableName} = il2cpp_codegen_com_query_interface_no_throw<{iStringableTypeName}>({"__this"});");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({iStringableVariableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			new ComMethodWithPreOwnedInterfacePointerMethodBodyWriter(context, iStringableToString).WriteMethodBody(writer, metadataAccess);
		}
		MethodDefinition objectToString = context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.Name == "ToString");
		List<string> arguments = new List<string> { "__this" };
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{objectToString.ReturnType.CppNameForVariable} {"toStringRetVal"};");
		MethodBodyWriter.WriteMethodCallExpression("toStringRetVal", writer, methodDefinition, objectToString, context.Global.Services.TypeFactory.EmptyResolver(), MethodCallType.Normal, metadataAccess.MethodMetadataFor(objectToString), writer.Context.Global.Services.VTable, arguments, useArrayBoundsCheck: true);
		writer.WriteReturnStatement("toStringRetVal");
	}

	private static void WriteMethodBodyForDirectComOrWindowsRuntimeCall(MinimalContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (!methodDefinition.IsComOrWindowsRuntimeMethod(context))
		{
			throw new InvalidOperationException("WriteMethodBodyForDirectComOrWindowsRuntimeCall called for non-COM and non-Windows Runtime method");
		}
		MethodReference interfaceMethod = (methodDefinition.DeclaringType.IsInterface ? method : method.GetOverriddenInterfaceMethod(context, method.DeclaringType.GetInterfaces(context)));
		if (interfaceMethod == null)
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"The method '" + method.FullName + "' has no implementation.\")"));
		}
		else if (!interfaceMethod.DeclaringType.IsComOrWindowsRuntimeInterface(context))
		{
			WriteMethodBodyForProjectedInterfaceMethod(writer, method, interfaceMethod, metadataAccess);
		}
		else
		{
			new ComInstanceMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
		}
	}

	private static void WriteMethodBodyForProjectedInterfaceMethod(IGeneratedMethodCodeWriter writer, MethodReference method, MethodReference interfaceMethod, IRuntimeMetadataAccess metadataAccess)
	{
		MethodDefinition interfaceMethodDef = interfaceMethod.Resolve();
		TypeReference interfaceForAdapterType = GetInterfaceForAdapterType(writer.Context, method, interfaceMethod.DeclaringType);
		TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(interfaceForAdapterType);
		TypeDefinition adapterTypeDef = writer.Context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(interfaceForAdapterType.Resolve());
		TypeReference adapterType = typeResolver.Resolve(adapterTypeDef);
		MethodDefinition adapterMethodDef = adapterTypeDef.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == interfaceMethodDef));
		MethodReference adapterMethod = typeResolver.Resolve(adapterMethodDef);
		writer.AddForwardDeclaration(adapterType);
		writer.AddIncludeForMethodDeclaration(adapterMethod);
		List<string> args = new List<string>();
		foreach (ParameterDefinition parameter in method.Parameters)
		{
			args.Add(parameter.CppName);
		}
		TypeReference returnType = writer.Context.Global.Services.TypeFactory.ResolverFor(interfaceMethod.DeclaringType).Resolve(interfaceMethod.ReturnType);
		if (!returnType.IsVoid)
		{
			string returnVariable = "returnValue";
			writer.WriteStatement($"{returnType.CppNameForVariable} {returnVariable}");
			writer.WriteMethodCallWithResultStatement(metadataAccess, $"reinterpret_cast<{adapterType.CppNameForVariable}>({"__this"})", null, adapterMethod, MethodCallType.Normal, returnVariable, args.ToArray());
			writer.WriteReturnStatement(returnVariable);
		}
		else
		{
			writer.WriteMethodCallStatement(metadataAccess, $"reinterpret_cast<{adapterType.CppNameForVariable}>({"__this"})", null, adapterMethod, MethodCallType.Normal, args.ToArray());
		}
		metadataAccess.TypeInfoFor(adapterType);
	}

	private static TypeReference GetInterfaceForAdapterType(MinimalContext context, MethodReference method, TypeReference interfaceType)
	{
		List<TypeReference> directlyImplementedProjectedInterfaces = new List<TypeReference>();
		foreach (TypeReference iface in from i in method.DeclaringType.GetInterfaces(context)
			where i.IsComOrWindowsRuntimeInterface(context)
			select i)
		{
			TypeReference projectedToCLR = context.Global.Services.WindowsRuntime.ProjectToCLR(iface);
			if (iface != projectedToCLR)
			{
				directlyImplementedProjectedInterfaces.Add(projectedToCLR);
			}
		}
		foreach (TypeReference iface2 in directlyImplementedProjectedInterfaces)
		{
			if (interfaceType == iface2)
			{
				return iface2;
			}
		}
		TypeDefinition interfaceTypeDef = interfaceType.Resolve();
		if (interfaceTypeDef.Module == context.Global.Services.TypeProvider.Corlib.MainModule && interfaceTypeDef.Namespace == "System.Collections" && interfaceTypeDef.Name == "IEnumerable")
		{
			foreach (TypeReference implementedInterface in directlyImplementedProjectedInterfaces)
			{
				TypeReference iface3 = FindInterfaceThatImplementsAnotherInterface(context, implementedInterface, interfaceType, "IEnumerable`1");
				if (iface3 != null)
				{
					return iface3;
				}
			}
		}
		return interfaceType;
	}

	private static TypeReference FindInterfaceThatImplementsAnotherInterface(ReadOnlyContext context, TypeReference potentialInterface, TypeReference interfaceType, string interfaceName)
	{
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(potentialInterface);
		foreach (TypeReference implementedInterface in potentialInterface.Resolve().Interfaces.Select((InterfaceImplementation i) => typeResolver.Resolve(i.InterfaceType)))
		{
			if (potentialInterface.Name == interfaceName && implementedInterface == interfaceType)
			{
				return potentialInterface;
			}
			TypeReference implementingInterface = FindInterfaceThatImplementsAnotherInterface(context, implementedInterface, interfaceType, interfaceName);
			if (implementingInterface != null)
			{
				return implementingInterface;
			}
		}
		return null;
	}

	private static void WriteMethodBodyForInternalCall(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, IICallMappingService icallMapping)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (!methodDefinition.IsInternalCall)
		{
			throw new Exception();
		}
		if (IntrinsicRemap.ShouldRemap(writer.Context, methodDefinition, fullGenericSharing: false))
		{
			string[] argumentArray = MethodSignatureWriter.ParametersForICall(writer.Context, methodDefinition, ParameterFormat.WithName).ToArray();
			IntrinsicRemap.IntrinsicCall intrinsicCall = IntrinsicRemap.GetMappedCallFor(writer, methodDefinition, methodDefinition, metadataAccess, argumentArray);
			if (methodDefinition.ReturnType.MetadataType != MetadataType.Void)
			{
				writer.WriteReturnStatement(intrinsicCall.FunctionName + "(" + intrinsicCall.Arguments.AggregateWithComma(writer.Context) + ")");
				return;
			}
			writer.WriteLine($"{intrinsicCall.FunctionName}({intrinsicCall.Arguments.AggregateWithComma(writer.Context)});");
		}
		else
		{
			if (methodDefinition.HasGenericParameters)
			{
				throw new NotSupportedException("Internal calls cannot have generic parameters: " + methodDefinition.FullName);
			}
			string nameWithoutReturnType = method.FullName.Substring(method.FullName.IndexOf(" ") + 1);
			string icall = icallMapping.ResolveICallFunction(nameWithoutReturnType);
			if (icall != null)
			{
				EmitDirectICallInvocation(method, writer, icall, icallMapping.ResolveICallHeader(nameWithoutReturnType), methodDefinition);
			}
			else
			{
				EmitFunctionPointerICallInvocation(method, writer, methodDefinition, metadataAccess);
			}
		}
	}

	private static void EmitFunctionPointerICallInvocation(MethodReference method, IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition, IRuntimeMetadataAccess metadataAccess)
	{
		writer.WriteInternalCallResolutionStatement(methodDefinition, metadataAccess);
		_ = writer.Context;
		string resultAssignment = string.Empty;
		Action writeReturnExpression = delegate
		{
		};
		if (!methodDefinition.ReturnType.IsVoid)
		{
			string cppNameForVariable = method.ReturnType.CppNameForVariable;
			writeReturnExpression = delegate
			{
				writer.WriteReturnStatement("icallRetVal");
			};
			resultAssignment = cppNameForVariable + " icallRetVal = ";
		}
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{resultAssignment}_il2cpp_icall_func({MethodSignatureWriter.FormatParametersForICall(writer.Context, method, ParameterFormat.WithName)});");
		writeReturnExpression();
	}

	private static void EmitDirectICallInvocation(MethodReference method, IGeneratedMethodCodeWriter writer, string icall, string icallHeader, MethodDefinition methodDefinition)
	{
		if (icallHeader != null)
		{
			writer.AddInclude(icallHeader);
		}
		writer.WriteLine($"typedef {MethodSignatureWriter.GetICallMethodVariable(writer.Context, methodDefinition)};");
		writer.WriteLine("using namespace il2cpp::icalls;");
		string callExpression = $"(({method.CppName}_ftn){icall}) ({MethodSignatureWriter.FormatParametersForICall(writer.Context, method, ParameterFormat.WithName)})";
		if (method.ReturnType.IsVoid)
		{
			writer.WriteStatement(callExpression);
		}
		else
		{
			writer.WriteReturnStatement(callExpression);
		}
	}

	private static void WriteMethodBodyForPInvokeImpl(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
	{
		new PInvokeMethodBodyWriter(writer.Context, method).WriteMethodBody(writer, metadataAccess);
	}

	private static void WriteExternMethodeDeclarationForInternalPInvokeImpl(IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		new PInvokeMethodBodyWriter(writer.Context, method).WriteExternMethodDeclarationForInternalPInvoke(writer);
	}

	internal static void WriteMethodForDelegatePInvoke(SourceWritingContext context, IGeneratedMethodCodeWriter writer, MethodReference method, DelegatePInvokeMethodBodyWriter delegatePInvokeMethodBodyWriter)
	{
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
		string methodName = context.Global.Services.Naming.ForDelegatePInvokeWrapper(method.DeclaringType);
		bool includeHiddenMethodInfo = MethodSignatureWriter.NeedsHiddenMethodInfo(context, method, MethodCallType.Normal, forFullGenericSharing: false);
		string signature = MethodSignatureWriter.GetMethodSignature(methodName, MethodSignatureWriter.FormatReturnType(context, typeResolver.Resolve(method.ReturnType)), MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_EXTERN_C");
		writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			delegatePInvokeMethodBodyWriter.WriteMethodBody(bodyWriter, metadataAccess);
		}, methodName, method, WritingMethodFor.MethodBody);
		context.Global.Collectors.WrappersForDelegateFromManagedToNative.Add(context, method);
	}

	internal static bool MethodCanBeDirectlyCalled(ReadOnlyContext context, MethodReference method)
	{
		if (!TypeMethodsCanBeDirectlyCalled(context, method.DeclaringType))
		{
			return false;
		}
		if (method.HasGenericParameters)
		{
			return false;
		}
		MethodDefinition methodDefinition = method.Resolve();
		TypeDefinition typeDefinition = methodDefinition.DeclaringType;
		if (typeDefinition.IsWindowsRuntime && typeDefinition.IsInterface && !typeDefinition.IsPublic && context.Global.Services.WindowsRuntime.ProjectToCLR(typeDefinition) == typeDefinition)
		{
			return IsInternalInterfaceMethodCalledFromRuntime(method);
		}
		if (methodDefinition.IsAbstract)
		{
			return method.DeclaringType.IsComOrWindowsRuntimeInterface(context);
		}
		return true;
	}

	private static bool IsInternalInterfaceMethodCalledFromRuntime(MethodReference method)
	{
		TypeReference declaringType = method.DeclaringType;
		if (declaringType.Namespace == "Windows.Foundation" && declaringType.Name == "IUriRuntimeClass")
		{
			return method.Name == "get_RawUri";
		}
		return false;
	}

	internal static bool TypeMethodsCanBeDirectlyCalled(ReadOnlyContext context, TypeReference type)
	{
		if (type.HasGenericParameters)
		{
			return false;
		}
		TypeDefinition typeDefinition = type.Resolve();
		if (typeDefinition.IsInterface && !type.IsComOrWindowsRuntimeInterface(context) && !type.ContainsDefaultInterfaceMethod)
		{
			return false;
		}
		if (typeDefinition.IsWindowsRuntimeProjection)
		{
			return typeDefinition.IsExposedToWindowsRuntime();
		}
		return true;
	}

	internal static bool IsGetOrSetGenericValueOnArray(MethodReference method)
	{
		if (method.DeclaringType.IsSystemArray)
		{
			if (!(method.Name == "GetGenericValueImpl"))
			{
				return method.Name == "SetGenericValueImpl";
			}
			return true;
		}
		return false;
	}

	private static void WriteMethodBody(MethodWriteContext context, IGeneratedMethodCodeWriter methodBodyWriter, IRuntimeMetadataAccess metadataAccess)
	{
		MethodReference method = context.MethodReference;
		MethodDefinition methodDefinition = context.MethodDefinition;
		if (!ReplaceWithHardcodedAlternativeIfPresent(context, method, methodDefinition, methodBodyWriter, metadataAccess))
		{
			if (!methodDefinition.HasBody || !methodDefinition.Body.Instructions.Any())
			{
				WriteMethodBodyForMethodWithoutBody(methodBodyWriter, method, metadataAccess);
			}
			else
			{
				new MethodBodyWriter(context, methodBodyWriter, metadataAccess).Generate();
			}
		}
	}

	private static void WritePrologue(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		SourceWritingContext context = writer.Context;
		if (context.Global.Parameters.EnableStacktrace)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"StackTraceSentry _stackTraceSentry({metadataAccess.MethodInfo(method)});");
		}
		if (ShouldDeepProfileMethod(context, method))
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"ProfilerMethodSentry _profilerMethodSentry({metadataAccess.MethodInfo(method)});");
		}
	}

	private static bool ShouldDeepProfileMethod(SourceWritingContext context, MethodReference method)
	{
		if (!context.Global.Parameters.EnableDeepProfiler)
		{
			return false;
		}
		return !CompilerServicesSupport.HasIgnoredByDeepProfilerAttribute(method.Resolve());
	}

	private static void WriteMethodBodyForMethodWithoutBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
	{
		if (!MethodCanBeDirectlyCalled(writer.Context, method))
		{
			throw new InvalidOperationException("Trying to generate a body for method '" + method.FullName + "'");
		}
		MethodDefinition methodDefinition = method.Resolve();
		WriteRuntimeImplementedMethodBodyDelegate runtimeImplementedMethodBodyWriter;
		if (methodDefinition.IsRuntime && !methodDefinition.IsInternalCall && !methodDefinition.DeclaringType.IsInterface && method.DeclaringType.IsDelegate)
		{
			new DelegateMethodsWriter(writer).WriteMethodBodyForIsRuntimeMethod(method, metadataAccess);
		}
		else if (writer.Context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetWriter(methodDefinition, out runtimeImplementedMethodBodyWriter))
		{
			runtimeImplementedMethodBodyWriter(writer, method, metadataAccess);
		}
		else if (methodDefinition.IsComOrWindowsRuntimeMethod(writer.Context))
		{
			WriteMethodBodyForComOrWindowsRuntimeMethod(writer.Context, method, writer, metadataAccess);
		}
		else if (methodDefinition.IsInternalCall)
		{
			WriteMethodBodyForInternalCall(method, writer, metadataAccess, writer.Context.Global.Services.ICallMapping);
		}
		else if (methodDefinition.IsPInvokeImpl)
		{
			WriteMethodBodyForPInvokeImpl(writer, methodDefinition, metadataAccess);
		}
		else
		{
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"The method '" + method.FullName + "' has no implementation.\")"));
		}
	}

	private static bool ReplaceWithHardcodedAlternativeIfPresent(ReadOnlyContext context, MethodReference method, MethodDefinition unresolvedMethod, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
		if (method.DeclaringType.IsSystemArray && method.Name == "UnsafeMov" && method.Parameters.Count == 1)
		{
			ParameterDefinition parameter = method.Parameters.Single();
			TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
			TypeReference returnType = typeResolver.Resolve(method.ReturnType);
			TypeReference typeReference = typeResolver.ResolveParameterType(method, parameter);
			string parameterName = parameter.CppName;
			string parameterAccess = (typeReference.GetRuntimeStorage(context).IsVariableSized() ? Emit.VariableSizedAnyForArgLoad(metadataAccess, unresolvedMethod.ReturnType, parameterName) : parameterName);
			if (returnType.GetRuntimeStorage(context).IsVariableSized())
			{
				writer.WriteLine($"il2cpp_codegen_array_unsafe_mov({metadataAccess.TypeInfoFor(unresolvedMethod.ReturnType)}, {"il2cppRetVal"}, {metadataAccess.TypeInfoFor(unresolvedMethod.Parameters[0].ParameterType)}, {parameterAccess});");
			}
			else if (parameterAccess != parameterName)
			{
				writer.WriteReturnStatement($"static_cast<{returnType.CppNameForVariable}>(reinterpret_cast<intptr_t>({parameterAccess}))");
			}
			else
			{
				writer.WriteReturnStatement($"static_cast<{returnType.CppNameForVariable}>({parameterName})");
			}
			return true;
		}
		if (method.DeclaringType.Name == "CompilerMessageAttribute" && method.Name == ".ctor" && method.DeclaringType.Namespace == "Microsoft.FSharp.Core" && method.Parameters.Count == 2 && method.Parameters[0].ParameterType == context.Global.Services.TypeProvider.GetSystemType(SystemType.Object) && method.Parameters[1].ParameterType == context.Global.Services.TypeProvider.GetSystemType(SystemType.Object))
		{
			return true;
		}
		return false;
	}

	private static int HashStringWithFNV1A32(string text)
	{
		uint result = 2166136261u;
		foreach (char c in text)
		{
			result = 16777619 * (result ^ (byte)(c & 0xFF));
			result = 16777619 * (result ^ (byte)((int)c >> 8));
		}
		return (int)result;
	}

	private static long HashStringWithFNV1A64(string text)
	{
		ulong result = 14695981039346656037uL;
		foreach (char c in text)
		{
			result = 1099511628211L * (result ^ (byte)(c & 0xFF));
			result = 1099511628211L * (result ^ (byte)((int)c >> 8));
		}
		return (long)result;
	}
}
