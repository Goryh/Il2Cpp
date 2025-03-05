using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.SourceWriters.Utils;

public class WritingUtils
{
	public static IEnumerable<TypeWritingInformation> TypeToWritingInformation(ReadOnlyContext readOnlyContext, TypeReference type)
	{
		List<MethodReference> methodsToWrite = new List<MethodReference>();
		foreach (LazilyInflatedMethod methodContext in type.IterateLazilyInflatedMethods(readOnlyContext))
		{
			MethodDefinition definition = methodContext.Definition;
			if (definition.HasGenericParameters)
			{
				continue;
			}
			if (ShouldGenerateIntoOwnFile(definition, out var requested))
			{
				if (!requested)
				{
					readOnlyContext.Global.Services.MessageLogger.LogWarning(definition, "Method was given it's own cpp file because it is large and costly to compile");
				}
				yield return new TypeWritingInformation(type, new MethodReference[1] { methodContext.InflatedMethod }.AsReadOnly(), chunkToOwnFile: true, writeTypeLevelInformation: false);
			}
			else
			{
				methodsToWrite.Add(methodContext.InflatedMethod);
			}
		}
		yield return new TypeWritingInformation(type, methodsToWrite.AsReadOnly(), ShouldGenerateIntoOwnFile(type, methodsToWrite), writeTypeLevelInformation: true);
	}

	public static bool ShouldGenerateIntoOwnFile(MethodReference method)
	{
		bool requested;
		return ShouldGenerateIntoOwnFile(method, out requested);
	}

	public static bool ShouldGenerateIntoOwnFile(MethodReference method, out bool requested)
	{
		if (RequestsOwnFile(method))
		{
			requested = true;
			return true;
		}
		requested = false;
		return IsCostlyToCompile(method);
	}

	private static bool ShouldGenerateIntoOwnFile(TypeReference type, List<MethodReference> methodsToWrite)
	{
		if (!IsCostlyToCompile(methodsToWrite))
		{
			return RequestsOwnFile(type);
		}
		return true;
	}

	private static bool IsCostlyToCompile(MethodReference method)
	{
		return method.CodeSize >= 26000;
	}

	private static bool IsCostlyToCompile(List<MethodReference> methodsToWrite)
	{
		return methodsToWrite.Sum((MethodReference m) => m.Resolve().GetApproximateGeneratedCodeSize()) >= 26000;
	}

	private static bool RequestsOwnFile(MethodReference method)
	{
		return CompilerServicesSupport.HasGenerateIntoOwnCppFile(method.Resolve());
	}

	private static bool RequestsOwnFile(TypeReference type)
	{
		return CompilerServicesSupport.HasGenerateIntoOwnCppFile(type.Resolve());
	}
}
