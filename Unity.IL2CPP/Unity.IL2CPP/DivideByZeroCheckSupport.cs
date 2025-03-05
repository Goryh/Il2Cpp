using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public struct DivideByZeroCheckSupport
{
	private readonly IGeneratedMethodCodeWriter _writer;

	private readonly MethodDefinition _methodDefinition;

	private readonly bool _divideByZeroChecksGloballyEnabled;

	public DivideByZeroCheckSupport(IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition, bool divideByZeroChecksGloballyEnabled)
	{
		_writer = writer;
		_methodDefinition = methodDefinition;
		_divideByZeroChecksGloballyEnabled = divideByZeroChecksGloballyEnabled;
	}

	public void WriteDivideByZeroCheckIfNeeded(StackInfo stackInfo)
	{
		if (ShouldEmitDivideByZeroChecksForMethod())
		{
			RecordDivideByZeroCheckEmitted();
			string divideByZeroCheck = Emit.DivideByZeroCheck(stackInfo.Type.ResolvedType, stackInfo.Expression);
			if (!string.IsNullOrEmpty(divideByZeroCheck))
			{
				_writer.WriteStatement(divideByZeroCheck);
			}
		}
	}

	private void RecordDivideByZeroCheckEmitted()
	{
		_writer.Context.Global.Collectors.Stats.RecordDivideByZeroCheckEmitted(_methodDefinition);
	}

	private bool ShouldEmitDivideByZeroChecksForMethod()
	{
		return CompilerServicesSupport.HasDivideByZeroChecksSupportEnabled(_methodDefinition, _divideByZeroChecksGloballyEnabled);
	}
}
