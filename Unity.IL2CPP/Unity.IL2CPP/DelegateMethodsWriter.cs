using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP;

public class DelegateMethodsWriter
{
	private readonly struct DelegateInvokeStubSignatureBuilder
	{
		private readonly string _methodName;

		private readonly string _returnType;

		private readonly string _parameters;

		public DelegateInvokeStubSignatureBuilder(ReadOnlyContext context, MethodReference invokeMethod, bool includeHiddenMethodInfo)
		{
			_methodName = invokeMethod.CppName;
			_returnType = MethodSignatureWriter.FormatReturnType(context, invokeMethod.GetResolvedReturnType(context));
			_parameters = MethodSignatureWriter.FormatParameters(context, invokeMethod, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo);
		}

		public string Build(string suffix)
		{
			return $"{_returnType} {_methodName}_{suffix}({_parameters})";
		}
	}

	private readonly SourceWritingContext _context;

	private readonly IGeneratedMethodCodeWriter _writer;

	private readonly FieldReference _methodPtrField;

	private readonly string _methodPtrName;

	private readonly FieldReference _invokeImplField;

	private readonly string _invokeImplName;

	private readonly FieldReference _multicastInvokeImplField;

	private readonly string _multicastInvokeImplName;

	private readonly FieldReference _methodField;

	private readonly string _methodName;

	private readonly string _methodIsVirtualName;

	private readonly string _isDelegateOpenName;

	private readonly FieldReference _targetField;

	private readonly string _targetName;

	private readonly FieldReference _invokeImplThisField;

	private readonly string _invokeImplThisName;

	private readonly FieldReference _delegatesArrayField;

	private readonly string _delegatesArrayName;

	private readonly string _delegateCountName;

	private const string FunctionPointerType = "FunctionPointerType";

	private bool MulticastDelegatesStripped => _delegatesArrayName == null;

