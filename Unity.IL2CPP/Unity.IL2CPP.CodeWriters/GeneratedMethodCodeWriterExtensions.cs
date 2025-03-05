using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

namespace Unity.IL2CPP.CodeWriters;

public static class GeneratedMethodCodeWriterExtensions
{
	public static void AddIncludeForMethodDeclaration(this IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		SourceWritingContext context = writer.Context;
		TypeReference type = method.DeclaringType;
		if ((type.IsInterface && !type.IsComOrWindowsRuntimeInterface(context) && !method.IsDefaultInterfaceMethod) || type.IsArray || type.HasGenericParameters)
		{
			return;
		}
		IDirectDeclarationAccessForGeneratedMethodCodeWriter declarationsWriter = (IDirectDeclarationAccessForGeneratedMethodCodeWriter)writer;
		if (declarationsWriter.AddMethodDeclaration(method))
		{
			MethodSignatureWriter.RecordIncludes(writer, method);
			if (method.CanShare(context))
			{
				MethodReference sharedMethod = method.GetSharedMethod(context);
				MethodSignatureWriter.RecordIncludes(writer, sharedMethod);
				declarationsWriter.AddSharedMethodDeclaration(sharedMethod);
			}
		}
	}

	public static string VirtualCallInvokeMethod(this IGeneratedMethodCodeWriter writer, IMethodSignature method, TypeResolver typeResolver, VirtualMethodCallType callType, bool doCallViaInvoker = false, IEnumerable<TypeReference> parameterTypes = null)
	{
		if (!(method is MethodReference) && callType != VirtualMethodCallType.InvokerCall)
		{
			throw new ArgumentException("Only InvokerCalls support for MethodSignatures");
		}
		SourceWritingContext context = writer.Context;
		IDirectDeclarationAccessForGeneratedMethodCodeWriter obj = (IDirectDeclarationAccessForGeneratedMethodCodeWriter)writer;
		if (context.Global.Parameters.VirtualCallsViaInvokers || callType == VirtualMethodCallType.InvokerCall || callType == VirtualMethodCallType.ConstrainedInvokerCall)
		{
			doCallViaInvoker = true;
		}
		TypeReference returnType;
		if (method is MethodReference methodReference)
		{
			returnType = typeResolver.ResolveReturnType(methodReference);
			if (methodReference.ContainsGenericParameter)
			{
				throw new InvalidOperationException(methodReference.FullName);
			}
		}
		else
		{
			returnType = typeResolver.ResolveReturnType(method);
		}
		bool returnAsByRefParameter = returnType.IsReturnedByRef(context);
		bool methodReturnsVoid = method.ReturnType.IsVoid;
		bool num = !methodReturnsVoid && returnAsByRefParameter;
		bool isFunc = !methodReturnsVoid && !returnAsByRefParameter;
		List<TypeReference> templateArguments = new List<TypeReference>();
		if (isFunc)
		{
			templateArguments.Add(returnType);
		}
		MethodReference methodReferenceForParameterTypes = method as MethodReference;
		if (methodReferenceForParameterTypes != null)
		{
			templateArguments.AddRange(parameterTypes ?? method.Parameters.Select((ParameterDefinition p) => typeResolver.ResolveParameterType(methodReferenceForParameterTypes, p)));
		}
		else
		{
			templateArguments.AddRange(parameterTypes ?? method.Parameters.Select((ParameterDefinition p) => typeResolver.Resolve(p.ParameterType)));
		}
		if (num)
		{
			templateArguments.Add(returnType.CreatePointerType(context));
		}
		string templateString = string.Empty;
		if (templateArguments.Count > 0)
		{
			templateString = "< " + templateArguments.Select((TypeReference variableType) => variableType.CppNameForVariable).AggregateWithComma(context) + " >";
		}
		ReadOnlyCollection<InvokerParameterData> invokerParameters = InvokerParameterData.FromParameterList(context, templateArguments.Skip(isFunc ? 1 : 0), doCallViaInvoker);
		obj.AddVirtualMethodDeclarationData(new VirtualMethodDeclarationData(method, invokerParameters, methodReturnsVoid || returnAsByRefParameter, callType, doCallViaInvoker));
		return InvokerData.FormatInvokerName(callType, invokerParameters.Count, isFunc, doCallViaInvoker) + templateString + "::Invoke";
	}

	public static string VirtualCallInvokeMethod(this IGeneratedMethodCodeWriter writer, MethodReference method, TypeResolver typeResolver, bool doCallViaInvoker = false)
	{
		bool isInterface = method.Resolve().DeclaringType.IsInterface;
		bool isGeneric = method.Resolve().HasGenericParameters;
		VirtualMethodCallType callType = ((isInterface && isGeneric) ? VirtualMethodCallType.GenericInterface : (isInterface ? VirtualMethodCallType.Interface : (isGeneric ? VirtualMethodCallType.GenericVirtual : VirtualMethodCallType.Virtual)));
		return writer.VirtualCallInvokeMethod(method, typeResolver, callType, doCallViaInvoker);
	}

	public static void WriteInternalCallResolutionStatement(this IGeneratedMethodCodeWriter writer, MethodDefinition method, IRuntimeMetadataAccess metadataAccess)
	{
		string nameWithoutReturnType = method.FullName.Substring(method.FullName.IndexOf(" ") + 1);
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"typedef {MethodSignatureWriter.GetICallMethodVariable(writer.Context, method)};");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"static {method.CppName}_ftn _il2cpp_icall_func;");
		writer.WriteLine("if (!_il2cpp_icall_func)");
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"_il2cpp_icall_func = ({method.CppName}_ftn)il2cpp_codegen_resolve_icall (\"{nameWithoutReturnType}\");");
	}

	public static void AddInternalPInvokeMethodDeclaration(this IGeneratedMethodCodeWriter writer, string methodName, string internalPInvokeDeclaration, string moduleName, bool forForcedInternalPInvoke, bool isExplicitlyInternal)
	{
		IDirectDeclarationAccessForGeneratedMethodCodeWriter declarationsWriter = (IDirectDeclarationAccessForGeneratedMethodCodeWriter)writer;
		using Returnable<StringBuilder> buildContext = writer.Context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder pinvokeDeclaration = buildContext.Value;
		if (forForcedInternalPInvoke)
		{
			pinvokeDeclaration.AppendFormat("#if {0} || {1}\n", "FORCE_PINVOKE_INTERNAL", PInvokeMethodBodyWriter.FORCE_PINVOKE_lib_INTERNAL(moduleName));
			pinvokeDeclaration.Append(internalPInvokeDeclaration);
			pinvokeDeclaration.Append("\n#endif\n");
			declarationsWriter.TryAddInternalPInvokeMethodDeclarationsForForcedInternalPInvoke(methodName, pinvokeDeclaration.ToString());
			return;
		}
		if (!isExplicitlyInternal)
		{
			pinvokeDeclaration.AppendFormat("#if !{0} && !{1}\n", "FORCE_PINVOKE_INTERNAL", PInvokeMethodBodyWriter.FORCE_PINVOKE_lib_INTERNAL(moduleName));
		}
		pinvokeDeclaration.Append(internalPInvokeDeclaration);
		pinvokeDeclaration.Append('\n');
		if (!isExplicitlyInternal)
		{
			pinvokeDeclaration.Append("#endif\n");
		}
		declarationsWriter.TryAddInternalPInvokeMethodDeclarations(methodName, pinvokeDeclaration.ToString());
	}
}
