using System;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.TableWriters;

public class WriteInvokerTable : GeneratedCodeTableWriterBaseChunkedTransformed<InvokerSignature, (string, string)>
{
	protected override string TableName => "Il2CppInvokerTable";

	protected override string CodeTableType => "const InvokerMethod";

	protected override bool ExternTable => true;

	protected override string CodeTableName(GlobalSchedulingContext context)
	{
		return context.Services.ContextScope.ForMetadataGlobalVar("g_Il2CppInvokerPointers");
	}

	public override TableInfo Schedule(IPhaseWorkScheduler<GlobalWriteContext> scheduler)
	{
		return Schedule(scheduler, scheduler.SchedulingContext.Results.SecondaryCollection.Invokers.SortedSignatures, scheduler.SchedulingContext.InputData.JobCount);
	}

	protected override (string, string) Transform(ReadOnlyContext context, InvokerSignature item)
	{
		string name = InvokerCollection.NameForInvoker(context, item);
		using Returnable<StringBuilder> buildContext = context.Global.Services.Factory.CheckoutStringBuilder();
		CodeStringBuilder codeStringBuilder = new CodeStringBuilder(context, buildContext.Value);
		codeStringBuilder.Indent();
		InvokerWriter.WriteInvokerBody(codeStringBuilder, item.HasThis, item.ReducedParameterTypes, item.ReducedParameterTypes[0]);
		string body = buildContext.Value.ToString();
		return (name, body);
	}

	protected override void WriteDeclarations(SourceWritingContext context, IGeneratedCodeStream writer, ReadOnlyCollection<Tuple<InvokerSignature, (string, string)>> allItems)
	{
		writer.AddIncludeOrExternForTypeDefinition(context, context.Global.Services.TypeProvider.SystemObject);
		foreach (Tuple<InvokerSignature, (string, string)> pair in allItems)
		{
			InvokerSignature data = pair.Item1;
			TypeReference returnType = data.ReducedParameterTypes[0];
			writer.AddIncludeOrExternForTypeDefinition(context, returnType.Module.TypeSystem.Object);
			writer.AddIncludeOrExternForTypeDefinition(context, returnType);
			for (int index = 1; index < data.ReducedParameterTypes.Length; index++)
			{
				writer.AddIncludeOrExternForTypeDefinition(context, data.ReducedParameterTypes[index]);
			}
			writer.WriteLine($"void {pair.Item2.Item1} (Il2CppMethodPointer methodPointer, const {"RuntimeMethod"}* methodMetadata, void* obj, void** args, void* returnAddress)");
			writer.WriteLine("{");
			writer.Write(pair.Item2.Item2);
			writer.WriteLine("}");
			writer.WriteLine();
		}
	}

	protected override void WriteItem(SourceWritingContext context, IGeneratedCodeStream writer, Tuple<InvokerSignature, (string, string)> item)
	{
		writer.Write(item.Item2.Item1);
	}
}
