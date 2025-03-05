using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal class BenchmarkSupport
{
	private const string BenchmarkAttributeName = "BenchmarkAttribute";

	private static bool IsBenchmarkAttribute(CustomAttribute attribute)
	{
		return attribute.AttributeType.Name == "BenchmarkAttribute";
	}

	private static bool IsBenchmarkMethod(IMemberDefinition methodDefinition)
	{
		if (methodDefinition == null)
		{
			return false;
		}
		foreach (CustomAttribute customAttribute in methodDefinition.CustomAttributes)
		{
			if (IsBenchmarkAttribute(customAttribute))
			{
				return true;
			}
		}
		return false;
	}

	public static bool BeginBenchmark(MethodReference method, IGeneratedMethodCodeWriter writer)
	{
		if (writer.Context.Global.Parameters.GoogleBenchmark && method != null && IsBenchmarkMethod(method.Resolve()))
		{
			writer.AddStdInclude("benchmark/benchmark.h");
			writer.WriteLine($"benchmark::RegisterBenchmark(\"{method.DeclaringType.Name}.{method.Name}\", [=](benchmark::State & state) {{");
			writer.Write("for (auto _ : state) ");
			return true;
		}
		return false;
	}

	public static void EndBenchmark(bool benchmarkMethod, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess runtimeMetadataAccess, MethodReference method, string thisExpression)
	{
		if (!benchmarkMethod)
		{
			return;
		}
		writer.Write("})");
		MethodReference setupMethod = GetBenchmarkConfigMethod(writer.Context, method, "BenchmarkSetupAttribute");
		if (setupMethod != null)
		{
			WriteBenchmarkConfigMethodCall(writer, setupMethod, runtimeMetadataAccess.MethodMetadataFor(setupMethod), "Setup", thisExpression);
		}
		MethodReference teardownMethod = GetBenchmarkConfigMethod(writer.Context, method, "BenchmarkTeardownAttribute");
		if (teardownMethod != null)
		{
			WriteBenchmarkConfigMethodCall(writer, teardownMethod, runtimeMetadataAccess.MethodMetadataFor(setupMethod), "Teardown", thisExpression);
		}
		CustomAttribute benchmarkAttribute = method.Resolve().CustomAttributes.Single(IsBenchmarkAttribute);
		if (benchmarkAttribute.Properties.SingleOrDefault((CustomAttributeNamedArgument p) => p.Name == "MultiThreaded")?.Argument.Value as bool? == true)
		{
			int maxThreads = (benchmarkAttribute.Properties.SingleOrDefault((CustomAttributeNamedArgument p) => p.Name == "MaxThreads")?.Argument.Value as int?).GetValueOrDefault();
			if (maxThreads <= 0)
			{
				maxThreads = Math.Max(Environment.ProcessorCount / 2, 2);
			}
			writer.Write($"->Threads({maxThreads})");
		}
		writer.WriteLine(";");
	}

	private static void WriteBenchmarkConfigMethodCall(IGeneratedMethodCodeWriter writer, MethodReference methodToCall, IMethodMetadataAccess metadataAccess, string setupMethodName, string thisExpression)
	{
		SourceWritingContext context = writer.Context;
		writer.Write($"->{setupMethodName}([=](benchmark::State& state) {{ ");
		MethodBodyWriter.WriteMethodCallExpression(null, writer, methodToCall, methodToCall, context.Global.Services.TypeFactory.EmptyResolver(), MethodCallType.Normal, metadataAccess, context.Global.Services.VTable, new string[1] { thisExpression }, useArrayBoundsCheck: false);
		writer.WriteLine(" })");
	}

	private static MethodReference GetBenchmarkConfigMethod(ReadOnlyContext context, MethodReference benchMarkMethod, string attributeName)
	{
		foreach (MethodReference method in benchMarkMethod.DeclaringType.GetMethods(context))
		{
			foreach (CustomAttribute item in method.Resolve().CustomAttributes.Where((CustomAttribute ca) => ca.Constructor.DeclaringType.Namespace == "Unity.IL2CPP.Benchmarking.GBenchmarks" && ca.Constructor.DeclaringType.Name == attributeName))
			{
				if (item.ConstructorArguments[0].Value as string == benchMarkMethod.Name)
				{
					if (method.HasParameters)
					{
						throw new InvalidOperationException("Methods with a " + attributeName + " must not take any parameters");
					}
					if (method.IsStatic)
					{
						throw new InvalidOperationException("Methods with a " + attributeName + " must be instance methods");
					}
					return method;
				}
			}
		}
		return null;
	}
}
