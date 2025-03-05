using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

internal class ProjectedMethodBodyWriter : ComCallableWrapperMethodBodyWriter
{
	private readonly MethodReference _actualManagedMethod;

	protected override string ManagedObjectExpression => "__this";

	public ProjectedMethodBodyWriter(ReadOnlyContext context, MethodReference managedMethod, MethodReference nativeInterfaceMethod)
		: base(context, nativeInterfaceMethod, nativeInterfaceMethod, MarshalType.WindowsRuntime)
	{
		_actualManagedMethod = managedMethod;
	}

	protected override void WriteMethodCallStatement(IRuntimeMetadataAccess metadataAccess, string thisArgument, string[] localVariableNames, IGeneratedMethodCodeWriter writer, string returnVariable = null)
	{
		MethodCallType methodCallType = (_actualManagedMethod.DeclaringType.IsInterface ? MethodCallType.Virtual : MethodCallType.Normal);
		if (_actualManagedMethod.DeclaringType.IsValueType)
		{
			thisArgument = $"({_actualManagedMethod.DeclaringType.CppName}*)UnBox({thisArgument}, {metadataAccess.TypeInfoFor(_actualManagedMethod.DeclaringType)})";
		}
		string actualReturnVariable = returnVariable;
		TypeReference actualReturnType = _context.Global.Services.TypeFactory.ResolverFor(_actualManagedMethod.DeclaringType, _actualManagedMethod).ResolveReturnType(_actualManagedMethod);
		TypeReference returnType = _context.Global.Services.TypeFactory.ResolverFor(_managedMethod.DeclaringType, _managedMethod).ResolveReturnType(_managedMethod);
		bool areSame = actualReturnType == returnType;
		if (writer.Context.Global.Parameters.EmitComments)
		{
			writer.WriteCommentedLine(_actualManagedMethod.FullName);
		}
		if (!areSame)
		{
			actualReturnVariable = returnVariable + "ExactType";
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteStatement($"{actualReturnType.CppNameForVariable} {actualReturnVariable}");
		}
		WriteMethodCallStatementWithResult(metadataAccess, thisArgument, _actualManagedMethod, methodCallType, writer, actualReturnVariable, localVariableNames);
		if (!string.IsNullOrEmpty(returnVariable) && !areSame)
		{
			IGeneratedMethodCodeWriter generatedMethodCodeWriter = writer;
			generatedMethodCodeWriter.WriteStatement($"{returnVariable} = {actualReturnVariable}");
		}
	}
}
