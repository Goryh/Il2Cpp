using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel;

public class MethodBody
{
	private readonly List<Instruction> _instructions;

	public ParameterDefinition ThisParameter { get; }

	public MethodDefinition Method { get; }

	public bool InitLocals { get; }

	public int CodeSize { get; private set; }

	public ReadOnlyCollection<VariableDefinition> Variables { get; private set; }

	public ReadOnlyCollection<Instruction> Instructions => _instructions.AsReadOnly();

	public ReadOnlyCollection<ExceptionHandler> ExceptionHandlers { get; private set; }

	public bool HasVariables => Variables.Count > 0;

	public bool HasExceptionHandlers => ExceptionHandlers.Count > 0;

	internal MethodBody(MethodDefinition method, ParameterDefinition thisParameter, bool initLocals, int codeSize, ReadOnlyCollection<VariableDefinition> variables, List<Instruction> instructions, ReadOnlyCollection<ExceptionHandler> exceptionHandlers)
	{
		Method = method;
		ThisParameter = thisParameter;
		InitLocals = initLocals;
		CodeSize = codeSize;
		Variables = variables;
		_instructions = instructions;
		ExceptionHandlers = exceptionHandlers;
	}

	public ILProcessor GetILProcessor()
	{
		Method.DeclaringType.Context.AssertDefinitionsAreNotFrozen();
		return new ILProcessor(this, _instructions);
	}

	public void OptimizeMacros()
	{
		Method.DeclaringType.Context.AssertDefinitionsAreNotFrozen();
		ComputeOffsets();
	}

	private void ComputeOffsets()
	{
		int offset = 0;
		foreach (Instruction instruction in Instructions)
		{
			instruction.Offset = offset;
			offset += instruction.GetSize();
		}
		CodeSize = offset;
	}

	internal VariableDefinition AddVariable(TypeReference variableType)
	{
		VariableDefinition variable = new VariableDefinition(variableType, isPinned: false, Variables.Count, string.Empty);
		Variables = Variables.Append(variable).ToArray().AsReadOnly();
		return variable;
	}

	internal ExceptionHandler AddExceptionHandler(TypeReference catchType, ExceptionHandlerType handlerType, Instruction tryStart, Instruction tryEnd, Instruction filterStart, Instruction handlerStart, Instruction handlerEnd)
	{
		ExceptionHandler exceptionHandler = new ExceptionHandler(catchType, handlerType, tryStart, tryEnd, filterStart, handlerStart, handlerEnd);
		ExceptionHandlers = ExceptionHandlers.Append(exceptionHandler).ToArray().AsReadOnly();
		return exceptionHandler;
	}
}
