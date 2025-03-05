using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.SourceWriters.Utils;

internal static class MarshalingDefinitions
{
	public class TypeWritingInput
	{
		public readonly TypeReference Type;

		public readonly TypeDefinition Definition;

		public readonly ReadOnlyCollection<MethodWritingInput> MethodsToWrite;

		public readonly ReadOnlyCollection<DefaultMarshalInfoWriter> MarshalInfoWriters;

		public TypeWritingInput(TypeReference type, TypeDefinition definition, ReadOnlyCollection<MethodWritingInput> methodsToWrite, ReadOnlyCollection<DefaultMarshalInfoWriter> marshalInfoWriters)
		{
			Type = type;
			Definition = definition;
			MethodsToWrite = methodsToWrite;
			MarshalInfoWriters = marshalInfoWriters;
		}
	}

	public class MethodWritingInput
	{
		public readonly MethodDefinition Definition;

		public readonly DelegatePInvokeMethodBodyWriter DelegatePInvokeMethodBodyWriter;

		public readonly bool NeedsReversePInvoke;

		public bool NeedsDelegatePInvoke => DelegatePInvokeMethodBodyWriter != null;

		public MethodWritingInput(MethodDefinition definition, DelegatePInvokeMethodBodyWriter delegatePInvokeMethodBodyWriter, bool needsReversePInvoke)
		{
			Definition = definition;
			DelegatePInvokeMethodBodyWriter = delegatePInvokeMethodBodyWriter;
			NeedsReversePInvoke = needsReversePInvoke;
		}
	}

	public static void Write(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference item)
	{
		TypeWritingInput writingInput = Collect(context, item);
		if (writingInput != null)
		{
			Write(context, writer, writingInput);
		}
	}

	public static void Write(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeWritingInput item)
	{
		context.Global.Services.ErrorInformation.CurrentType = item.Definition;
		foreach (DefaultMarshalInfoWriter marshalInfoWriter in item.MarshalInfoWriters)
		{
			marshalInfoWriter.WriteMarshalFunctionDefinitions(writer);
		}
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(item.Type);
		foreach (MethodWritingInput methodWritingInput in item.MethodsToWrite)
		{
			MethodReference method = typeResolver.Resolve(methodWritingInput.Definition);
			context.Global.Services.ErrorInformation.CurrentMethod = methodWritingInput.Definition;
			if (methodWritingInput.NeedsDelegatePInvoke)
			{
				MethodWriter.WriteMethodForDelegatePInvoke(context, writer, method, methodWritingInput.DelegatePInvokeMethodBodyWriter);
			}
			if (methodWritingInput.NeedsReversePInvoke)
			{
				ReversePInvokeMethodBodyWriter.WriteReversePInvokeMethodDefinitions(writer, method);
			}
		}
	}

	public static TypeWritingInput Collect(ReadOnlyContext context, TypeReference type)
	{
		ReadOnlyCollection<DefaultMarshalInfoWriter> marshalInfoWriters = CollectDefaultMarshalInfoWriters(context, type);
		ReadOnlyCollection<MethodWritingInput> methodsToWrite = CollectMethodsToWrite(context, type);
		if (marshalInfoWriters.Count == 0 && methodsToWrite.Count == 0)
		{
			return null;
		}
		return new TypeWritingInput(type, type.Resolve(), methodsToWrite, marshalInfoWriters);
	}

	public static ReadOnlyCollection<TypeWritingInput> Collect(ReadOnlyContext context, IEnumerable<GenericInstanceType> items)
	{
		return Collect(context, items.Cast<TypeReference>());
	}

	public static ReadOnlyCollection<TypeWritingInput> Collect(ReadOnlyContext context, IEnumerable<TypeReference> items)
	{
		List<TypeWritingInput> typesToWrite = new List<TypeWritingInput>();
		foreach (TypeReference type in items)
		{
			TypeWritingInput writingInput = Collect(context, type);
			if (writingInput != null)
			{
				typesToWrite.Add(writingInput);
			}
		}
		return typesToWrite.AsReadOnly();
	}

	private static ReadOnlyCollection<MethodWritingInput> CollectMethodsToWrite(ReadOnlyContext context, TypeReference type)
	{
		List<MethodWritingInput> methodsToWrite = new List<MethodWritingInput>();
		TypeDefinition typeDefinition = type.Resolve();
		bool couldDelegatePInvokeMethodBodyWriterBeNeeded = typeDefinition.IsDelegate && !typeDefinition.HasGenericParameters;
		bool couldReversePInvokeWrappersBeNeeded = context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers || ReversePInvokeMethodBodyWriter.CouldReversePInvokeWrapperBeNeeded(typeDefinition);
		if (!couldDelegatePInvokeMethodBodyWriterBeNeeded && !couldReversePInvokeWrappersBeNeeded)
		{
			return methodsToWrite.AsReadOnly();
		}
		foreach (LazilyInflatedMethod item in type.IterateLazilyInflatedMethods(context))
		{
			LazilyInflatedMethod method = item;
			DelegatePInvokeMethodBodyWriter delegatePInvokeMethodBodyWriter = ((!couldDelegatePInvokeMethodBodyWriterBeNeeded) ? null : GetDelegatePInvokeMethodBodyWriterIfNecessary(context, in method));
			bool needsReservePInvoke = couldReversePInvokeWrappersBeNeeded && SourceWriter.NeedsReversePInvokeWrapper(context, in method);
			if (delegatePInvokeMethodBodyWriter != null || needsReservePInvoke)
			{
				methodsToWrite.Add(new MethodWritingInput(method.Definition, delegatePInvokeMethodBodyWriter, needsReservePInvoke));
			}
		}
		return methodsToWrite.AsReadOnly();
	}

	private static ReadOnlyCollection<DefaultMarshalInfoWriter> CollectDefaultMarshalInfoWriters(ReadOnlyContext context, TypeReference type)
	{
		List<DefaultMarshalInfoWriter> marshalInfoWriters = new List<DefaultMarshalInfoWriter>();
		MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(context, type);
		foreach (MarshalType marshalType in marshalTypesForMarshaledType)
		{
			DefaultMarshalInfoWriter marshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, null, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(type));
			if (marshalInfoWriter.WillWriteMarshalFunctionDefinitions())
			{
				marshalInfoWriters.Add(marshalInfoWriter);
			}
		}
		return marshalInfoWriters.AsReadOnly();
	}

	private static DelegatePInvokeMethodBodyWriter GetDelegatePInvokeMethodBodyWriterIfNecessary(ReadOnlyContext context, in LazilyInflatedMethod method)
	{
		if (!DelegatePInvokeMethodBodyWriter.IsDelegatePInvokeWrapperNecessaryQuickChecks(context, in method))
		{
			return null;
		}
		if (method.DeclaringType.IsGenericInstance)
		{
			return null;
		}
		if (method.InflatedMethod.HasGenericParameters || method.InflatedMethod.IsGenericInstance)
		{
			return null;
		}
		if (DelegatePInvokeMethodBodyWriter.IsDelegatePInvokeWrapperNecessaryFinalCheck(context, method.InflatedMethod, out var possibleDelegatePInvokeMethodBodyWriter))
		{
			return possibleDelegatePInvokeMethodBodyWriter;
		}
		return null;
	}
}
