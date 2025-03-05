using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

internal sealed class ICollectionProjectedMethodBodyWriter
{
	public delegate IEnumerable<RuntimeGenericData> GetGenericSharingDataForMethodDelegate(PrimaryCollectionContext context, MethodDefinition method);

	private struct MapMethodData
	{
		public readonly string MethodName;

		public readonly TypeReference ReturnType;

		public readonly GetGenericSharingDataForMethodDelegate GetGenericSharingDataForMethod;

		public readonly WriteRuntimeImplementedMethodBodyDelegate WriteMethodBodyDelegate;

		public MapMethodData(string methodName, TypeReference returnType, GetGenericSharingDataForMethodDelegate getGenericSharingDataForMethod, WriteRuntimeImplementedMethodBodyDelegate writeMethodBodyDelegate)
		{
			MethodName = methodName;
			ReturnType = returnType;
			GetGenericSharingDataForMethod = getGenericSharingDataForMethod;
			WriteMethodBodyDelegate = writeMethodBodyDelegate;
		}
	}

	private class LaterPhaseCallbacks
	{
		private readonly TypeDefinition _iDictionaryType;

		private readonly TypeDefinition _iMapType;

		private readonly TypeDefinition _keyValuePairType;

		public LaterPhaseCallbacks(TypeDefinition iDictionaryType, TypeDefinition iMapType, TypeDefinition keyValuePairType)
		{
			_iDictionaryType = iDictionaryType;
			_iMapType = iMapType;
			_keyValuePairType = keyValuePairType;
		}

		public static IEnumerable<RuntimeGenericData> GetICollectionGenericSharingData(PrimaryCollectionContext context, MethodDefinition method)
		{
			GenericParameter genericParameter = method.DeclaringType.GenericParameters[0];
			return new RuntimeGenericTypeData[1]
			{
				new RuntimeGenericTypeData(RuntimeGenericContextInfo.Class, genericParameter)
			};
		}