	public DelegateMethodsWriter(IGeneratedMethodCodeWriter writer)
	{
		_context = writer.Context;
		_writer = writer;
		_methodPtrField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method_ptr");
		_methodPtrName = _methodPtrField.CppName;
		_invokeImplField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "invoke_impl");
		_invokeImplName = _invokeImplField.CppName;
		_methodField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method");
		_methodName = _methodField.CppName;
		FieldDefinition methodIsVirtualField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method_is_virtual");
		_methodIsVirtualName = methodIsVirtualField.CppName;
		_multicastInvokeImplField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "extra_arg");
		_multicastInvokeImplName = _multicastInvokeImplField.CppName;
		_invokeImplThisField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method_code");
		_invokeImplThisName = _invokeImplThisField.CppName;
		_isDelegateOpenName = null;
		_targetField = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_target");
		_targetName = _targetField.CppName;
		_delegatesArrayField = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.SingleOrDefault((FieldDefinition f) => f.Name == "delegates");
		_delegatesArrayName = _delegatesArrayField?.CppName;
		_delegateCountName = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.SingleOrDefault((FieldDefinition f) => f.Name == "delegateCount")?.CppName;
	}

	public void WriteMethodBodyForIsRuntimeMethod(MethodReference method, IRuntimeMetadataAccess metadataAccess)
	{
		if (!method.DeclaringType.IsDelegate)
		{
			throw new NotSupportedException("Cannot WriteMethodBodyForIsRuntimeMethod for non multicast delegate type: " + method.DeclaringType.FullName);
		}
		switch (method.Name)
		{
		case "Invoke":
			WriteMethodBodyForInvoke(method);
			break;
		case "BeginInvoke":
			WriteMethodBodyForBeginInvoke(method, metadataAccess);
			break;
		case "EndInvoke":
			WriteMethodBodyForDelegateEndInvoke(method);
			break;
		case ".ctor":
			WriteMethodBodyForDelegateConstructor(method);
			break;
		default:
			_writer.WriteDefaultReturn(_context, method.GetResolvedReturnType(_context));
			break;
		}
	}

	public void WriteInvokeStubs(TypeReference typeReference)
	{
		MethodReference invokeMethod = typeReference.GetMethods(_context).Single((MethodReference m) => m.Name == "Invoke");
		TypeResolver typeResolver = _context.Global.Services.TypeFactory.ResolverFor(typeReference, invokeMethod);
		DelegateInvokeStubSignatureBuilder sigBuilder = new DelegateInvokeStubSignatureBuilder(_context, invokeMethod, IncludeHiddenMethodInfo());
		NullChecksSupport nullChecksSupport = new NullChecksSupport(_writer, invokeMethod.Resolve());
		if (!MulticastDelegatesStripped)
		{
			WriteInvokerStubForMulticastCalls(invokeMethod, sigBuilder.Build("Multicast"));
		}
		if (!CallDelegatesViaInvokers(invokeMethod))
		{
			WriteOpenStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenInst"), isInstance: true);
			WriteOpenStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenStatic"), isInstance: false);
		}
		if (CallDelegatesViaInvokers(invokeMethod))
		{
			WriteStaticOpenInvokerStub(invokeMethod, typeResolver, sigBuilder.Build("OpenStaticInvoker"));
			WriteStaticClosedInvokerStub(invokeMethod, typeResolver, sigBuilder.Build("ClosedStaticInvoker"));
		}
		if (CallDelegatesViaInvokers(invokeMethod))
		{
			WriteInstanceClosedInvokerStub(invokeMethod, typeResolver, sigBuilder.Build("ClosedInstInvoker"));
			if (ShouldEmitOpenInstanceInvocations(invokeMethod))
			{
				WriteInstanceOpenInvokerStub(invokeMethod, typeResolver, nullChecksSupport, sigBuilder.Build("OpenInstInvoker"));
			}
		}
		if (ShouldEmitOpenInstanceVirtualInvocations(invokeMethod))
		{
			if (CallDelegatesViaInvokers(invokeMethod))
			{
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenVirtualInvoker"), VirtualMethodCallType.Virtual, invokerCall: true);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenInterfaceInvoker"), VirtualMethodCallType.Interface, invokerCall: true);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenGenericVirtualInvoker"), VirtualMethodCallType.GenericVirtual, invokerCall: true);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenGenericInterfaceInvoker"), VirtualMethodCallType.GenericInterface, invokerCall: true);
			}
			else
			{
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenVirtual"), VirtualMethodCallType.Virtual, invokerCall: false);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenInterface"), VirtualMethodCallType.Interface, invokerCall: false);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenGenericVirtual"), VirtualMethodCallType.GenericVirtual, invokerCall: false);
				WriteInstanceOpenVirtualCallStub(invokeMethod, typeResolver, in nullChecksSupport, sigBuilder.Build("OpenGenericInterface"), VirtualMethodCallType.GenericInterface, invokerCall: false);
			}
		}
	}

	private void WriteInvokerStubForMulticastCalls(MethodReference invokeMethod, string methodSignature)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			string delegatesVariableName = "delegatesToInvoke";
			string currentDelegateVariableName = "currentDelegate";
			string lengthVariableName = "length";
			string expressionForGetDelegates = ExpressionForFieldOfThis(_delegatesArrayName);
			_writer.AddIncludeForTypeDefinition(_context, _delegatesArrayField.FieldType);
			string lengthExpression = ((_delegateCountName != null) ? ExpressionForFieldOfThis(_delegateCountName) : (expressionForGetDelegates + "->max_length"));
			IGeneratedMethodCodeWriter writer = _writer;
			writer.WriteLine($"{"il2cpp_array_size_t"} {lengthVariableName} = {lengthExpression};");
			writer = _writer;
			writer.WriteLine($"{"Delegate_t"}** {delegatesVariableName} = reinterpret_cast<{"Delegate_t"}**>({expressionForGetDelegates}->{ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck: false)}(0));");
			if (!invokeMethod.ReturnType.IsVoid && !invokeMethod.ReturnValueIsByRef(_context))
			{
				_writer.WriteVariable(_context, invokeMethod.GetResolvedReturnType(_context), "retVal");
			}
			using (new BlockWriter($"for ({"il2cpp_array_size_t"} i = 0; i < {lengthVariableName}; i++)", _writer))
			{
				writer = _writer;
				writer.WriteLine($"{invokeMethod.DeclaringType.CppNameForVariable} {currentDelegateVariableName} = reinterpret_cast<{invokeMethod.DeclaringType.CppNameForVariable}>({delegatesVariableName}[i]);");
				WriteDelegateInvokeCall(invokeMethod, currentDelegateVariableName, "retVal = ");
			}
			if (!invokeMethod.ReturnType.IsVoid && !invokeMethod.ReturnValueIsByRef(_context))
			{
				_writer.WriteStatement("return retVal");
			}
		}
	}

	private void WriteOpenStub(MethodReference invokeMethod, TypeResolver typeResolver, in NullChecksSupport nullChecksSupport, string methodSignature, bool isInstance)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			if (isInstance && ShouldEmitOpenInstanceInvocations(invokeMethod))
			{
				nullChecksSupport.WriteNullCheckIfNeeded(_context, typeResolver.ResolveParameterType(invokeMethod, invokeMethod.Parameters[0]), invokeMethod.Parameters[0].CppName);
			}
			string parametersForTypeDef = MethodSignatureWriter.FormatParameters(_context, invokeMethod, ParameterFormat.WithTypeNoThis, IncludeHiddenMethodInfo());
			string parametersForInvocation = MethodSignatureWriter.FormatParameters(_context, invokeMethod, ParameterFormat.WithNameNoThis, IncludeHiddenMethodInfo());
			WriteDirectCall(invokeMethod, ExpressionForFieldOfThis(_methodPtrName), parametersForTypeDef, parametersForInvocation);
		}
	}

	private void WriteStaticOpenInvokerStub(MethodReference invokeMethod, TypeResolver typeResolver, string methodSignature)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			List<TypeReference> parameterTypes = (from p in invokeMethod.GetResolvedParameters(_context)
				select p.ParameterType).ToList();
			List<string> parametersForInvocation = MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithNameNoThis).ToList();
			parametersForInvocation.Insert(0, "NULL");
			WriteIndirectCall(invokeMethod, typeResolver, parameterTypes, parametersForInvocation, VirtualMethodCallType.InvokerCall, invokerCall: true);
		}
	}

	private void WriteStaticClosedInvokerStub(MethodReference invokeMethod, TypeResolver typeResolver, string methodSignature)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			List<TypeReference> parameterTypes = (from p in invokeMethod.GetResolvedParameters(_context)
				select p.ParameterType).ToList();
			parameterTypes.Insert(0, _context.Global.Services.TypeProvider.ObjectTypeReference);
			List<string> parametersForInvocation = MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithNameNoThis).ToList();
			parametersForInvocation.Insert(0, "NULL");
			parametersForInvocation.Insert(1, ExpressionForFieldOfThis(_targetName));
			WriteIndirectCall(invokeMethod, typeResolver, parameterTypes, parametersForInvocation, VirtualMethodCallType.InvokerCall, invokerCall: true);
		}
	}

	private void WriteInstanceOpenInvokerStub(MethodReference invokeMethod, TypeResolver typeResolver, NullChecksSupport nullChecksSupport, string methodSignature)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			nullChecksSupport.WriteNullCheckIfNeeded(_context, typeResolver.ResolveParameterType(invokeMethod, invokeMethod.Parameters[0]), invokeMethod.Parameters[0].CppName);
			List<TypeReference> parameterTypes = (from p in invokeMethod.GetResolvedParameters(_context)
				select p.ParameterType).Skip(1).ToList();
			List<string> parametersForInvocation = MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithNameNoThis).ToList();
			WriteIndirectCall(invokeMethod, typeResolver, parameterTypes, parametersForInvocation, VirtualMethodCallType.InvokerCall, invokerCall: true);
		}
	}

	private void WriteInstanceClosedInvokerStub(MethodReference invokeMethod, TypeResolver typeResolver, string methodSignature)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			List<TypeReference> parameterTypes = (from p in invokeMethod.GetResolvedParameters(_context)
				select p.ParameterType).ToList();
			List<string> parametersForInvocation = new List<string>(invokeMethod.Parameters.Count + 1) { ExpressionForFieldOfThis(_targetName) };
			parametersForInvocation.AddRange(MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithNameNoThis));
			WriteIndirectCall(invokeMethod, typeResolver, parameterTypes, parametersForInvocation, VirtualMethodCallType.InvokerCall, invokerCall: true);
		}
	}

	private void WriteInstanceOpenVirtualCallStub(MethodReference invokeMethod, TypeResolver typeResolver, in NullChecksSupport nullChecksSupport, string methodSignature, VirtualMethodCallType callType, bool invokerCall)
	{
		using (new BlockWriter(methodSignature, _writer))
		{
			nullChecksSupport.WriteNullCheckIfNeeded(_context, typeResolver.ResolveParameterType(invokeMethod, invokeMethod.Parameters[0]), invokeMethod.Parameters[0].CppName);
			List<TypeReference> parameterTypes = (from p in invokeMethod.GetResolvedParameters(_context).Skip(1)
				select p.ParameterType).ToList();
			List<string> parametersForInvocation = MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithNameNoThis).ToList();
			WriteIndirectCall(invokeMethod, typeResolver, parameterTypes, parametersForInvocation, callType, invokerCall);
		}
	}

	private void WriteMethodBodyForDelegateConstructor(MethodReference method)
	{
		string cleanTargetName = method.Parameters[0].CppName;
		string cleanMethodName = method.Parameters[1].CppName;
		MethodReference invokeMethod = method.DeclaringType.GetMethods(_context).Single((MethodReference m) => m.Name == "Invoke");
		_writer.WriteFieldSetter(_methodPtrField, ExpressionForFieldOfThis(_methodPtrName), "(intptr_t)il2cpp_codegen_get_method_pointer((RuntimeMethod*)" + cleanMethodName + ")");
		_writer.WriteFieldSetter(_methodField, ExpressionForFieldOfThis(_methodName), cleanMethodName);
		_writer.WriteFieldSetter(_targetField, ExpressionForFieldOfThis(_targetName), cleanTargetName);
		WriteSelectMethodIl2Cpp(cleanMethodName, cleanTargetName, invokeMethod);
		if (!MulticastDelegatesStripped)
		{
			_writer.WriteFieldSetter(_multicastInvokeImplField, ExpressionForFieldOfThis(_multicastInvokeImplName), "(intptr_t)&" + invokeMethod.CppName + "_Multicast");
		}
	}

	private void WriteSelectMethodTiny(MethodReference invokeMethod)
	{
		using (new BlockWriter("if (" + ExpressionForFieldOfThis(_isDelegateOpenName) + ")", _writer))
		{
			WriteSetInvokeImplField(invokeMethod, "OpenStatic", forInvoker: false);
			_writer.WriteFieldSetter(_targetField, ExpressionForFieldOfThis(_targetName), "__this");
		}
		using (new BlockWriter("else", _writer))
		{
			WriteSetInvokeImplFieldForClosed(invokeMethod, forInvoker: false, needsBlock: false);
		}
	}

	private void WriteSelectMethodIl2Cpp(string cleanMethodName, string cleanTargetName, MethodReference invokeMethod)
	{
		bool canHaveOpenInstances = ShouldEmitOpenInstanceInvocations(invokeMethod);
		bool canBeVirtual = ShouldEmitOpenInstanceVirtualInvocations(invokeMethod);
		string methodInfoName = "(RuntimeMethod*)" + cleanMethodName;
		string isOpenName = "isOpen";
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"int parameterCount = il2cpp_codegen_method_parameter_count({methodInfoName});");
		_writer.WriteFieldSetter(_invokeImplThisField, ExpressionForFieldOfThis(_invokeImplThisName), "(intptr_t)__this");
		using (new BlockWriter("if (MethodIsStatic(" + methodInfoName + "))", _writer))
		{
			writer = _writer;
			writer.WriteLine($"bool {isOpenName} = parameterCount == {invokeMethod.Parameters.Count};");
			WriteStaticInvokeBlock(invokeMethod, isOpenName, CallDelegatesViaInvokers(invokeMethod));
		}
		using (new BlockWriter("else", _writer))
		{
			if (canHaveOpenInstances)
			{
				writer = _writer;
				writer.WriteLine($"bool {isOpenName} = parameterCount == {invokeMethod.Parameters.Count - 1};");
			}
			WriteInstanceInvokeBlock(invokeMethod, methodInfoName, isOpenName, cleanTargetName, canHaveOpenInstances, canBeVirtual, CallDelegatesViaInvokers(invokeMethod));
		}
	}

	private void WriteStaticInvokeBlock(MethodReference invokeMethod, string isOpenName, bool forInvoker)
	{
		using (new IndentWriter("if (" + isOpenName + ")", _writer))
		{
			WriteSetInvokeImplField(invokeMethod, "OpenStatic", forInvoker);
		}
		using (new IndentWriter("else", _writer))
		{
			WriteSetInvokeImplFieldForClosed(invokeMethod, forInvoker, needsBlock: true, "Static");
		}
	}

	private void WriteInstanceInvokeBlock(MethodReference invokeMethod, string methodInfoName, string isOpenName, string cleanTargetName, bool canBeOpen, bool canBeVirtual, bool forInvoker)
	{
		if (canBeOpen)
		{
			using (new BlockWriter("if (" + isOpenName + ")", _writer))
			{
				if (canBeVirtual)
				{
					using (new BlockWriter("if (" + ExpressionForFieldOfThis(_methodIsVirtualName) + ")", _writer))
					{
						IGeneratedMethodCodeWriter writer = _writer;
						writer.WriteLine($"if (il2cpp_codegen_method_is_generic_instance_method({methodInfoName}))");
						using (new IndentWriter(_writer))
						{
							using (new IndentWriter("if (il2cpp_codegen_method_is_interface_method(" + methodInfoName + "))", _writer))
							{
								WriteSetInvokeImplField(invokeMethod, "OpenGenericInterface", forInvoker);
							}
							using (new IndentWriter("else", _writer))
							{
								WriteSetInvokeImplField(invokeMethod, "OpenGenericVirtual", forInvoker);
							}
						}
						using (new IndentWriter("else", _writer))
						{
							using (new IndentWriter("if (il2cpp_codegen_method_is_interface_method(" + methodInfoName + "))", _writer))
							{
								WriteSetInvokeImplField(invokeMethod, "OpenInterface", forInvoker);
							}
							using (new IndentWriter("else", _writer))
							{
								WriteSetInvokeImplField(invokeMethod, "OpenVirtual", forInvoker);
							}
						}
					}
				}
				using (new ConditionalBlockWriter(canBeVirtual, "else", _writer))
				{
					WriteSetInvokeImplField(invokeMethod, "OpenInst", forInvoker);
				}
			}
		}
		using (new ConditionalBlockWriter(canBeOpen, "else", _writer))
		{
			using (new IndentWriter("if (" + cleanTargetName + " == NULL)", _writer))
			{
				_writer.WriteLine("il2cpp_codegen_raise_exception(il2cpp_codegen_get_argument_exception(NULL, \"Delegate to an instance method cannot have null 'this'.\"), NULL);");
			}
			WriteSetInvokeImplFieldForClosed(invokeMethod, forInvoker, needsBlock: false, "Inst");
		}
	}

	private void WriteSetInvokeImplField(MethodReference invokeMethod, string suffix, bool forInvoker, string invokerSuffix = "")
	{
		if (forInvoker)
		{
			if (!string.IsNullOrEmpty(invokerSuffix))
			{
				suffix += invokerSuffix;
			}
			suffix += "Invoker";
		}
		_writer.WriteFieldSetter(_invokeImplField, ExpressionForFieldOfThis(_invokeImplName), "(intptr_t)&" + invokeMethod.CppName + "_" + suffix);
	}

	private void WriteSetInvokeImplFieldForClosed(MethodReference invokeMethod, bool forInvoker, bool needsBlock, string invokerSuffix = "")
	{
		if (forInvoker)
		{
			WriteSetInvokeImplField(invokeMethod, "Closed", forInvoker: true, invokerSuffix);
			return;
		}
		if (needsBlock)
		{
			_writer.BeginBlock();
		}
		_writer.WriteFieldSetter(_invokeImplField, ExpressionForFieldOfThis(_invokeImplName), ExpressionForFieldOfThis(_methodPtrName));
		_writer.WriteFieldSetter(_invokeImplThisField, ExpressionForFieldOfThis(_invokeImplThisName), "(intptr_t)" + ExpressionForFieldOfThis(_targetName));
		if (needsBlock)
		{
			_writer.EndBlock();
		}
	}

	private void WriteMethodBodyForInvoke(MethodReference invokeMethod)
	{
		_writer.AddIncludeForTypeDefinition(_writer.Context, invokeMethod.GetResolvedThisType(_context));
		WriteDelegateInvokeCall(invokeMethod, "__this", "return ");
		_context.Global.Collectors.IndirectCalls.Add(_context, invokeMethod, IndirectCallUsage.Virtual | IndirectCallUsage.Static);
		_context.Global.Collectors.IndirectCalls.Add(_context, invokeMethod.ReturnType, new TypeReference[1] { _context.Global.Services.TypeProvider.SystemObject }.Concat(from p in invokeMethod.GetResolvedParameters(_context)
			select p.ParameterType).ToArray(), IndirectCallUsage.Static);
		if (ShouldEmitOpenInstanceInvocations(invokeMethod))
		{
			_context.Global.Collectors.IndirectCalls.Add(_context, invokeMethod, IndirectCallUsage.Virtual | IndirectCallUsage.Instance, skipFirstArg: true);
		}
	}

	private void WriteDelegateInvokeCall(MethodReference invokeMethod, string delegateVariableName, string returnValueStatement)
	{
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"typedef {MethodSignatureWriter.FormatReturnType(_context, invokeMethod.GetResolvedReturnType(_context))} (*{"FunctionPointerType"}) ({MethodSignatureWriter.FormatParameters(_context, invokeMethod, ParameterFormat.WithTypeThisObject, IncludeHiddenMethodInfo())});");
		List<string> parameters = MethodSignatureWriter.ParametersFor(_context, invokeMethod, ParameterFormat.WithName).ToList();
		parameters[0] = "(Il2CppObject*)" + ExpressionForFieldOf(delegateVariableName, _invokeImplThisName);
		if (IncludeHiddenMethodInfo())
		{
			parameters.Add("reinterpret_cast<RuntimeMethod*>(" + ExpressionForFieldOf(delegateVariableName, _methodName) + ")");
		}
		string call = Emit.Call(_context, $"(({"FunctionPointerType"}){ExpressionForFieldOf(delegateVariableName, _invokeImplName)})", parameters);
		_writer.WriteMethodCallWithReturnValueStatementIfNeeded(invokeMethod, returnValueStatement, call);
	}

	private bool ShouldEmitOpenInstanceInvocations(MethodReference invokeMethod)
	{
		if (!invokeMethod.HasParameters)
		{
			return false;
		}
		TypeReference resolveParameterType = invokeMethod.GetResolvedParameters(_context)[0].ParameterType;
		if (resolveParameterType.IsIl2CppFullySharedGenericType)
		{
			return true;
		}
		if (resolveParameterType.IsValueType)
		{
			return false;
		}
		return true;
	}

	private bool ShouldEmitOpenInstanceVirtualInvocations(MethodReference invokeMethod)
	{
		if (!invokeMethod.HasParameters)
		{
			return false;
		}
		TypeReference resolveParameterType = invokeMethod.GetResolvedParameters(_context)[0].ParameterType;
		if (resolveParameterType.IsIl2CppFullySharedGenericType)
		{
			return true;
		}
		if (resolveParameterType.IsValueType)
		{
			return false;
		}
		if (resolveParameterType.IsPointer)
		{
			return false;
		}
		if (resolveParameterType.IsByReference)
		{
			return false;
		}
		if (resolveParameterType.IsFunctionPointer)
		{
			return false;
		}
		if (resolveParameterType.Resolve().IsSealed)
		{
			return false;
		}
		if (invokeMethod.Parameters[0].IsIn)
		{
			return false;
		}
		return true;
	}

	private bool CallDelegatesViaInvokers(MethodReference methodReference)
	{
		if (!_context.Global.Parameters.DelegateCallsViaInvokers)
		{
			return methodReference.HasFullGenericSharingSignature(_context);
		}
		return true;
	}

	private bool IncludeHiddenMethodInfo()
	{
		return true;
	}

	private void WriteDirectCall(MethodReference invokeMethod, string methodPtrExpression, string parametersForTypeDef, string parametersForInvocation)
	{
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"typedef {MethodSignatureWriter.FormatReturnType(_context, invokeMethod.GetResolvedReturnType(_context))} (*{"FunctionPointerType"}) ({parametersForTypeDef});");
		string castedFunctionPointerExpression = $"(({"FunctionPointerType"}){methodPtrExpression})";
		string call = Emit.Call(_context, castedFunctionPointerExpression, parametersForInvocation);
		_writer.WriteMethodCallWithReturnValueStatementIfNeeded(invokeMethod, "return ", call);
	}

	private void WriteIndirectCall(MethodReference invokeMethod, TypeResolver typeResolver, IReadOnlyList<TypeReference> parameterTypes, List<string> parameters, VirtualMethodCallType callType, bool invokerCall)
	{
		switch (callType)
		{
		case VirtualMethodCallType.Virtual:
			parameters.Insert(0, "il2cpp_codegen_method_get_slot(method)");
			break;
		case VirtualMethodCallType.Interface:
			parameters.Insert(0, "il2cpp_codegen_method_get_slot(method)");
			parameters.Insert(1, "il2cpp_codegen_method_get_declaring_type(method)");
			break;
		default:
			parameters.Insert(0, "method");
			break;
		}
		if (invokerCall)
		{
			switch (callType)
			{
			case VirtualMethodCallType.Interface:
				parameters[2] = "(RuntimeObject*)" + parameters[2];
				break;
			case VirtualMethodCallType.InvokerCall:
				parameters.Insert(0, "(Il2CppMethodPointer)" + ExpressionForFieldOfThis(_methodPtrName));
				break;
			default:
				parameters[1] = "(RuntimeObject*)" + parameters[1];
				break;
			}
		}
		string call = Emit.Call(_context, _writer.VirtualCallInvokeMethod(invokeMethod, typeResolver, callType, invokerCall, parameterTypes), parameters);
		_writer.WriteMethodCallWithReturnValueStatementIfNeeded(invokeMethod, "return ", call);
	}

	private static string ExpressionForFieldOfThis(string targetFieldName)
	{
		return ExpressionForFieldOf("__this", targetFieldName);
	}

	private static string ExpressionForFieldOf(string variableName, string targetFieldName)
	{
		return variableName + "->" + targetFieldName;
	}

	private void WriteMethodBodyForBeginInvoke(MethodReference method, IRuntimeMetadataAccess metadataAccess)
	{
		IGeneratedMethodCodeWriter writer = _writer;
		writer.WriteLine($"void *__d_args[{method.Parameters.Count - 1}] = {{0}};");
		if (BeginInvokeHasAdditionalParameters(method))
		{
			ReadOnlyCollection<ParameterDefinition> parameters = method.GetResolvedParameters(_context);
			for (int i = 0; i < parameters.Count - 2; i++)
			{
				ParameterDefinition parameterDefinition = parameters[i];
				TypeReference origParameterType = parameterDefinition.ParameterType.WithoutModifiers();
				TypeReference parameterType = origParameterType;
				string parameterName = parameterDefinition.CppName;
				string valueString = parameterName;
				if (parameterType is ByReferenceType byReferenceType)
				{
					parameterType = byReferenceType.ElementType;
				}
				if (parameterType.GetRuntimeStorage(_context).IsVariableSized())
				{
					writer = _writer;
					writer.WriteLine($"RuntimeClass* {parameterName}_klass = il2cpp_codegen_class_from_type(il2cpp_codegen_method_parameter_type((MethodInfo*){ExpressionForFieldOfThis(_methodName)}, {i}));");
					valueString = $"(il2cpp_codegen_class_is_value_type({parameterName}_klass) ? Box({parameterName}_klass, {valueString}) : (void*){(origParameterType.IsByReference ? Emit.Dereference(valueString) : valueString)})";
				}
				else
				{
					if (origParameterType.IsByReference)
					{
						valueString = Emit.Dereference(valueString);
					}
					if (parameterType.IsValueType)
					{
						valueString = Emit.Box(_context, parameterType, valueString, metadataAccess);
					}
				}
				writer = _writer;
				writer.WriteLine($"__d_args[{i}] = {valueString};");
			}
		}
		_writer.WriteReturnStatement($"({method.GetResolvedReturnType(_context).CppNameForVariable})il2cpp_codegen_delegate_begin_invoke((RuntimeDelegate*)__this, __d_args, (RuntimeDelegate*){method.Parameters[method.Parameters.Count - 2].CppName}, (RuntimeObject*){method.Parameters[method.Parameters.Count - 1].CppName})");
	}

	private static bool BeginInvokeHasAdditionalParameters(MethodReference method)
	{
		return method.Parameters.Count > 2;
	}

	private void WriteMethodBodyForDelegateEndInvoke(MethodReference method)
	{
		ParameterDefinition asyncResultParam = method.Parameters[method.Parameters.Count - 1];
		string outArgsString = "0";
		List<string> outArgs = CollectOutArgsIfAny(method);
		IGeneratedMethodCodeWriter writer;
		if (outArgs.Count > 0)
		{
			_writer.WriteLine("void* ___out_args[] = {");
			foreach (string outArg in outArgs)
			{
				writer = _writer;
				writer.WriteLine($"{outArg},");
			}
			_writer.WriteLine("};");
			outArgsString = "___out_args";
		}
		if (method.ReturnType.IsVoid)
		{
			writer = _writer;
			writer.WriteLine($"il2cpp_codegen_delegate_end_invoke((Il2CppAsyncResult*) {asyncResultParam.CppName}, {outArgsString});");
			return;
		}
		writer = _writer;
		writer.WriteLine($"RuntimeObject *__result = il2cpp_codegen_delegate_end_invoke((Il2CppAsyncResult*) {asyncResultParam.CppName}, {outArgsString});");
		TypeReference returnType = method.GetResolvedReturnType(_context);
		if (returnType.GetRuntimeStorage(_context).IsVariableSized())
		{
			writer = _writer;
			writer.WriteLine($"RuntimeClass* returnType = il2cpp_codegen_class_from_type(il2cpp_codegen_method_return_type((MethodInfo*){ExpressionForFieldOfThis(_methodName)}));");
			_writer.WriteLine("uint32_t returnTypeSize = il2cpp_codegen_sizeof(returnType);");
			_writer.WriteLine("void* unboxStorage = alloca(returnTypeSize);");
			_writer.WriteLine("void* unboxed = UnBox_Any((RuntimeObject*)__result, returnType, unboxStorage);");
			_writer.WriteLine("il2cpp_codegen_memcpy(il2cppRetVal, unboxed, returnTypeSize);");
		}
		else if (!returnType.IsValueType)
		{
			_writer.WriteReturnStatement("(" + returnType.CppNameForVariable + ")__result");
		}
		else
		{
			_writer.WriteReturnStatement("*" + Emit.CastToPointer(_context, returnType, "UnBox ((RuntimeObject*)__result)"));
		}
	}

	private List<string> CollectOutArgsIfAny(MethodReference method)
	{
		List<string> outArgs = new List<string>();
		for (int i = 0; i < method.Parameters.Count - 1; i++)
		{
			if (method.Parameters[i].ParameterType.IsByReference)
			{
				outArgs.Add(method.Parameters[i].CppName);
			}
		}
		return outArgs;
	}
}
