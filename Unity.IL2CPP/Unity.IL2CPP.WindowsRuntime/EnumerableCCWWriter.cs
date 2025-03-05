using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class EnumerableCCWWriter : IProjectedComCallableWrapperMethodWriter
{
	private sealed class FirstMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly string _adapterTypeName;

		protected override bool IsReturnValueMarshaled => false;

		public FirstMethodBodyWriter(ReadOnlyContext context, MethodReference getEnumeratorMethod, MethodReference firstMethod, TypeReference iteratorType)
			: base(context, getEnumeratorMethod, firstMethod)
		{
			_adapterTypeName = context.Global.Services.Naming.ForWindowsRuntimeAdapterClass(iteratorType);
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteLine($"{_typeResolver.Resolve(_managedMethod.ReturnType).CppNameForVariable} {writer.Context.Global.Services.Naming.ForInteropReturnValue()};");
			base.WriteInteropCallStatementWithinTryBlock(writer, localVariableNames, metadataAccess);
			string createInstanceCall = Emit.Call(writer.Context, _adapterTypeName + "::__CreateInstance", writer.Context.Global.Services.Naming.ForInteropReturnValue());
			generatedMethodCodeWriter = writer;
			IGeneratedMethodCodeWriter generatedMethodCodeWriter2 = generatedMethodCodeWriter;
			CodeWriterAssignInterpolatedStringHandler left = new CodeWriterAssignInterpolatedStringHandler(1, 1, generatedMethodCodeWriter);
			left.AppendLiteral("*");
			left.AppendFormatted(writer.Context.Global.Services.Naming.ForComInterfaceReturnParameterName());
			generatedMethodCodeWriter2.WriteAssignStatement(ref left, $"({writer.Context.Global.Services.Naming.ForInteropReturnValue()} != NULL) ? {createInstanceCall} : {"NULL"}");
		}
	}

	public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
		if (interfaceType.Resolve().HasGenericParameters)
		{
			GenericEnumeratorToIteratorAdapterWriter.WriteDefinitions(context, writer, (GenericInstanceType)interfaceType);
		}
		else
		{
			EnumeratorToBindableIteratorAdapterWriter.WriteDefinitions(context, writer);
		}
	}

	public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
	{
		TypeReference iiterableType = method.DeclaringType;
		TypeResolver typeResolver = context.Global.Services.TypeFactory.ResolverFor(iiterableType);
		TypeReference ienumerableType = context.Global.Services.WindowsRuntime.ProjectToCLR(iiterableType);
		TypeResolver typeResolver2 = context.Global.Services.TypeFactory.ResolverFor(ienumerableType);
		MethodDefinition getEnumeratorMethodDef = ienumerableType.Resolve().Methods.First((MethodDefinition m) => m.Name == "GetEnumerator");
		MethodReference getEnumeratorMethod = typeResolver2.Resolve(getEnumeratorMethodDef);
		TypeReference returnType = typeResolver.Resolve(method.ReturnType);
		return new FirstMethodBodyWriter(context, getEnumeratorMethod, method, returnType);
	}
}
