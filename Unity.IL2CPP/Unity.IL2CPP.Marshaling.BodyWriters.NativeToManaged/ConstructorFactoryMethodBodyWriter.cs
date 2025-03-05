using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

internal class ConstructorFactoryMethodBodyWriter : ComCallableWrapperMethodBodyWriter
{
	public ConstructorFactoryMethodBodyWriter(ReadOnlyContext context, MethodDefinition constructor, MethodReference nativeInterfaceMethod)
		: base(context, constructor, nativeInterfaceMethod, MarshalType.WindowsRuntime)
	{
	}

	protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
	{
		TypeReference type = _managedMethod.DeclaringType;
		INamingService naming = writer.Context.Global.Services.Naming;
		string thisVariableName = "managedInstance";
		IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteLine($"{type.CppNameForVariable} {thisVariableName} = {Emit.NewObj(writer.Context, type, metadataAccess)};");
		writer.WriteMethodCallStatement(metadataAccess, thisVariableName, _managedMethod, _managedMethod, MethodCallType.Normal, localVariableNames);
		generatedMethodCodeWriter = writer;
		generatedMethodCodeWriter.WriteStatement($"{naming.ForInteropReturnValue()} = {thisVariableName}");
	}
}
