using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Metadata;

public static class VTableBuilderExtensions
{
	public static string IndexForWithComment(this IVTableBuilderService vTableBuilder, ReadOnlyContext context, MethodDefinition method)
	{
		return vTableBuilder.IndexFor(context, method) + (context.Global.Parameters.EmitComments ? (" /* " + method.FullName + " */") : string.Empty);
	}

	public static string IndexForWithComment(this IVTableBuilderService vTableBuilder, ReadOnlyContext context, MethodDefinition method, MethodReference methodForComment)
	{
		return vTableBuilder.IndexFor(context, method) + (context.Global.Parameters.EmitComments ? (" /* " + methodForComment.FullName + " */") : string.Empty);
	}
}
