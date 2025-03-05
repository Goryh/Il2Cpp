using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters;

internal sealed class WindowsRuntimeDelegateMarshalInfoWriter : MarshalableMarshalInfoWriter
{
	private readonly TypeResolver _typeResolver;

	private readonly MethodReference _invokeMethod;

	private readonly string _comCallableWrapperInterfaceName;

	private readonly string _nativeInvokerName;

	private readonly string _nativeInvokerSignature;

	private readonly MethodReference _fullGenericSharedInvokeMethod;

	private readonly string _fullGenericSharedInvokerName;

	private readonly string _fullGenericSharedInvokerSignature;

	private readonly MarshaledType[] _marshaledTypes;

	public override MarshaledType[] GetMarshaledTypes(ReadOnlyContext context)
	{
		return _marshaledTypes;
	}

	public WindowsRuntimeDelegateMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
		: base(type)
	{
		TypeDefinition typeDef = type.Resolve();
		if (!typeDef.IsDelegate)
		{
			throw new ArgumentException("WindowsRuntimeDelegateMarshalInfoWriter cannot marshal non-delegate type " + type.FullName + ".");
		}
		_typeResolver = context.Global.Services.TypeFactory.ResolverFor(type);
		_invokeMethod = _typeResolver.Resolve(typeDef.Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
		_comCallableWrapperInterfaceName = context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(type);
		_nativeInvokerName = context.Global.Services.Naming.ForWindowsRuntimeDelegateNativeInvokerMethod(_invokeMethod);
		_marshaledTypes = new MarshaledType[1]
		{
			new MarshaledType(_comCallableWrapperInterfaceName + "*", _comCallableWrapperInterfaceName + "*")
		};
		string returnTypeName = MethodSignatureWriter.FormatReturnType(context, _typeResolver.ResolveReturnType(_invokeMethod));
		string parameters = $"{context.Global.Services.TypeProvider.Il2CppComObjectTypeReference.CppNameForVariable} {"__this"}, {MethodSignatureWriter.FormatParameters(context, _invokeMethod, ParameterFormat.WithTypeAndNameNoThis, includeHiddenMethodInfo: true)}";
		_nativeInvokerSignature = MethodSignatureWriter.GetMethodSignature(_nativeInvokerName, returnTypeName, parameters, "IL2CPP_EXTERN_C", string.Empty);
		if (_invokeMethod.CanShare(context) && _invokeMethod.GetSharedMethod(context).HasFullGenericSharingSignature(context))
		{
			_fullGenericSharedInvokeMethod = _invokeMethod.GetSharedMethod(context);
			returnTypeName = MethodSignatureWriter.FormatReturnType(context, _typeResolver.ResolveReturnType(_invokeMethod));
			parameters = $"{context.Global.Services.TypeProvider.Il2CppComObjectTypeReference.CppNameForVariable} {"__this"}, {MethodSignatureWriter.FormatParameters(context, _fullGenericSharedInvokeMethod, ParameterFormat.WithTypeAndNameNoThis, includeHiddenMethodInfo: true)}";
			_fullGenericSharedInvokerName = _nativeInvokerName + "_gshared";
			_fullGenericSharedInvokerSignature = MethodSignatureWriter.GetMethodSignature(_fullGenericSharedInvokerName, returnTypeName, parameters, "IL2CPP_EXTERN_C", string.Empty);
		}
	}

	public override void WriteMarshaledTypeForwardDeclaration(IReadOnlyContextGeneratedCodeWriter writer)
	{
		writer.AddForwardDeclaration("struct " + _comCallableWrapperInterfaceName);
	}

	public override void WriteNativeStructDefinition(IReadOnlyContextGeneratedCodeWriter writer)
	{
		foreach (ParameterDefinition parameter in _invokeMethod.Parameters)
		{
			MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeResolver.Resolve(parameter.ParameterType), MarshalType.WindowsRuntime, null, useUnicodeCharSet: true).WriteMarshaledTypeForwardDeclaration(writer);
		}
		MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeResolver.Resolve(_invokeMethod.ReturnType), MarshalType.WindowsRuntime, null, useUnicodeCharSet: true).WriteMarshaledTypeForwardDeclaration(writer);
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("COM Callable Wrapper interface definition for " + _typeRef.FullName);
		}
		IReadOnlyContextGeneratedCodeWriter readOnlyContextGeneratedCodeWriter = writer;
		readOnlyContextGeneratedCodeWriter.WriteLine($"struct {_comCallableWrapperInterfaceName} : Il2CppIUnknown");
		using (new BlockWriter(writer, semicolon: true))
		{
			writer.WriteLine("static const Il2CppGuid IID;");
			string parameterList = MethodSignatureWriter.FormatComMethodParameterList(writer.Context, _invokeMethod, _invokeMethod, _typeResolver, MarshalType.WindowsRuntime, includeTypeNames: true, preserveSig: false);
			readOnlyContextGeneratedCodeWriter = writer;
			readOnlyContextGeneratedCodeWriter.WriteLine($"virtual il2cpp_hresult_t STDCALL Invoke({parameterList}) = 0;");
		}
		writer.WriteLine();
	}

	public override void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
	{
		writer.WriteStatement(_fullGenericSharedInvokerSignature ?? _nativeInvokerSignature);
	}

	public override void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
	{
		writer.AddIncludeForTypeDefinition(writer.Context, _typeRef);
		writer.WriteLine($"const Il2CppGuid {_comCallableWrapperInterfaceName}::IID = {writer.Context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(writer.Context, _typeRef).GetGuid(writer.Context).ToInitializer()};");
		writer.Context.Global.Collectors.InteropGuids.Add(writer.Context, _typeRef);
		WriteNativeInvoker(writer);
	}

	public override bool WillWriteMarshalFunctionDefinitions()
	{
		return true;
	}

	private void WriteNativeInvoker(IGeneratedMethodCodeWriter writer)
	{
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine("Native invoker for " + _typeRef.FullName);
		}
		writer.WriteMethodWithMetadataInitialization(_nativeInvokerSignature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			new WindowsRuntimeDelegateMethodBodyWriter(writer.Context, _invokeMethod).WriteMethodBody(bodyWriter, metadataAccess);
		}, _nativeInvokerName, _invokeMethod);
		if (_fullGenericSharedInvokeMethod == null)
		{
			return;
		}
		writer.WriteLine(_fullGenericSharedInvokerSignature);
		using (new BlockWriter(writer))
		{
			List<string> translatedArgs = new List<string>(_invokeMethod.Parameters.Count + 3);
			if (_invokeMethod.HasThis)
			{
				translatedArgs.Add("__this");
			}
			ReadOnlyCollection<ParameterDefinition> sharedParameters = _fullGenericSharedInvokeMethod.GetResolvedParameters(writer.Context);
			ReadOnlyCollection<ParameterDefinition> invokerParameters = _invokeMethod.GetResolvedParameters(writer.Context);
			for (int i = 0; i < _invokeMethod.Parameters.Count; i++)
			{
				ParameterDefinition sharedParam = sharedParameters[i];
				ParameterDefinition invokerParam = invokerParameters[i];
				if (!sharedParam.ParameterType.ContainsFullySharedGenericTypes)
				{
					translatedArgs.Add(sharedParam.CppName);
				}
				else if (invokerParam.ParameterType.IsValueType)
				{
					translatedArgs.Add(Emit.Dereference(Emit.CastToPointer(writer.Context, invokerParam.ParameterType, sharedParam.CppName)));
				}
				else
				{
					translatedArgs.Add(Emit.Cast(writer.Context, invokerParam.ParameterType, sharedParam.CppName));
				}
			}
			if (_invokeMethod.ReturnType.IsNotVoid)
			{
				IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteStatement($"{_invokeMethod.ReturnType.CppName} = invokerReturnValue");
			}
			if (_invokeMethod.ReturnValueIsByRef(writer.Context))
			{
				translatedArgs.Add("invokerReturnValue");
			}
			else if (_invokeMethod.ReturnType.IsNotVoid)
			{
				writer.Write("invokerReturnValue = ");
			}
			translatedArgs.Add("method");
			writer.WriteStatement(Emit.Call(writer.Context, _nativeInvokerName, translatedArgs));
			if (_fullGenericSharedInvokeMethod.ReturnValueIsByRef(writer.Context))
			{
				writer.WriteStatement("il2cpp_codegen_memcpy(il2cppRetVal, &invokerReturnValue, sizeof(invokerReturnValue))");
			}
			else if (_fullGenericSharedInvokeMethod.ReturnType.IsNotVoid)
			{
				writer.WriteStatement("return invokerReturnValue");
			}
		}
	}

	public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
	{
		writer.WriteLine($"{_comCallableWrapperInterfaceName}* {variableName} = {"NULL"};");
	}

	public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
	{
		return "NULL";
	}

	public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({sourceVariable.Load(writer.Context)} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			string targetName = writer.Context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_target").CppName;
			string prevName = writer.Context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.Single((FieldDefinition f) => f.Name == "delegates").CppName;
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"RuntimeObject* target = {sourceVariable.Load(writer.Context)}->{targetName};");
			writer.WriteLine();
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (target != {"NULL"} && {sourceVariable.Load(writer.Context)}->{prevName} == {"NULL"} && target->klass == {metadataAccess.TypeInfoFor(writer.Context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference)})");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {writer.Context.Global.Services.Naming.ForInteropHResultVariable()} = static_cast<{writer.Context.Global.Services.TypeProvider.Il2CppComObjectTypeReference.CppNameForVariable}>(target)->{writer.Context.Global.Services.Naming.ForIl2CppComObjectIdentityField()}->QueryInterface({_comCallableWrapperInterfaceName}::IID, reinterpret_cast<void**>(&{destinationVariable}));");
				writer.WriteStatement(Emit.Call(writer.Context, "il2cpp_codegen_com_raise_exception_if_failed", writer.Context.Global.Services.Naming.ForInteropHResultVariable(), "false"));
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{destinationVariable} = il2cpp_codegen_com_get_or_create_ccw<{_comCallableWrapperInterfaceName}>({sourceVariable.Load(writer.Context)});");
			}
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{destinationVariable} = {"NULL"};");
		}
	}

	public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			writer.WriteLine("Il2CppIManagedObjectHolder* imanagedObject = NULL;");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"il2cpp_hresult_t {writer.Context.Global.Services.Naming.ForInteropHResultVariable()} = ({variableName})->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&imanagedObject));");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"if (IL2CPP_HR_SUCCEEDED({writer.Context.Global.Services.Naming.ForInteropHResultVariable()}))");
			using (new BlockWriter(writer))
			{
				destinationVariable.WriteStore(writer, "static_cast<{0}>(imanagedObject->GetManagedObject())", _typeRef.CppNameForVariable);
				writer.WriteLine("imanagedObject->Release();");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				MethodReference ctor = _typeRef.GetMethods(writer.Context).Single((MethodReference c) => c.IsConstructor && c.Parameters.Count == 2);
				destinationVariable.WriteStore(writer, Emit.NewObj(writer.Context, _typeRef, metadataAccess));
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"RuntimeObject* rcw = il2cpp_codegen_com_get_or_create_rcw_for_sealed_class<{writer.Context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference.CppName}>({variableName}, {metadataAccess.TypeInfoFor(writer.Context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference)});");
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"{"intptr_t"} methodInfo = reinterpret_cast<{"intptr_t"}>({metadataAccess.MethodInfo(_invokeMethod)});");
				writer.WriteStatement(Emit.Call(writer.Context, metadataAccess.Method(ctor), new string[4]
				{
					destinationVariable.Load(writer.Context),
					"rcw",
					"methodInfo",
					"NULL"
				}));
				writer.AddMethodForwardDeclaration(_fullGenericSharedInvokerSignature ?? _nativeInvokerSignature);
				writer.WriteStatement(Emit.Call(writer.Context, "il2cpp_codegen_set_closed_delegate_invoke", new string[3]
				{
					destinationVariable.Load(writer.Context),
					"rcw",
					Emit.Cast("void*", _fullGenericSharedInvokerName ?? _nativeInvokerName)
				}));
				generatedMethodCodeWriter = writer;
				generatedMethodCodeWriter.WriteLine($"il2cpp_codegen_com_cache_queried_interface(static_cast<Il2CppComObject*>(rcw), {_comCallableWrapperInterfaceName}::IID, {variableName});");
				writer.AddIncludeForTypeDefinition(writer.Context, writer.Context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference);
			}
		}
		writer.WriteLine("else");
		using (new BlockWriter(writer))
		{
			destinationVariable.WriteStore(writer, "NULL");
		}
	}

	public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
	{
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"if ({variableName} != {"NULL"})");
		using (new BlockWriter(writer))
		{
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"({variableName})->Release();");
			generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{variableName} = {"NULL"};");
		}
	}
}
