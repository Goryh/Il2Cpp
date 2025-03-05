using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP;

public class MethodSignatureWriter
{
	public static string GetICallMethodVariable(ReadOnlyContext context, MethodDefinition method)
	{
		return $"{ICallReturnTypeFor(context, method)} (*{method.CppName}_ftn) ({FormatParametersForICall(context, method, ParameterFormat.WithType)})";
	}

	public static string GetMethodPointerForVTable(ReadOnlyContext context, MethodReference method)
	{
		ParameterFormat parameterFormat = ((method.DeclaringType.IsValueType && method.HasThis) ? ParameterFormat.WithTypeThisObject : ParameterFormat.WithType);
		return GetMethodPointer(context, method, parameterFormat);
	}

	public static string GetMethodPointer(ReadOnlyContext context, MethodReference method, ParameterFormat parameterFormat = ParameterFormat.WithType)
	{
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType, method);
		return GetMethodSignature("(*)", FormatReturnType(context, typeResolver.ResolveReturnType(method)), FormatParameters(context, method, parameterFormat, includeHiddenMethodInfo: true), string.Empty);
	}

	internal static string GetMethodSignatureForDefinition(MethodWriteContext context, IReadOnlyContextGeneratedCodeWriter writer)
	{
		return BuildMethodSignature(context, writer, "IL2CPP_EXTERN_C", string.Empty, NeedsHiddenMethodInfoForDefinition(context, context.MethodReference));
	}

	internal static string GetInlineMethodSignature(MethodWriteContext context, IReadOnlyContextGeneratedCodeWriter writer)
	{
		return BuildMethodSignature(context, writer, "IL2CPP_MANAGED_FORCE_INLINE", "_inline", NeedsHiddenMethodInfo(context, context.MethodReference, MethodCallType.Normal, forFullGenericSharing: false));
	}

	internal static void WriteMethodSignatureRaw(ReadOnlyContext context, IDirectWriter writer, MethodReference method)
	{
		WriteRawMethodSignature(context, writer, method, "IL2CPP_EXTERN_C", string.Empty, NeedsHiddenMethodInfo(context, method, MethodCallType.Normal, forFullGenericSharing: false));
	}

	internal static void WriteMethodSignatureRawInline(ReadOnlyContext context, IDirectWriter writer, MethodReference method)
	{
		WriteRawMethodSignature(context, writer, method, "IL2CPP_MANAGED_FORCE_INLINE", "_inline", NeedsHiddenMethodInfo(context, method, MethodCallType.Normal, forFullGenericSharing: false));
	}

	private static string BuildRawMethodSignature(MethodWriteContext context, string specifier, string namePostfix, bool includeHiddenMethodInfo)
	{
		return BuildRawMethodSignature(context, context.MethodReference, specifier, namePostfix, includeHiddenMethodInfo);
	}

	private static string BuildRawMethodSignature(ReadOnlyContext context, MethodReference method, string specifier, string namePostfix, bool includeHiddenMethodInfo)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		builder.Append(specifier);
		builder.Append(' ');
		AppendMethodAttributes(context, builder, method);
		string callingConvention = null;
		if (method.IsUnmanagedCallersOnly)
		{
			callingConvention = UnmanagedCallersOnlyUtils.GetUnmanagedCallersCallingConv(method);
		}
		AppendMethodSignatureWithPostfix(builder, method.CppName, namePostfix, FormatReturnType(context, method.GetResolvedReturnType(context)), FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), callingConvention);
		return builder.ToString();
	}

	private static void WriteRawMethodSignature(ReadOnlyContext context, IDirectWriter writer, MethodReference method, string specifier, string namePostfix, bool includeHiddenMethodInfo)
	{
		writer.Write(specifier);
		writer.Write(' ');
		WriteMethodAttributes(context, writer, method);
		string callingConvention = null;
		if (method.IsUnmanagedCallersOnly)
		{
			callingConvention = UnmanagedCallersOnlyUtils.GetUnmanagedCallersCallingConv(method);
		}
		WriteMethodSignatureWithPostfix(writer, method.CppName, namePostfix, FormatReturnType(context, method.GetResolvedReturnType(context)), FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), callingConvention);
	}

	private static string BuildMethodSignature(MethodWriteContext context, IReadOnlyContextGeneratedCodeWriter writer, string specifier, string namePostfix, bool includeHiddenMethodInfo)
	{
		RecordIncludes(writer, context.MethodReference, context.TypeResolver);
		return BuildRawMethodSignature(context, specifier, namePostfix, includeHiddenMethodInfo);
	}

	public static string GetSharedMethodSignature(MethodWriteContext context, IReadOnlyContextGeneratedCodeWriter writer)
	{
		return BuildMethodSignature(context, writer, "IL2CPP_EXTERN_C", "_gshared", includeHiddenMethodInfo: true);
	}

	public static string GetSharedMethodSignatureInline(MethodWriteContext context, IReadOnlyContextGeneratedCodeWriter writer)
	{
		return BuildMethodSignature(context, writer, "IL2CPP_MANAGED_FORCE_INLINE", "_gshared_inline", includeHiddenMethodInfo: true);
	}

	public static string GetSharedMethodSignatureRaw(ReadOnlyContext context, MethodReference method)
	{
		return BuildRawMethodSignature(context, method, "IL2CPP_EXTERN_C", "_gshared", includeHiddenMethodInfo: true);
	}

	public static string GetSharedMethodSignatureRawInline(ReadOnlyContext context, MethodReference method)
	{
		return BuildRawMethodSignature(context, method, "IL2CPP_MANAGED_FORCE_INLINE", "_gshared_inline", includeHiddenMethodInfo: true);
	}

	public static string FormatReturnType(ReadOnlyContext context, TypeReference managedReturnType)
	{
		if (managedReturnType.IsVoid || managedReturnType.IsReturnedByRef(context))
		{
			return context.Global.Services.TypeProvider.SystemVoid.CppNameForVariable;
		}
		return managedReturnType.CppNameForVariable;
	}

	public static string FormatParametersForICall(ReadOnlyContext context, MethodReference method, ParameterFormat format = ParameterFormat.WithTypeAndName)
	{
		List<string> parameters = ParametersForInternal(context, method, format, includeHiddenMethodInfo: false, useVoidPointerForThis: false, returnAsByRefParam: false).ToList();
		if (parameters.Count != 0)
		{
			return parameters.AggregateWithComma(context);
		}
		return string.Empty;
	}

	public static string FormatParameters(ReadOnlyContext context, MethodReference method, ParameterFormat format = ParameterFormat.WithTypeAndName, bool includeHiddenMethodInfo = false)
	{
		List<string> parameters = ParametersForInternal(context, method, format, includeHiddenMethodInfo, useVoidPointerForThis: false, method.ReturnValueIsByRef(context)).ToList();
		if (parameters.Count != 0)
		{
			return parameters.AggregateWithComma(context);
		}
		return string.Empty;
	}

	public static IEnumerable<string> ParametersForICall(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format = ParameterFormat.WithTypeAndName)
	{
		return ParametersForInternal(context, methodDefinition, format, includeHiddenMethodInfo: false, useVoidPointerForThis: false, returnAsByRefParam: false);
	}

	public static IEnumerable<string> ParametersFor(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format = ParameterFormat.WithTypeAndName, bool includeHiddenMethodInfo = false, bool useVoidPointerForThis = false)
	{
		return ParametersForInternal(context, methodDefinition, format, includeHiddenMethodInfo, useVoidPointerForThis, methodDefinition.ReturnValueIsByRef(context));
	}

	private static IEnumerable<string> ParametersForInternal(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format, bool includeHiddenMethodInfo, bool useVoidPointerForThis, bool returnAsByRefParam)
	{
		switch (format)
		{
		case ParameterFormat.WithTypeAndNameThisObject:
		{
			TypeReference thisType = ThisTypeFor(context, methodDefinition);
			if (useVoidPointerForThis)
			{
				yield return FormatThisParameterAsVoidPointer(thisType);
			}
			else
			{
				yield return FormatParameterName(context, methodDefinition.Module.TypeSystem.Object, "__this", format, IsJobStruct(thisType));
			}
			break;
		}
		case ParameterFormat.WithTypeThisObject:
			yield return useVoidPointerForThis ? "void*" : context.Global.Services.TypeProvider.ObjectTypeReference.CppNameForVariable;
			break;
		default:
			if (methodDefinition.HasThis)
			{
				yield return FormatThis(context, format, ThisTypeFor(context, methodDefinition));
			}
			break;
		case ParameterFormat.WithTypeAndNameNoThis:
		case ParameterFormat.WithTypeNoThis:
		case ParameterFormat.WithNameNoThis:
			break;
		}
		foreach (ParameterDefinition parameterDefinition in methodDefinition.GetResolvedParameters(context))
		{
			yield return ParameterStringFor(context, methodDefinition, format, parameterDefinition, !parameterDefinition.ParameterType.IsValueType && !parameterDefinition.ParameterType.IsPointer);
		}
		if (returnAsByRefParam)
		{
			TypeReference returnType = context.Global.Services.TypeFactory.ResolverFor(methodDefinition.DeclaringType, methodDefinition).ResolveReturnType(methodDefinition);
			if (returnType.IsNotVoid)
			{
				yield return FormatParameterName(context, returnType.CreatePointerType(context), "il2cppRetVal", format);
			}
		}
		if (includeHiddenMethodInfo)
		{
			yield return FormatHiddenMethodArgument(format);
		}
	}

	private static TypeReference ThisTypeFor(ReadOnlyContext context, MethodReference methodDefinition)
	{
		TypeReference thisType = methodDefinition.DeclaringType;
		if (thisType.IsValueType)
		{
			thisType = thisType.CreatePointerType(context);
		}
		else if (thisType.IsSpecialSystemBaseType())
		{
			thisType = methodDefinition.Module.TypeSystem.Object;
		}
		return thisType;
	}

	public static string ICallReturnTypeFor(ReadOnlyContext context, MethodDefinition method)
	{
		return method.ReturnType.CppNameForVariable;
	}

	private static string FormatMonoErrorForICall(ParameterFormat format)
	{
		switch (format)
		{
		case ParameterFormat.WithTypeAndName:
		case ParameterFormat.WithTypeAndNameNoThis:
		case ParameterFormat.WithTypeAndNameThisObject:
			return "MonoError* error_icall";
		case ParameterFormat.WithType:
		case ParameterFormat.WithTypeThisObject:
			return "MonoError*";
		case ParameterFormat.WithName:
			return "&error_icall";
		default:
			throw new ArgumentOutOfRangeException("format");
		}
	}

	private static string FormatHiddenMethodArgument(ParameterFormat format)
	{
		switch (format)
		{
		case ParameterFormat.WithTypeAndName:
		case ParameterFormat.WithTypeAndNameNoThis:
		case ParameterFormat.WithTypeAndNameThisObject:
			return "const RuntimeMethod* method";
		case ParameterFormat.WithType:
		case ParameterFormat.WithTypeThisObject:
		case ParameterFormat.WithTypeNoThis:
			return "const RuntimeMethod*";
		case ParameterFormat.WithName:
		case ParameterFormat.WithNameNoThis:
			return "method";
		default:
			throw new ArgumentOutOfRangeException("format");
		}
	}

	public static string ParameterStringFor(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format, ParameterDefinition parameterDefinition, bool useHandles = false)
	{
		return FormatParameterName(context, parameterDefinition.ParameterType, parameterDefinition.CppName, format);
	}

	public static string BuildMethodAttributes(ReadOnlyContext context, MethodReference method)
	{
		List<string> attributes = new List<string>();
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition.NoInlining || IsJobStruct(ThisTypeFor(context, methodDefinition)))
		{
			attributes.Add("IL2CPP_NO_INLINE");
		}
		attributes.Add("IL2CPP_METHOD_ATTR");
		return attributes.AggregateWithSpace();
	}

	public static void AppendMethodAttributes(ReadOnlyContext context, StringBuilder builder, MethodReference method)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition.NoInlining || IsJobStruct(ThisTypeFor(context, methodDefinition)))
		{
			builder.Append("IL2CPP_NO_INLINE");
			builder.Append(' ');
		}
		builder.Append("IL2CPP_METHOD_ATTR");
		builder.Append(' ');
	}

	public static void WriteMethodAttributes(ReadOnlyContext context, IDirectWriter writer, MethodReference method)
	{
		MethodDefinition methodDefinition = method.Resolve();
		if (methodDefinition.NoInlining || IsJobStruct(ThisTypeFor(context, methodDefinition)))
		{
			writer.Write("IL2CPP_NO_INLINE");
			writer.Write(' ');
		}
		writer.Write("IL2CPP_METHOD_ATTR");
		writer.Write(' ');
	}

	internal static string GetMethodSignature(string name, string returnType, string parameters, string specifiers = "", string attributes = "")
	{
		return $"{specifiers} {attributes} {returnType} {name} ({parameters})";
	}

	private static void AppendMethodSignatureWithPostfix(StringBuilder builder, string name, string namePostfix, string returnType, string parameters, string callingConvention)
	{
		builder.Append(returnType);
		builder.Append(' ');
		if (callingConvention != null)
		{
			builder.Append(callingConvention);
			builder.Append(' ');
		}
		builder.Append(name);
		builder.Append(namePostfix);
		builder.Append(' ');
		builder.Append('(');
		builder.Append(parameters);
		builder.Append(')');
		builder.Append(' ');
	}

	private static void WriteMethodSignatureWithPostfix(IDirectWriter writer, string name, string namePostfix, string returnType, string parameters, string callingConvention)
	{
		writer.Write(returnType);
		writer.Write(' ');
		if (callingConvention != null)
		{
			writer.Write(callingConvention);
			writer.Write(' ');
		}
		writer.Write(name);
		writer.Write(namePostfix);
		writer.Write(' ');
		writer.Write('(');
		writer.Write(parameters);
		writer.Write(')');
		writer.Write(' ');
	}

	internal static void RecordIncludes(IGeneratedCodeWriter writer, MethodReference method)
	{
		TypeResolver typeResolver = writer.Context.Global.Services.TypeFactory.ResolverFor(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
		if (method.HasThis)
		{
			writer.AddIncludesForTypeReference(writer.Context, method.DeclaringType.IsComOrWindowsRuntimeInterface(writer.Context) ? writer.Context.Global.Services.TypeProvider.SystemObject : method.DeclaringType);
		}
		if (method.ReturnType.IsNotVoid)
		{
			writer.AddIncludesForTypeReference(writer.Context, typeResolver.ResolveReturnType(method));
		}
		foreach (ParameterDefinition parameter in method.Parameters)
		{
			writer.AddIncludesForTypeReference(writer.Context, typeResolver.ResolveParameterType(method, parameter), requiresCompleteType: true);
		}
	}

	internal static void RecordIncludes(IReadOnlyContextGeneratedCodeWriter writer, MethodReference method, TypeResolver typeResolver)
	{
		if (method.HasThis)
		{
			writer.AddIncludesForTypeReference(writer.Context, method.DeclaringType.IsComOrWindowsRuntimeInterface(writer.Context) ? writer.Context.Global.Services.TypeProvider.SystemObject : method.DeclaringType);
		}
		if (method.ReturnType.IsNotVoid)
		{
			writer.AddIncludesForTypeReference(writer.Context, typeResolver.ResolveReturnType(method));
		}
		foreach (ParameterDefinition parameter in method.Parameters)
		{
			writer.AddIncludesForTypeReference(writer.Context, typeResolver.ResolveParameterType(method, parameter), requiresCompleteType: true);
		}
	}

	private static string FormatThis(ReadOnlyContext context, ParameterFormat format, TypeReference thisType)
	{
		return FormatParameterName(context, thisType, "__this", format, IsJobStruct(thisType));
	}

	private static string FormatThisParameterAsVoidPointer(TypeReference thisType)
	{
		string modifier = (IsJobStruct(thisType) ? "IL2CPP_PARAMETER_RESTRICT " : string.Empty);
		return "void* " + modifier + "__this";
	}

	private static string FormatParameterName(ReadOnlyContext context, TypeReference parameterType, string parameterName, ParameterFormat format, bool addRestrictModifier = false)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder builder = builderContext.Value;
		if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithType || format == ParameterFormat.WithTypeNoThis || format == ParameterFormat.WithTypeAndNameThisObject || format == ParameterFormat.WithTypeThisObject)
		{
			if (context.Global.Parameters.EmitComments && parameterType.GetRuntimeFieldLayout(context) == RuntimeFieldLayoutKind.Variable)
			{
				StringBuilder stringBuilder = builder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder);
				handler.AppendLiteral("/*");
				handler.AppendFormatted(parameterType.FullName);
				handler.AppendLiteral("*/");
				stringBuilder.Append(ref handler);
			}
			builder.Append(parameterType.CppNameForVariable);
			if (addRestrictModifier)
			{
				builder.Append(" IL2CPP_PARAMETER_RESTRICT");
			}
		}
		if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithTypeAndNameThisObject)
		{
			builder.Append(' ');
		}
		if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithName || format == ParameterFormat.WithTypeAndNameThisObject || format == ParameterFormat.WithNameNoThis)
		{
			builder.Append(parameterName);
		}
		return builder.ToString();
	}

	public static bool CanDevirtualizeMethodCall(MethodDefinition method)
	{
		if (method.IsVirtual && !method.DeclaringType.IsSealed)
		{
			return method.IsFinal;
		}
		return true;
	}

	public static bool NeedsMethodMetadataCollected(ReadOnlyContext context, MethodReference methodReference, bool forFullGenericSharing)
	{
		return NeedsHiddenMethodInfo(context, methodReference, MethodCallType.Normal, forFullGenericSharing);
	}

	public static bool NeedsHiddenMethodInfo(ReadOnlyContext context, MethodReference method, MethodCallType callType, bool forFullGenericSharing)
	{
		if (IntrinsicRemap.ShouldRemap(context, method, forFullGenericSharing) && !IntrinsicRemap.StillNeedsHiddenMethodInfo(context, method, forFullGenericSharing))
		{
			return false;
		}
		if (callType == MethodCallType.Virtual && !CanDevirtualizeMethodCall(method.Resolve()))
		{
			return false;
		}
		return NeedsHiddenMethodInfoForDefinition(context, method);
	}

	private static bool NeedsHiddenMethodInfoForDefinition(ReadOnlyContext context, MethodReference method)
	{
		if (ArrayNaming.IsSpecialArrayMethod(method))
		{
			return false;
		}
		if (MethodWriter.IsGetOrSetGenericValueOnArray(method))
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
		if (method.IsUnmanagedCallersOnly)
		{
			return false;
		}
		return true;
	}

	internal static string FormatProjectedComCallableWrapperMethodDeclaration(SourceWritingContext context, MethodReference interfaceMethod, TypeResolver typeResolver, MarshalType marshalType)
	{
		string parameterList = FormatComMethodParameterList(context, interfaceMethod, interfaceMethod, typeResolver, marshalType, includeTypeNames: true, preserveSig: false);
		string methodName = context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(interfaceMethod);
		string thisTypeName = interfaceMethod.DeclaringType.CppNameForVariable;
		if (!string.IsNullOrEmpty(parameterList))
		{
			return $"il2cpp_hresult_t {methodName}({thisTypeName} {"__this"}, {parameterList})";
		}
		return $"il2cpp_hresult_t {methodName}({thisTypeName} {"__this"})";
	}

	internal static string FormatComMethodParameterList(ReadOnlyContext context, MethodReference interopMethod, MethodReference interfaceMethod, TypeResolver typeResolver, MarshalType marshalType, bool includeTypeNames, bool preserveSig)
	{
		List<string> parameters = new List<string>();
		int parameterIndex = 0;
		foreach (ParameterDefinition parameter in interopMethod.Parameters)
		{
			MarshalInfo marshalInfo = interfaceMethod.Parameters[parameterIndex].MarshalInfo;
			TypeReference parameterType = typeResolver.Resolve(parameter.ParameterType);
			MarshaledType[] marshaledTypes = MarshalDataCollector.MarshalInfoWriterFor(context, parameterType, marshalType, marshalInfo, useUnicodeCharSet: true).GetMarshaledTypes(context);
			foreach (MarshaledType type in marshaledTypes)
			{
				parameters.Add(string.Format(includeTypeNames ? "{0} {1}" : "{1}", type.DecoratedName, parameter.CppName + type.VariableName));
			}
			parameterIndex++;
		}
		TypeReference returnType = typeResolver.Resolve(interopMethod.ReturnType);
		if (returnType.IsNotVoid)
		{
			MarshalInfo marshalInfo2 = interfaceMethod.MethodReturnType.MarshalInfo;
			MarshaledType[] returnMarshaledTypes = MarshalDataCollector.MarshalInfoWriterFor(context, returnType, marshalType, marshalInfo2, useUnicodeCharSet: true).GetMarshaledTypes(context);
			for (int j = 0; j < returnMarshaledTypes.Length - 1; j++)
			{
				parameters.Add(string.Format(includeTypeNames ? "{0}* {1}" : "{1}", returnMarshaledTypes[j].DecoratedName, context.Global.Services.Naming.ForComInterfaceReturnParameterName() + returnMarshaledTypes[j].VariableName));
			}
			if (!preserveSig)
			{
				parameters.Add(string.Format(includeTypeNames ? "{0}* {1}" : "{1}", returnMarshaledTypes[^1].DecoratedName, context.Global.Services.Naming.ForComInterfaceReturnParameterName()));
			}
		}
		return parameters.AggregateWithComma(context);
	}

	private static bool IsJobStruct(TypeReference thisType)
	{
		TypeDefinition thisTypeDefinition = thisType.Resolve();
		if (thisTypeDefinition.HasAttribute("Unity.Jobs.LowLevel.Unsafe", "JobProducerTypeAttribute"))
		{
			return true;
		}
		foreach (InterfaceImplementation @interface in thisTypeDefinition.Interfaces)
		{
			if (@interface.InterfaceType.HasAttribute("Unity.Jobs.LowLevel.Unsafe", "JobProducerTypeAttribute"))
			{
				return true;
			}
		}
		return false;
	}
}
