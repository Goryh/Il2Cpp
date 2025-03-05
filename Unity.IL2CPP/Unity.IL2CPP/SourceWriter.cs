using System;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.SourceWriters;
using Unity.IL2CPP.SourceWriters.Utils;

namespace Unity.IL2CPP;

public static class SourceWriter
{
	internal static void WriteGenericMethodDefinition(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceMethod method)
	{
		writer.AddIncludeForTypeDefinition(context, method.DeclaringType);
		MethodWriter.WriteMethodDefinition(context.CreateAssemblyWritingContext(method), writer, method);
		WriteReversePInvokeWrappersForMethodIfNecessary(writer, method);
	}

	internal static void WriteTypesMethods(SourceWritingContext context, IGeneratedMethodCodeWriter writer, in TypeWritingInformation writingInformation, NPath filePath, bool writeMarshalingDefinitions)
	{
		TypeDefinition systemArray = context.Global.Services.TypeProvider.SystemArray;
		if (systemArray != null)
		{
			writer.AddIncludeForTypeDefinition(context, systemArray);
		}
		writer.WriteClangWarningDisables();
		TypeReference type = writingInformation.DeclaringType;
		writer.AddIncludeForTypeDefinition(context, type);
		try
		{
			if (writingInformation.WriteTypeLevelInformation)
			{
				if (type.IsDelegate)
				{
					new DelegateMethodsWriter(writer).WriteInvokeStubs(type);
				}
				ReadOnlyCollection<MethodDefinition> methods = writingInformation.DeclaringType.Resolve().Methods;
				if (writeMarshalingDefinitions)
				{
					MarshalingDefinitions.Write(context, writer, type);
				}
				else if (context.Global.Parameters.FullGenericSharingOnly)
				{
					foreach (MethodDefinition item in methods)
					{
						MethodDefinition methodDefinition = item.Resolve();
						context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
						WriteReversePInvokeWrappersForMethodIfNecessary(writer, methodDefinition);
					}
				}
			}
			foreach (MethodReference methodToWrite in writingInformation.MethodsToWrite)
			{
				MethodDefinition method = methodToWrite.Resolve();
				context.Global.Services.ErrorInformation.CurrentMethod = method;
				if (context.Global.Parameters.EnableErrorMessageTest)
				{
					ErrorTypeAndMethod.ThrowIfIsErrorMethod(context, method);
				}
				if (!string.IsNullOrEmpty(context.Global.InputData.AssemblyMethod) && filePath != null && method.FullName.Contains(context.Global.InputData.AssemblyMethod))
				{
					context.Global.Collectors.MatchedAssemblyMethodSourceFiles.Add(filePath);
				}
				MethodWriter.WriteMethodDefinition(context.CreateAssemblyWritingContext(method), writer, methodToWrite);
			}
		}
		catch (Exception)
		{
			writer.ErrorOccurred = true;
			throw;
		}
		writer.WriteClangWarningEnables();
	}

	internal static bool NeedsReversePInvokeWrapper(ReadOnlyContext context, in LazilyInflatedMethod method)
	{
		if (!context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers)
		{
			return ReversePInvokeMethodBodyWriter.IsReversePInvokeWrapperNecessary(context, in method);
		}
		return true;
	}

	private static void WriteReversePInvokeWrappersForMethodIfNecessary(IGeneratedMethodCodeWriter writer, MethodReference method)
	{
		if (writer.Context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers || ReversePInvokeMethodBodyWriter.IsReversePInvokeWrapperNecessary(writer.Context, method))
		{
			ReversePInvokeMethodBodyWriter.WriteReversePInvokeMethodDefinitions(writer, method);
		}
	}

	internal static void WriteTypeDefinition(ReadOnlyContext context, IReadOnlyContextGeneratedCodeWriter writer, TypeReference type, TypeDefinitionWriter.FieldType fieldType, out TypeReference[] typesRequiringInteropGuids)
	{
		typesRequiringInteropGuids = null;
		if (type.IsComOrWindowsRuntimeInterface(context))
		{
			if (fieldType == TypeDefinitionWriter.FieldType.Instance)
			{
				ComInterfaceWriter.WriteComInterfaceFor(context, writer, type, out typesRequiringInteropGuids);
			}
		}
		else
		{
			TypeDefinitionWriter.WriteTypeDefinitionFor(context, type, writer, fieldType, out typesRequiringInteropGuids);
		}
	}
}