		public static IEnumerable<RuntimeGenericData> GetICollectionContainsGenericSharingData(PrimaryCollectionContext context, MethodDefinition method)
		{
			GenericParameter genericParameter = method.DeclaringType.GenericParameters[0];
			TypeDefinition equalityComparer = context.Global.Services.TypeProvider.GetSystemType(SystemType.EqualityComparer);
			GenericInstanceType equalityComparerInstance = context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(equalityComparer, genericParameter);
			TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(equalityComparerInstance);
			MethodReference defaultMethod = typeResolver.Resolve(equalityComparer.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0));
			MethodReference equalsMethod = typeResolver.Resolve(equalityComparer.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2 && m.HasThis));
			context.Global.Collectors.GenericMethods.Add(context, defaultMethod);
			context.Global.Collectors.GenericMethods.Add(context, equalsMethod);
			return new RuntimeGenericData[3]
			{
				new RuntimeGenericTypeData(RuntimeGenericContextInfo.Class, genericParameter),
				new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, defaultMethod),
				new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, equalsMethod)
			};
		}

		public void WriteGetIMapSizeMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToMapMethod(writer, method, metadataAccess, "get_Size", forwardKey: false, forwardValue: false);
		}

		public void WriteAddToIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToDictionaryMethod(writer, method, metadataAccess, "Add", forwardKey: true, forwardValue: true);
		}

		public void WriteClearIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToMapMethod(writer, method, metadataAccess, "Clear", forwardKey: false, forwardValue: false);
		}

		public void WriteRemoveFromIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToDictionaryMethod(writer, method, metadataAccess, "Remove", forwardKey: true, forwardValue: false);
		}

		public void WriteIMapContainsMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			if (!ThrowExceptionIfGenericParameterIsNotKeyValuePair(writer, method, out var isFullyShared))
			{
				SourceWritingContext context = writer.Context;
				IDataModelService factory = context.Global.Services.TypeFactory;
				TypeReference fullySharedType = context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference;
				TypeDefinition adapterTypeDef = method.DeclaringType.Resolve();
				GenericInstanceType keyValuePairInstance = (isFullyShared ? factory.CreateGenericInstanceType(_keyValuePairType, null, fullySharedType, fullySharedType) : ((GenericInstanceType)((GenericInstanceType)method.DeclaringType).GenericArguments[0]));
				TypeResolver keyValuePairResolver = context.Global.Services.TypeFactory.ResolverFor(keyValuePairInstance);
				TypeReference keyType = (isFullyShared ? fullySharedType : keyValuePairInstance.GenericArguments[0]);
				TypeReference valueType = (isFullyShared ? fullySharedType : keyValuePairInstance.GenericArguments[1]);
				MethodReference keyValuePairCtor = keyValuePairResolver.Resolve(_keyValuePairType.Methods.Single((MethodDefinition m) => m.IsConstructor));
				GenericInstanceType iDictionaryInstance = factory.CreateGenericInstanceTypeFromDefinition(_iDictionaryType, keyType, valueType);
				TypeResolver dictionaryTypeResolver = context.Global.Services.TypeFactory.ResolverFor(iDictionaryInstance);
				TypeDefinition equalityComparer = writer.Context.Global.Services.TypeProvider.GetSystemType(SystemType.EqualityComparer);
				GenericInstanceType equalityComparerInstance = factory.CreateGenericInstanceTypeFromDefinition(equalityComparer, isFullyShared ? fullySharedType : keyValuePairInstance);
				TypeResolver equalityComparerResolver = context.Global.Services.TypeFactory.ResolverFor(equalityComparerInstance);
				MethodReference equalityComparerDefaultGetter = equalityComparerResolver.Resolve(equalityComparer.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0));
				MethodReference equalsMethod = equalityComparerResolver.Resolve(equalityComparer.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2 && m.HasThis));
				MethodDefinition tryGetValueMethodDef = _iDictionaryType.Methods.Single((MethodDefinition m) => m.Name == "TryGetValue");
				MethodReference tryGetValueMethod = dictionaryTypeResolver.Resolve(tryGetValueMethodDef);
				List<string> tryGetValueArguments = new List<string>
				{
					context.Global.Services.VTable.IndexForWithComment(context, tryGetValueMethodDef),
					"inflatedInterface",
					"__this"
				};
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"RuntimeClass* keyValuePairClass = {metadataAccess.TypeInfoFor(adapterTypeDef.GenericParameters[0])};");
				if (isFullyShared)
				{
					EmitThrowExceptionIfSharedGenericInstanceIsNotKeyValuePair(writer, "keyValuePairClass", metadataAccess);
				}
				string dictionaryTypeInfo = metadataAccess.UnresolvedTypeInfoFor(_iDictionaryType);
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"RuntimeClass* inflatedInterface = InitializedTypeInfo(il2cpp_codegen_inflate_generic_class({dictionaryTypeInfo}, il2cpp_codegen_get_generic_class_inst(keyValuePairClass)));");
				string keyValue = DeclareAndExtractPropertyFromPair(writer, method, metadataAccess, keyValuePairResolver, keyType, "key", "get_Key", isFullyShared, "keyValuePairClass", 0);
				tryGetValueArguments.Add(keyValue);
				if (isFullyShared)
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"RuntimeClass* inflatedEqualityComparer = InitializedTypeInfo(il2cpp_codegen_inflate_generic_class({metadataAccess.UnresolvedTypeInfoFor(equalityComparer)}, il2cpp_codegen_type_from_class(keyValuePairClass)));");
					writer.WriteLine("RuntimeClass* valueClass = il2cpp_codegen_get_generic_argument(keyValuePairClass, 1);");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{valueType.CppNameForVariable} value = alloca(il2cpp_codegen_sizeof(valueClass));");
					tryGetValueArguments.Add("(Il2CppFullySharedGenericAny*)value");
				}
				else
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{valueType.CppNameForVariable} value;");
					tryGetValueArguments.Add("&value");
				}
				TypeReference returnType = tryGetValueMethod.ReturnType;
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{returnType.CppNameForVariable} {"tryGetValueReturnValue"};");
				if (method.ReturnValueIsByRef(context))
				{
					tryGetValueArguments.Add("&tryGetValueReturnValue");
				}
				else
				{
					writer.Write("tryGetValueReturnValue = ");
				}
				writer.WriteStatement(Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(tryGetValueMethod, dictionaryTypeResolver, isFullyShared), tryGetValueArguments));
				writer.WriteLine("if (!tryGetValueReturnValue)");
				using (new BlockWriter(writer))
				{
					writer.WriteReturnStatement("false");
				}
				writer.WriteLine();
				writer.WriteLine("bool result;");
				writer.AddIncludeForTypeDefinition(writer.Context, equalityComparerInstance);
				if (isFullyShared)
				{
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{fullySharedType.CppName} comparisonPair = alloca(il2cpp_codegen_sizeof(keyValuePairClass));");
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"const RuntimeMethod* kvpCtor = il2cpp_codegen_get_generic_instance_method_from_method_definition(keyValuePairClass, {metadataAccess.UnresolvedMethodInfo(keyValuePairCtor.Resolve())});");
					string[] kvpCtorArgs = new string[5]
					{
						"il2cpp_codegen_get_direct_method_pointer(kvpCtor)",
						"kvpCtor",
						"comparisonPair",
						keyValue,
						Emit.VariableSizedAnyForArgPassing("valueClass", "value")
					};
					writer.WriteStatement(Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(keyValuePairCtor, keyValuePairResolver, VirtualMethodCallType.InvokerCall), kvpCtorArgs));
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"const RuntimeMethod* equalityComparerGetDefault = il2cpp_codegen_get_generic_instance_method_from_method_definition(inflatedEqualityComparer, {metadataAccess.UnresolvedMethodInfo(equalityComparerDefaultGetter.Resolve())});");
					string[] equalityComparerGetDefaultArgs = new string[3] { "il2cpp_codegen_get_direct_method_pointer(equalityComparerGetDefault)", "equalityComparerGetDefault", "NULL" };
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.Write($"{equalityComparerInstance.CppNameForVariable} comparer = ");
					writer.WriteStatement(Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(equalityComparerDefaultGetter, equalityComparerResolver, VirtualMethodCallType.InvokerCall), equalityComparerGetDefaultArgs));
					IVTableBuilderService vtableBuilder = writer.Context.Global.Services.VTable;
					string[] equalityEqualsArgs = new string[4]
					{
						vtableBuilder.IndexFor(writer.Context, equalsMethod.Resolve()).ToString(),
						"comparer",
						method.Parameters[0].CppName,
						"comparisonPair"
					};
					writer.Write("result = ");
					writer.WriteStatement(Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(equalsMethod, equalityComparerResolver, doCallViaInvoker: true), equalityEqualsArgs));
				}
				else
				{
					writer.AddIncludeForMethodDeclaration(keyValuePairCtor);
					writer.AddIncludeForMethodDeclaration(equalityComparerDefaultGetter);
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{keyValuePairInstance.CppNameForVariable} comparisonPair;");
					writer.WriteMethodCallStatement(metadataAccess, "&comparisonPair", method, keyValuePairCtor, MethodCallType.Normal, "key", "value");
					writer.WriteLine();
					generatedMethodCodeWriter = writer;
					generatedMethodCodeWriter.WriteLine($"{equalityComparerInstance.CppNameForVariable} comparer;");
					writer.WriteMethodCallWithResultStatement(metadataAccess, "NULL", method, equalityComparerDefaultGetter, MethodCallType.Normal, "comparer");
					writer.WriteMethodCallWithResultStatement(metadataAccess, "comparer", method, equalsMethod, MethodCallType.Virtual, "result", method.Parameters[0].CppName, "comparisonPair");
				}
				writer.WriteReturnStatement("result");
			}
		}

		private void ForwardCallToMapMethod(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess, string mapMethodName, bool forwardKey, bool forwardValue)
		{
			ForwardKeyValuePairCallToMethod(writer, method, metadataAccess, _iMapType, mapMethodName, forwardKey, forwardValue);
		}

		private void ForwardCallToDictionaryMethod(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess, string dictionaryMethodName, bool forwardKey, bool forwardValue)
		{
			ForwardKeyValuePairCallToMethod(writer, method, metadataAccess, _iDictionaryType, dictionaryMethodName, forwardKey, forwardValue);
		}

		private void ForwardKeyValuePairCallToMethod(IGeneratedMethodCodeWriter writer, MethodReference currentMethod, IRuntimeMetadataAccess metadataAccess, TypeDefinition methodDeclaringTypeDef, string methodName, bool forwardKey, bool forwardValue)
		{
			if (ThrowExceptionIfGenericParameterIsNotKeyValuePair(writer, currentMethod, out var isFullyShared))
			{
				return;
			}
			SourceWritingContext context = writer.Context;
			IDataModelService factory = context.Global.Services.TypeFactory;
			TypeReference fullySharedType = context.Global.Services.TypeProvider.Il2CppFullySharedGenericTypeReference;
			GenericInstanceType keyValuePairInstance = (isFullyShared ? factory.CreateGenericInstanceType(_keyValuePairType, null, fullySharedType, fullySharedType) : ((GenericInstanceType)((GenericInstanceType)currentMethod.DeclaringType).GenericArguments[0]));
			TypeResolver keyValuePairResolver = context.Global.Services.TypeFactory.ResolverFor(keyValuePairInstance);
			TypeReference keyType = (isFullyShared ? fullySharedType : keyValuePairInstance.GenericArguments[0]);
			TypeReference valueType = (isFullyShared ? fullySharedType : keyValuePairInstance.GenericArguments[1]);
			GenericInstanceType methodDeclaringTypeInstance = factory.CreateGenericInstanceTypeFromDefinition(methodDeclaringTypeDef, keyType, valueType);
			MethodDefinition methodDef = methodDeclaringTypeDef.Methods.Single((MethodDefinition m) => m.Name == methodName);
			TypeResolver methodDeclaringTypeResolver = context.Global.Services.TypeFactory.ResolverFor(methodDeclaringTypeInstance);
			MethodReference method = methodDeclaringTypeResolver.Resolve(methodDef);
			List<string> methodArguments = new List<string>
			{
				context.Global.Services.VTable.IndexForWithComment(context, methodDef),
				"inflatedInterface",
				"__this"
			};
			string typeInfo = metadataAccess.UnresolvedTypeInfoFor(methodDeclaringTypeDef);
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"RuntimeClass* keyValuePairClass = {metadataAccess.TypeInfoFor(currentMethod.DeclaringType.Resolve().GenericParameters[0])};");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"RuntimeClass* inflatedInterface = InitializedTypeInfo(il2cpp_codegen_inflate_generic_class({typeInfo}, il2cpp_codegen_get_generic_class_inst(keyValuePairClass)));");
			if (isFullyShared)
			{
				EmitThrowExceptionIfSharedGenericInstanceIsNotKeyValuePair(writer, "keyValuePairClass", metadataAccess);
			}
			if (forwardKey)
			{
				methodArguments.Add(DeclareAndExtractPropertyFromPair(writer, currentMethod, metadataAccess, keyValuePairResolver, keyType, "key", "get_Key", isFullyShared, "keyValuePairClass", 0));
			}
			if (forwardValue)
			{
				methodArguments.Add(DeclareAndExtractPropertyFromPair(writer, currentMethod, metadataAccess, keyValuePairResolver, valueType, "value", "get_Value", isFullyShared, "keyValuePairClass", 1));
			}
			TypeReference returnType = methodDeclaringTypeResolver.Resolve(method.ReturnType);
			if (returnType.MetadataType != MetadataType.Void)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{returnType.CppNameForVariable} {"forwardReturnValue"};");
				if (!method.ReturnValueIsByRef(context))
				{
					writer.Write("forwardReturnValue = ");
				}
				else
				{
					methodArguments.Add("&forwardReturnValue");
				}
			}
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(method, methodDeclaringTypeResolver, isFullyShared), methodArguments)};");
			if (returnType.MetadataType != MetadataType.Void)
			{
				writer.WriteReturnStatement("forwardReturnValue");
			}
		}

		private string DeclareAndExtractPropertyFromPair(IGeneratedMethodCodeWriter writer, MethodReference currentMethod, IRuntimeMetadataAccess metadataAccess, TypeResolver keyValuePairResolver, TypeReference propertyType, string variableName, string getterName, bool isFullyShared, string keyValueClass, int argNum)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{propertyType.CppNameForVariable} {variableName};");
			MethodReference getter = keyValuePairResolver.Resolve(_keyValuePairType.Methods.Single((MethodDefinition m) => m.Name == getterName));
			if (isFullyShared)
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"RuntimeClass* {variableName}Class = il2cpp_codegen_get_generic_argument({keyValueClass}, {argNum});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{variableName} = alloca(il2cpp_codegen_sizeof({variableName}Class));");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"const RuntimeMethod* {getterName} = il2cpp_codegen_get_generic_instance_method_from_method_definition({keyValueClass}, {metadataAccess.UnresolvedMethodInfo(getter.Resolve())});");
				string[] args = new string[4]
				{
					"il2cpp_codegen_get_direct_method_pointer(" + getterName + ")",
					getterName,
					currentMethod.Parameters[0].CppName,
					Emit.CastToPointer(writer.Context, propertyType, variableName)
				};
				writer.WriteStatement(Emit.Call(writer.Context, writer.VirtualCallInvokeMethod(getter, keyValuePairResolver, VirtualMethodCallType.InvokerCall), args));
				return Emit.VariableSizedAnyForArgPassing(variableName + "Class", variableName);
			}
			writer.AddIncludeForMethodDeclaration(getter);
			string addressOfPair = Emit.AddressOf(currentMethod.Parameters[0].CppName);
			writer.WriteMethodCallWithResultStatement(metadataAccess, addressOfPair, currentMethod, getter, MethodCallType.Normal, variableName);
			return variableName;
		}

		private bool ThrowExceptionIfGenericParameterIsNotKeyValuePair(IGeneratedMethodCodeWriter writer, MethodReference method, out bool isFullyShared)
		{
			TypeReference genericArgument = ((GenericInstanceType)method.DeclaringType).GenericArguments[0];
			isFullyShared = genericArgument.IsIl2CppFullySharedGenericType;
			if (_keyValuePairType == null || (_keyValuePairType != genericArgument.Resolve() && !isFullyShared))
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_invalid_cast_exception(\"\")"));
				return true;
			}
			return false;
		}

		private void EmitThrowExceptionIfSharedGenericInstanceIsNotKeyValuePair(IGeneratedMethodCodeWriter writer, string keyValuePairClass, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine($"if (il2cpp_codegen_get_generic_type_definition({keyValuePairClass}) != {metadataAccess.UnresolvedTypeInfoFor(_keyValuePairType)})");
			using (new BlockWriter(writer))
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_invalid_cast_exception(\"\")"));
			}
		}
	}

	private readonly TypeDefinition _iCollectionType;

	private readonly TypeDefinition _iMapType;

	private readonly TypeDefinition _iVectorType;

	private readonly TypeDefinition _keyValuePairType;

	private readonly LaterPhaseCallbacks _laterPhaseCallbacks;

	private readonly PrimaryCollectionContext _context;

	private readonly EditContext _typeEditContext;

	public ICollectionProjectedMethodBodyWriter(PrimaryCollectionContext context, EditContext typeEditContext, TypeDefinition iCollectionType, TypeDefinition iDictionaryType, TypeDefinition iMapType, TypeDefinition iVectorType)
	{
		_context = context;
		_typeEditContext = typeEditContext;
		_iCollectionType = iCollectionType;
		_iVectorType = iVectorType;
		_iMapType = iMapType;
		_keyValuePairType = context.Global.Services.TypeProvider.GetSystemType(SystemType.KeyValuePair);
		_laterPhaseCallbacks = new LaterPhaseCallbacks(iDictionaryType, iMapType, _keyValuePairType);
	}

	public void WriteAdd(MethodDefinition method)
	{
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		MethodDefinition iVectorGetSizeMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "Append");
		MapMethodData dictionaryMethodData = new MapMethodData("AddToIMap", _context.Global.Services.TypeProvider.SystemVoid, LaterPhaseCallbacks.GetICollectionGenericSharingData, _laterPhaseCallbacks.WriteAddToIMapMethodBody);
		DispatchToVectorOrMapMethod(ilProcessor, null, iVectorGetSizeMethod, dictionaryMethodData);
		if (method.ReturnType.MetadataType != MetadataType.Void)
		{
			MethodDefinition getCountMethod = _iCollectionType.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
			ilProcessor.Emit(OpCodes.Ldarg_0);
			ilProcessor.Emit(OpCodes.Callvirt, getCountMethod);
			ilProcessor.Emit(OpCodes.Ldc_I4_1);
			ilProcessor.Emit(OpCodes.Sub);
		}
		ilProcessor.Emit(OpCodes.Ret);
	}

	public void WriteClear(MethodDefinition method)
	{
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		MethodDefinition iVectorClearMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "Clear");
		MapMethodData dictionaryMethodData = new MapMethodData("ClearIMap", _context.Global.Services.TypeProvider.SystemVoid, LaterPhaseCallbacks.GetICollectionGenericSharingData, _laterPhaseCallbacks.WriteClearIMapMethodBody);
		DispatchToVectorOrMapMethod(ilProcessor, null, iVectorClearMethod, dictionaryMethodData);
		ilProcessor.Emit(OpCodes.Ret);
	}

	public void WriteContains(MethodDefinition method)
	{
		_typeEditContext.AddVariableToMethod(method, _context.Global.Services.TypeProvider.BoolTypeReference);
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		MapMethodData dictionaryMethodData = new MapMethodData("IMapContains", _context.Global.Services.TypeProvider.BoolTypeReference, LaterPhaseCallbacks.GetICollectionContainsGenericSharingData, _laterPhaseCallbacks.WriteIMapContainsMethodBody);
		DispatchToVectorOrMapMethod(ilProcessor, method.Body.Variables[0], EmitIVectorContains, dictionaryMethodData);
		ilProcessor.Emit(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Ret);
	}

	private void EmitIVectorContains(ILProcessor ilProcessor, TypeReference iVectorInstance, VariableDefinition resultVariable)
	{
		MethodDefinition indexOfMethodDef = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
		MethodReference indexOfMethod = _context.Global.Services.TypeFactory.ResolverFor(iVectorInstance).Resolve(indexOfMethodDef);
		VariableDefinition indexVariable = _typeEditContext.AddVariableToMethod(ilProcessor, _context.Global.Services.TypeProvider.UInt32TypeReference);
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldloca, indexVariable);
		ilProcessor.Emit(OpCodes.Callvirt, indexOfMethod);
		ilProcessor.Emit(OpCodes.Stloc, resultVariable);
	}

	public void WriteCopyTo(MethodDefinition method)
	{
		GenericParameter collectionElementType = (method.DeclaringType.HasGenericParameters ? method.DeclaringType.GenericParameters[0] : null);
		EmitCopyToLoop(_context, _typeEditContext, method.Body.GetILProcessor(), collectionElementType, delegate(ILProcessor ilProcessor)
		{
			ilProcessor.Emit(OpCodes.Ldarg_0);
		}, null);
	}

	public static void EmitCopyToLoop(ReadOnlyContext context, EditContext typeEditContext, ILProcessor ilProcessor, TypeReference collectionElementType, Action<ILProcessor> loadCollection, Action<ILProcessor> postProcessElement)
	{
		MethodDefinition argumentNullExceptionConstructor = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentNullException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		MethodDefinition argumentOutOfRangeExceptionConstructor = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentOutOfRangeException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		MethodDefinition argumentExceptionConstructor = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		MethodReference arraySetValueMethod = null;
		GetCopyToHelperMethods(context, collectionElementType, out var iEnumeratorType, out var getEnumeratorMethod, out var moveNextMethod, out var getCurrentMethod, out var iCollectionGetCountMethod, out var iDisposableDisposeMethod);
		ParameterDefinition arrayParameter = ilProcessor.Body.Method.Parameters[0];
		ArrayType arrayType = arrayParameter.ParameterType as ArrayType;
		if (arrayType == null)
		{
			TypeDefinition systemArray = arrayParameter.ParameterType.Resolve();
			if (systemArray != context.Global.Services.TypeProvider.SystemArray)
			{
				throw new InvalidProgramException("Unrecognized type of the first CopyTo method parameter: " + systemArray.FullName);
			}
			arraySetValueMethod = systemArray.Methods.Single((MethodDefinition m) => m.Name == "SetValue" && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.MetadataType == MetadataType.Int32);
		}
		VariableDefinition enumeratorVariable = typeEditContext.AddVariableToMethod(ilProcessor, iEnumeratorType);
		VariableDefinition collectionSizeVariable = typeEditContext.AddVariableToMethod(ilProcessor, context.Global.Services.TypeProvider.Int32TypeReference);
		Instruction checkEmptySize = ilProcessor.Create(OpCodes.Nop);
		Instruction checkIndexNotNegative = ilProcessor.Create(OpCodes.Nop);
		Instruction checkIndex = ilProcessor.Create(OpCodes.Nop);
		Instruction checkCollectionFits = ilProcessor.Create(OpCodes.Nop);
		Instruction copyLoop = ilProcessor.Create(OpCodes.Nop);
		Instruction loopStart = ilProcessor.Create(OpCodes.Nop);
		Instruction loopEnd = ilProcessor.Create(OpCodes.Nop);
		Instruction returnInstruction = ilProcessor.Create(OpCodes.Ret);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Brtrue, checkIndexNotNegative);
		ilProcessor.Emit(OpCodes.Ldstr, "array");
		ilProcessor.Emit(OpCodes.Newobj, argumentNullExceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
		ilProcessor.Append(checkIndexNotNegative);
		ilProcessor.Emit(OpCodes.Ldarg_2);
		ilProcessor.Emit(OpCodes.Ldc_I4_0);
		ilProcessor.Emit(OpCodes.Bge, checkEmptySize);
		ilProcessor.Emit(OpCodes.Ldstr, "index");
		ilProcessor.Emit(OpCodes.Newobj, argumentOutOfRangeExceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
		ilProcessor.Append(checkEmptySize);
		loadCollection(ilProcessor);
		ilProcessor.Emit(OpCodes.Callvirt, iCollectionGetCountMethod);
		ilProcessor.Emit(OpCodes.Stloc, collectionSizeVariable);
		ilProcessor.Emit(OpCodes.Ldloc, collectionSizeVariable);
		ilProcessor.Emit(OpCodes.Brtrue, checkIndex);
		ilProcessor.Emit(OpCodes.Ret);
		ilProcessor.Append(checkIndex);
		ilProcessor.Emit(OpCodes.Ldarg_2);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldlen);
		ilProcessor.Emit(OpCodes.Blt, checkCollectionFits);
		ilProcessor.Emit(OpCodes.Ldstr, "The specified index is out of bounds of the specified array.");
		ilProcessor.Emit(OpCodes.Newobj, argumentExceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
		ilProcessor.Append(checkCollectionFits);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldlen);
		ilProcessor.Emit(OpCodes.Ldloc, collectionSizeVariable);
		ilProcessor.Emit(OpCodes.Sub);
		ilProcessor.Emit(OpCodes.Ldarg_2);
		ilProcessor.Emit(OpCodes.Bge, copyLoop);
		ilProcessor.Emit(OpCodes.Ldstr, "The specified space is not sufficient to copy the elements from this Collection.");
		ilProcessor.Emit(OpCodes.Newobj, argumentExceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
		ilProcessor.Append(copyLoop);
		loadCollection(ilProcessor);
		ilProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
		ilProcessor.Emit(OpCodes.Stloc, enumeratorVariable);
		ilProcessor.Append(loopStart);
		ilProcessor.Emit(OpCodes.Ldloc, enumeratorVariable);
		ilProcessor.Emit(OpCodes.Callvirt, moveNextMethod);
		ilProcessor.Emit(OpCodes.Brfalse, loopEnd);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		if (arrayType == null)
		{
			ilProcessor.Emit(OpCodes.Ldloc, enumeratorVariable);
			ilProcessor.Emit(OpCodes.Callvirt, getCurrentMethod);
			postProcessElement?.Invoke(ilProcessor);
		}
		ilProcessor.Emit(OpCodes.Ldarg_2);
		ilProcessor.Emit(OpCodes.Dup);
		ilProcessor.Emit(OpCodes.Ldc_I4_1);
		ilProcessor.Emit(OpCodes.Add);
		ilProcessor.Emit(OpCodes.Starg, 2);
		if (arrayType != null)
		{
			ilProcessor.Emit(OpCodes.Ldloc, enumeratorVariable);
			ilProcessor.Emit(OpCodes.Callvirt, getCurrentMethod);
			postProcessElement?.Invoke(ilProcessor);
			ilProcessor.Emit(OpCodes.Stelem_Any, arrayType.ElementType);
		}
		else
		{
			ilProcessor.Emit(OpCodes.Call, arraySetValueMethod);
		}
		ilProcessor.Emit(OpCodes.Br, loopStart);
		ilProcessor.Append(loopEnd);
		if (iDisposableDisposeMethod != null)
		{
			ilProcessor.Emit(OpCodes.Leave, returnInstruction);
			Instruction finallyStart = ilProcessor.Create(OpCodes.Ldloc, enumeratorVariable);
			ilProcessor.Append(finallyStart);
			ilProcessor.Emit(OpCodes.Callvirt, iDisposableDisposeMethod);
			ilProcessor.Emit(OpCodes.Endfinally);
			typeEditContext.AddExceptionHandlerToMethod(ilProcessor, context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Finally, loopStart, finallyStart, null, finallyStart, returnInstruction);
		}
		ilProcessor.Append(returnInstruction);
	}

	private static void GetCopyToHelperMethods(ReadOnlyContext context, TypeReference collectionElementType, out TypeReference iEnumeratorType, out MethodReference getEnumeratorMethod, out MethodReference moveNextMethod, out MethodReference getCurrentMethod, out MethodReference iCollectionGetCountMethod, out MethodReference iDisposableDisposeMethod)
	{
		TypeDefinition iCollectionType = ((collectionElementType != null) ? context.Global.Services.TypeProvider.GetSystemType(SystemType.ICollection_1) : context.Global.Services.TypeProvider.GetSystemType(SystemType.ICollection));
		MethodDefinition iCollectionGetCountMethodDef = iCollectionType.Methods.Single((MethodDefinition m) => m.Name == "get_Count" && m.Parameters.Count == 0);
		TypeDefinition nonGenericIEnumeratorTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator);
		moveNextMethod = nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
		if (collectionElementType != null)
		{
			IDataModelService typeFactory = context.Global.Services.TypeFactory;
			TypeDefinition iEnumerableTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerable_1);
			GenericInstanceType iEnumerableInstance = typeFactory.CreateGenericInstanceType(iEnumerableTypeDef, null, collectionElementType);
			TypeDefinition iEnumeratorTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator_1);
			GenericInstanceType iEnumeratorInstance = (GenericInstanceType)(iEnumeratorType = typeFactory.CreateGenericInstanceType(iEnumeratorTypeDef, null, collectionElementType));
			MethodDefinition getEnumeratorMethodDef = iEnumerableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
			getEnumeratorMethod = typeFactory.ResolverFor(iEnumerableInstance).Resolve(getEnumeratorMethodDef);
			MethodDefinition getCurrentMethodDef = iEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
			getCurrentMethod = typeFactory.ResolverFor(iEnumeratorInstance).Resolve(getCurrentMethodDef);
			GenericInstanceType iCollectionInstance = typeFactory.CreateGenericInstanceType(iCollectionType, null, collectionElementType);
			iCollectionGetCountMethod = typeFactory.ResolverFor(iCollectionInstance).Resolve(iCollectionGetCountMethodDef);
			TypeDefinition iDisposable = context.Global.Services.TypeProvider.GetSystemType(SystemType.IDisposable);
			iDisposableDisposeMethod = iDisposable.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
		}
		else
		{
			TypeDefinition iEnumerableTypeDef2 = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerable);
			getEnumeratorMethod = iEnumerableTypeDef2.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
			iEnumeratorType = nonGenericIEnumeratorTypeDef;
			getCurrentMethod = nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
			iCollectionGetCountMethod = iCollectionGetCountMethodDef;
			iDisposableDisposeMethod = null;
		}
	}

	public void WriteGetCount(MethodDefinition method)
	{
		MethodDefinition invalidOperationExceptionCtor = _context.Global.Services.TypeProvider.GetSystemType(SystemType.InvalidOperationException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		_typeEditContext.AddVariableToMethod(method, _context.Global.Services.TypeProvider.Int32TypeReference);
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		Instruction loadString = ilProcessor.Create(OpCodes.Ldstr, "The backing collection is too large.");
		MethodDefinition iVectorGetSizeMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "get_Size");
		MapMethodData mapMethodData = new MapMethodData("GetIMapSize", _context.Global.Services.TypeProvider.Int32TypeReference, LaterPhaseCallbacks.GetICollectionGenericSharingData, _laterPhaseCallbacks.WriteGetIMapSizeMethodBody);
		DispatchToVectorOrMapMethod(ilProcessor, method.Body.Variables[0], iVectorGetSizeMethod, mapMethodData);
		ilProcessor.Emit(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Ldc_I4, int.MaxValue);
		ilProcessor.Emit(OpCodes.Bge_Un, loadString);
		ilProcessor.Emit(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Ret);
		ilProcessor.Append(loadString);
		ilProcessor.Emit(OpCodes.Newobj, invalidOperationExceptionCtor);
		ilProcessor.Emit(OpCodes.Throw);
	}

	public void WriteGetIsReadOnly(MethodDefinition method)
	{
		WriteReturnFalse(method);
	}

	public void WriteGetIsFixedSize(MethodDefinition method)
	{
		WriteReturnFalse(method);
	}

	public void WriteGetIsSynchronized(MethodDefinition method)
	{
		WriteReturnFalse(method);
	}

	private static void WriteReturnFalse(MethodDefinition method)
	{
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldc_I4_0);
		iLProcessor.Emit(OpCodes.Ret);
	}

	public void WriteGetSyncRoot(MethodDefinition method)
	{
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ret);
	}

	public void WriteRemove(MethodDefinition method)
	{
		VariableDefinition resultVariable = null;
		if (method.ReturnType.MetadataType != MetadataType.Void)
		{
			resultVariable = _typeEditContext.AddVariableToMethod(method, method.ReturnType);
		}
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		MapMethodData dictionaryMethodData = new MapMethodData("RemoveFromIMap", _context.Global.Services.TypeProvider.BoolTypeReference, LaterPhaseCallbacks.GetICollectionGenericSharingData, _laterPhaseCallbacks.WriteRemoveFromIMapMethodBody);
		DispatchToVectorOrMapMethod(ilProcessor, resultVariable, EmitIVectorRemove, dictionaryMethodData);
		if (resultVariable != null)
		{
			ilProcessor.Emit(OpCodes.Ldloc_0);
		}
		ilProcessor.Emit(OpCodes.Ret);
	}

	private void EmitIVectorRemove(ILProcessor ilProcessor, TypeReference iVectorInstance, VariableDefinition resultVariable)
	{
		TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(iVectorInstance);
		MethodDefinition indexOfMethodDef = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
		MethodReference indexOfMethod = typeResolver.Resolve(indexOfMethodDef);
		MethodDefinition removeAtMethodDef = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "RemoveAt");
		MethodReference removeAtMethod = typeResolver.Resolve(removeAtMethodDef);
		VariableDefinition indexVariable = _typeEditContext.AddVariableToMethod(ilProcessor, _context.Global.Services.TypeProvider.UInt32TypeReference);
		Instruction endLabel = ilProcessor.Create(OpCodes.Nop);
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldloca, indexVariable);
		ilProcessor.Emit(OpCodes.Callvirt, indexOfMethod);
		if (resultVariable != null)
		{
			ilProcessor.Emit(OpCodes.Dup);
			ilProcessor.Emit(OpCodes.Stloc, resultVariable);
		}
		ilProcessor.Emit(OpCodes.Brfalse, endLabel);
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Ldloc, indexVariable);
		ilProcessor.Emit(OpCodes.Callvirt, removeAtMethod);
		ilProcessor.Append(endLabel);
	}

	private void DispatchToVectorOrMapMethod(ILProcessor ilProcessor, VariableDefinition resultVariable, MethodDefinition vectorMethod, MapMethodData mapMethodData)
	{
		Action<ILProcessor, TypeReference, VariableDefinition> emitVectorCall = delegate(ILProcessor ilProcessorInner, TypeReference iVectorInstance, VariableDefinition resultVariableInner)
		{
			int count = ilProcessorInner.Body.Method.Parameters.Count;
			for (int i = 0; i < count + 1; i++)
			{
				ilProcessorInner.Emit(OpCodes.Ldarg, i);
			}
			ilProcessorInner.Emit(OpCodes.Callvirt, _context.Global.Services.TypeFactory.ResolverFor(iVectorInstance).Resolve(vectorMethod));
			if (vectorMethod.ReturnType.MetadataType != MetadataType.Void)
			{
				ilProcessor.Emit(OpCodes.Stloc, resultVariableInner);
			}
		};
		DispatchToVectorOrMapMethod(ilProcessor, resultVariable, emitVectorCall, mapMethodData);
	}

	private void DispatchToVectorOrMapMethod(ILProcessor ilProcessor, VariableDefinition resultVariable, Action<ILProcessor, TypeReference, VariableDefinition> emitVectorCall, MapMethodData mapMethodData)
	{
		IDataModelService factory = _context.Global.Services.TypeFactory;
		MethodDefinition method = ilProcessor.Body.Method;
		TypeDefinition adapterType = method.DeclaringType;
		TypeReference iVectorInstance = null;
		MethodReference mapMethodInstance = null;
		if (_iVectorType != null)
		{
			iVectorInstance = ((!_iVectorType.HasGenericParameters) ? ((TypeReference)_iVectorType) : ((TypeReference)factory.CreateGenericInstanceTypeFromDefinition(_iVectorType, adapterType.GenericParameters[0])));
		}
		if (_iMapType != null)
		{
			MethodDefinition mapMethod = _typeEditContext.BuildMethod(mapMethodData.MethodName, MethodAttributes.Private, mapMethodData.ReturnType).WithMethodImplAttributes(MethodImplAttributes.CodeTypeMask).WithParametersClonedFrom(method)
				.Complete(adapterType);
			_context.Global.Collectors.RuntimeImplementedMethodWriters.RegisterMethod(mapMethod, (PrimaryCollectionContext c) => mapMethodData.GetGenericSharingDataForMethod(c, mapMethod), mapMethodData.WriteMethodBodyDelegate);
			GenericInstanceType adapterInstance = factory.CreateGenericInstanceTypeFromDefinition(adapterType, adapterType.GenericParameters);
			mapMethodInstance = _context.Global.Services.TypeFactory.ResolverFor(adapterInstance).Resolve(mapMethod);
		}
		if (_iVectorType != null && _iMapType != null)
		{
			Instruction mapBranch = ilProcessor.Create(OpCodes.Nop);
			Instruction afterMapCallInstruction = ilProcessor.Create(OpCodes.Nop);
			ilProcessor.Emit(OpCodes.Ldarg_0);
			ilProcessor.Emit(OpCodes.Isinst, iVectorInstance);
			ilProcessor.Emit(OpCodes.Brfalse, mapBranch);
			emitVectorCall(ilProcessor, iVectorInstance, resultVariable);
			ilProcessor.Emit(OpCodes.Br, afterMapCallInstruction);
			ilProcessor.Append(mapBranch);
			for (int i = 0; i < method.Parameters.Count + 1; i++)
			{
				ilProcessor.Emit(OpCodes.Ldarg, i);
			}
			ilProcessor.Emit(OpCodes.Call, mapMethodInstance);
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				ilProcessor.Emit(OpCodes.Stloc, resultVariable);
			}
			ilProcessor.Append(afterMapCallInstruction);
		}
		else if (_iVectorType != null)
		{
			emitVectorCall(ilProcessor, iVectorInstance, resultVariable);
		}
		else
		{
			for (int j = 0; j < method.Parameters.Count + 1; j++)
			{
				ilProcessor.Emit(OpCodes.Ldarg, j);
			}
			ilProcessor.Emit(OpCodes.Call, mapMethodInstance);
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				ilProcessor.Emit(OpCodes.Stloc, resultVariable);
			}
		}
	}
}
