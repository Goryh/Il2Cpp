using System;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;

namespace Unity.IL2CPP;

public class ComInterfaceWriter
{
	public static bool IsVTableGapMethod(MethodReference method)
	{
		if (method.IsRuntimeSpecialName)
		{
			return method.Name.StartsWith("_VtblGap");
		}
		return false;
	}

	public static void WriteComInterfaceFor(ReadOnlyContext context, IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, out TypeReference[] typesRequiringInteropGuids)
	{
		if (type.Is(Il2CppCustomType.IActivationFactory))
		{
			typesRequiringInteropGuids = null;
			return;
		}
		if (context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(type.FullName);
		}
		WriteForwardDeclarations(context, writer, type);
		string baseInterface = (type.Resolve().IsExposedToWindowsRuntime() ? "Il2CppIInspectable" : "Il2CppIUnknown");
		writer.WriteLine($"struct NOVTABLE {type.CppName} : {baseInterface}");
		using (new BlockWriter(writer, semicolon: true))
		{
			writer.WriteStatement("static const Il2CppGuid IID");
			TypeReference clrType = context.Global.Services.WindowsRuntime.ProjectToCLR(type);
			if (clrType != type && clrType.IsInterface)
			{
				typesRequiringInteropGuids = new TypeReference[2] { type, clrType };
			}
			else
			{
				typesRequiringInteropGuids = new TypeReference[1] { type };
			}
			TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(type);
			int vtableGapOffset = 0;
			foreach (MethodDefinition method in type.Resolve().Methods)
			{
				if (IsVTableGapMethod(method))
				{
					WriteVTableGapMethods(writer, method, ref vtableGapOffset);
					continue;
				}
				MethodReference resolvedMethod = typeResolver.Resolve(method);
				writer.Write(GetSignature(context, resolvedMethod, resolvedMethod, typeResolver, null, isImplementation: false));
				writer.WriteLine(" = 0;");
			}
		}
	}

	private static void WriteVTableGapMethods(IReadOnlyContextGeneratedCodeWriter writer, MethodDefinition method, ref int vtableGapOffset)
	{
		int vtblGapNameLen = "_VtblGap".Length;
		int gapCount = 1;
		ReadOnlySpan<char> methodName = method.Name.AsSpan();
		if (methodName.Length > vtblGapNameLen)
		{
			int indexOfGapLengthStart = methodName.Slice(vtblGapNameLen).IndexOf('_');
			if (indexOfGapLengthStart > -1 && !int.TryParse(methodName.Slice(vtblGapNameLen + indexOfGapLengthStart + 1), out gapCount))
			{
				gapCount = 1;
			}
		}
		for (int i = 0; i < gapCount; i++)
		{
			writer.WriteStatement($"virtual void STDCALL {"_VtblGap"}_{i + vtableGapOffset}() = 0");
		}
		vtableGapOffset += gapCount;
	}

	public static string GetSignature(ReadOnlyContext context, MethodReference method, MethodReference interfaceMethod, TypeResolver typeResolver, string typeName = null, bool isImplementation = true)
	{
		using Returnable<StringBuilder> builderContext = context.Global.Services.Factory.CheckoutStringBuilder();
		StringBuilder sb = builderContext.Value;
		MarshalType marshalType = ((!interfaceMethod.DeclaringType.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
		bool preserveSig = interfaceMethod.Resolve().IsPreserveSig;
		string returnType = "il2cpp_hresult_t";
		if (preserveSig)
		{
			if (interfaceMethod.ReturnType.MetadataType == MetadataType.Void)
			{
				returnType = "void";
			}
			else
			{
				TypeReference methodReturnType = typeResolver.Resolve(interfaceMethod.ReturnType);
				MarshalInfo marshalInfo = interfaceMethod.MethodReturnType.MarshalInfo;
				returnType = MarshalDataCollector.MarshalInfoWriterFor(context, methodReturnType, marshalType, marshalInfo, useUnicodeCharSet: true).GetMarshaledTypes(context)[^1].DecoratedName;
			}
		}
		if (string.IsNullOrEmpty(typeName))
		{
			sb.Append("virtual ");
			sb.Append(returnType);
			sb.Append(" STDCALL ");
		}
		else
		{
			sb.Append(returnType);
			sb.Append(" ");
			sb.Append(typeName);
			sb.Append("::");
		}
		sb.Append(interfaceMethod.CppName);
		sb.Append('(');
		sb.Append(MethodSignatureWriter.FormatComMethodParameterList(context, method, interfaceMethod, typeResolver, marshalType, includeTypeNames: true, preserveSig));
		sb.Append(')');
		if (string.IsNullOrEmpty(typeName) && isImplementation)
		{
			sb.Append(" IL2CPP_OVERRIDE");
		}
		return sb.ToString();
	}

	private static void WriteForwardDeclarations(ReadOnlyContext context, IReadOnlyContextGeneratedCodeWriter writer, TypeReference type)
	{
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(type);
		MarshalType marshalType = ((!type.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
		foreach (MethodDefinition method in type.Resolve().Methods)
		{
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				MarshalDataCollector.MarshalInfoWriterFor(context, typeResolver.Resolve(parameter.ParameterType), marshalType, parameter.MarshalInfo, useUnicodeCharSet: true).WriteIncludesForFieldDeclaration(writer);
			}
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				MarshalDataCollector.MarshalInfoWriterFor(context, typeResolver.Resolve(method.ReturnType), marshalType, method.MethodReturnType.MarshalInfo, useUnicodeCharSet: true).WriteIncludesForFieldDeclaration(writer);
			}
		}
	}
}
