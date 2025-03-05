using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP;

public readonly struct NullChecksSupport
{
	private readonly IGeneratedMethodCodeWriter _writer;

	private readonly MethodDefinition _methodDefinition;

	private readonly bool _nullChecksGloballyEnabled;

	public NullChecksSupport(IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition)
	{
		_writer = writer;
		_methodDefinition = methodDefinition;
		_nullChecksGloballyEnabled = writer.Context.Global.Parameters.EmitNullChecks;
	}

	public void WriteNullCheckIfNeeded(ReadOnlyContext context, StackInfo stackInfo)
	{
		WriteNullCheckIfNeeded(context, stackInfo.Type.ResolvedType, stackInfo.Expression);
	}

	public void WriteNullCheckIfNeeded(ReadOnlyContext context, TypeReference typeReference, string expression)
	{
		if (ShouldEmitNullChecksForMethod() && !typeReference.GetRuntimeStorage(context).IsByValue() && (!typeReference.IsByReference || !typeReference.GetElementType().GetRuntimeStorage(context).IsByValue()))
		{
			RecordNullCheckEmitted();
			_writer.WriteStatement(Emit.NullCheck(expression));
		}
	}

	public void WriteNullCheckForInvocationIfNeeded(MethodReference methodReference, IList<string> args)
	{
		if (ShouldEmitNullChecksForMethod() && methodReference.HasThis && !methodReference.DeclaringType.IsValueType && !(args[0] == "__this"))
		{
			RecordNullCheckEmitted();
			_writer.WriteStatement(Emit.NullCheck(args[0]));
		}
	}

	private void RecordNullCheckEmitted()
	{
		_writer.Context.Global.Collectors.Stats.RecordNullCheckEmitted(_methodDefinition);
	}

	private bool ShouldEmitNullChecksForMethod()
	{
		return CompilerServicesSupport.HasNullChecksSupportEnabled(_writer.Context, _methodDefinition, _nullChecksGloballyEnabled);
	}
}
