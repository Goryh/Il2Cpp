using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.SourceWriters.Utils;

namespace Unity.IL2CPP.SourceWriters;

internal class GenericComDefinitionSourceWriter : SourceWriterBase<GenericInstanceType, MarshalingDefinitions.TypeWritingInput>
{
	private const string BaseFileName = "Il2CppGenericComDefinitions";

	public override string Name => "WriteGenericComDefinition";

	public override bool RequiresSortingBeforeChunking => false;

	public override FileCategory FileCategory => FileCategory.Metadata;

	private GenericComDefinitionSourceWriter()
	{
	}

	public static void EnqueueWrites(SourceWritingContext context, ICollection<GenericInstanceType> items)
	{
		ScheduledStreamFactory.Create(context, "Il2CppGenericComDefinitions.cpp".ToNPath(), new GenericComDefinitionSourceWriter()).Write(context.Global, items);
	}

	public override IEnumerable<MarshalingDefinitions.TypeWritingInput> CreateAndFilterItemsForWriting(GlobalWriteContext context, ICollection<GenericInstanceType> items)
	{
		return MarshalingDefinitions.Collect(context.GetReadOnlyContext(), items);
	}

	public override ReadOnlyCollection<ReadOnlyCollection<MarshalingDefinitions.TypeWritingInput>> Chunk(ReadOnlyCollection<MarshalingDefinitions.TypeWritingInput> items)
	{
		return items.ChunkByApproximateGeneratedCodeSize((MarshalingDefinitions.TypeWritingInput i) => i.MethodsToWrite.Select((MarshalingDefinitions.MethodWritingInput m) => m.Definition.GetApproximateGeneratedCodeSize()).Sum());
	}

	protected override string ProfilerSectionDetailsForItem(MarshalingDefinitions.TypeWritingInput item)
	{
		return item.Type.Name;
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, MarshalingDefinitions.TypeWritingInput item, NPath filePath)
	{
		context.Global.Services.ErrorInformation.CurrentType = item.Definition;
		foreach (DefaultMarshalInfoWriter marshalInfoWriter in item.MarshalInfoWriters)
		{
			marshalInfoWriter.WriteMarshalFunctionDefinitions(writer);
		}
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(item.Type);
		foreach (MarshalingDefinitions.MethodWritingInput methodWritingInput in item.MethodsToWrite)
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

	protected override void WriteHeader(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteFooter(IGeneratedMethodCodeWriter writer)
	{
	}

	protected override void WriteEnd(IGeneratedMethodCodeStream writer)
	{
	}
}
