using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime;

internal static class ProjectedComCallableWrapperMethodWriterDriver
{
	private sealed class NotSupportedMethodBodyWriter : ComCallableWrapperMethodBodyWriter
	{
		public NotSupportedMethodBodyWriter(ReadOnlyContext context, MethodReference method)
			: base(context, method, method, MarshalType.WindowsRuntime)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string exceptionMessage = "Cannot call method '" + InteropMethod.FullName + "' from native code. IL2CPP does not yet support calling this projected method.";
			writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_not_supported_exception(\"" + exceptionMessage + "\")"));
		}
	}

	public static void WriteFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(interfaceType);
		TypeDefinition interfaceTypeDef = interfaceType.Resolve();
		IProjectedComCallableWrapperMethodWriter methodWriter = context.Global.Services.WindowsRuntime.GetProjectedComCallableWrapperMethodWriterFor(interfaceTypeDef);
		methodWriter?.WriteDependenciesFor(context, writer, interfaceType);
		writer.AddIncludeForTypeDefinition(context, interfaceType);
		foreach (MethodDefinition methodDef in interfaceTypeDef.Methods)
		{
			MethodReference method = typeResolver.Resolve(methodDef);
			ComCallableWrapperMethodBodyWriter methodBodyWriter = methodWriter?.GetBodyWriter(context, method) ?? new NotSupportedMethodBodyWriter(writer.Context, method);
			if (writer.Context.Global.Parameters.EmitComments)
			{
				writer.WriteCommentedLine("Projected COM callable wrapper method for " + method.FullName);
			}
			string signature = MethodSignatureWriter.FormatProjectedComCallableWrapperMethodDeclaration(context, method, typeResolver, MarshalType.WindowsRuntime);
			string methodName = context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(method);
			writer.WriteMethodWithMetadataInitialization(signature, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				methodBodyWriter.WriteMethodBody(bodyWriter, metadataAccess);
			}, methodName, method);
		}
	}
}
