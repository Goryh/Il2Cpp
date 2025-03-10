using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic.Populaters;

internal class MethodBodyPopulator
{
	private static readonly ReadOnlyCollection<VariableDefinition> EmptyLocalVariables = Array.Empty<VariableDefinition>().AsReadOnly();

	private static readonly ReadOnlyCollection<ExceptionHandler> EmptyExceptionHandlers = Array.Empty<ExceptionHandler>().AsReadOnly();

	public static void PopulateMethodBody(CecilSourcedAssemblyData assemblyData, MethodDefinition method, Mono.Cecil.MethodDefinition source)
	{
		if (source.HasBody && source.RVA != 0 && !method.Module.Assembly.LoadedForExportsOnly)
		{
			ReadOnlyCollection<VariableDefinition> localVariables = CreateLocalVariables(assemblyData, method, source);
			Dictionary<Mono.Cecil.Cil.Instruction, Instruction> instructionMap;
			List<Instruction> instructions = CreateInstructions(assemblyData, method, source, localVariables, out instructionMap);
			ParameterDefinition thisParam = null;
			if (source.Body.ThisParameter != null)
			{
				thisParam = new ParameterDefinition(source.Body.ThisParameter, ReadOnlyCollectionCache<CustomAttribute>.Empty, null);
				thisParam.InitializeParameterType(assemblyData.ResolveReference(GenericParameterResolver.ResolveParameterTypeIfNeeded((Mono.Cecil.MethodReference)source, (ParameterReference)source.Body.ThisParameter)));
			}
			method.InitializeMethodBody(new MethodBody(method, thisParam, source.Body.InitLocals, source.Body.CodeSize, localVariables, instructions, CreateExceptionHandlers(assemblyData, method, source, instructionMap)));
		}
	}

	private static List<Instruction> CreateInstructions(CecilSourcedAssemblyData assemblyDef, MethodDefinition method, Mono.Cecil.MethodDefinition source, ReadOnlyCollection<VariableDefinition> variables, out Dictionary<Mono.Cecil.Cil.Instruction, Instruction> instructionMap)
	{
		List<Instruction> instructionDefs = new List<Instruction>(source.Body.Instructions.Count);
		instructionMap = new Dictionary<Mono.Cecil.Cil.Instruction, Instruction>(source.Body.Instructions.Count);
		foreach (Mono.Cecil.Cil.Instruction instruction in source.Body.Instructions)
		{
			Instruction instructionDef = new Instruction(instruction);
			object operand = instruction.Operand;
			if (!(operand is Mono.Cecil.TypeReference typeReference))
			{
				if (!(operand is Mono.Cecil.FieldReference fieldReference))
				{
					if (!(operand is Mono.Cecil.MethodReference methodReference))
					{
						if (!(operand is VariableReference variableReference))
						{
							if (!(operand is ParameterReference parameterReference))
							{
								if (operand is Mono.Cecil.CallSite callSite)
								{
									instructionDef.InitializeOperandDef(new CallSite(callSite, assemblyDef.ResolveReference(callSite.ReturnType), ParameterDefBuilder.BuildInitializedParameters(callSite, assemblyDef, (CecilSourcedAssemblyData asm, Mono.Cecil.TypeReference type) => asm.ResolveReference(type))));
								}
							}
							else if (parameterReference.Index == -1)
							{
								instructionDef.InitializeOperandDef(ParameterDefinition.MakeThisParameter(assemblyDef.ResolveReference(parameterReference.ParameterType), MetadataToken.ParamZero));
							}
							else
							{
								instructionDef.InitializeOperandDef(method.Parameters[parameterReference.Index]);
							}
						}
						else
						{
							instructionDef.InitializeOperandDef(variables[variableReference.Index]);
						}
					}
					else
					{
						instructionDef.InitializeOperandDef(assemblyDef.ResolveReference(methodReference));
					}
				}
				else
				{
					instructionDef.InitializeOperandDef(assemblyDef.ResolveReference(fieldReference));
				}
			}
			else
			{
				instructionDef.InitializeOperandDef(assemblyDef.ResolveReference(typeReference));
			}
			instructionDefs.Add(instructionDef);
			instructionMap.Add(instruction, instructionDef);
		}
		Dictionary<Mono.Cecil.Cil.Instruction, Instruction> internalMap = instructionMap;
		foreach (Mono.Cecil.Cil.Instruction instruction2 in source.Body.Instructions)
		{
			Instruction instructionDef2 = instructionMap[instruction2];
			object operand = instruction2.Operand;
			if (!(operand is Mono.Cecil.Cil.Instruction instructionOperand))
			{
				if (operand is Mono.Cecil.Cil.Instruction[] instructionList)
				{
					instructionDef2.InitializeOperandDef(instructionList.Select((Mono.Cecil.Cil.Instruction i) => internalMap[i]).ToArray());
				}
			}
			else
			{
				instructionDef2.InitializeOperandDef(instructionMap[instructionOperand]);
			}
			instructionDef2.InitializeNextAndPrevious((instruction2.Next != null) ? instructionMap[instruction2.Next] : null, (instruction2.Previous != null) ? instructionMap[instruction2.Previous] : null);
		}
		return instructionDefs;
	}

	private static ReadOnlyCollection<VariableDefinition> CreateLocalVariables(CecilSourcedAssemblyData assemblyDef, MethodDefinition methodDef, Mono.Cecil.MethodDefinition source)
	{
		if (!source.Body.HasVariables)
		{
			return EmptyLocalVariables;
		}
		List<VariableDefinition> locals = new List<VariableDefinition>(source.Body.Variables.Count);
		foreach (Mono.Cecil.Cil.VariableDefinition variable in source.Body.Variables)
		{
			source.DebugInformation.TryGetName(variable, out var debugName);
			locals.Add(new VariableDefinition(assemblyDef.ResolveReference(variable.VariableType), variable, debugName));
		}
		return locals.AsReadOnly();
	}

	private static ReadOnlyCollection<ExceptionHandler> CreateExceptionHandlers(CecilSourcedAssemblyData assemblyDef, MethodDefinition methodDef, Mono.Cecil.MethodDefinition source, Dictionary<Mono.Cecil.Cil.Instruction, Instruction> instructionDefMap)
	{
		if (!source.Body.HasExceptionHandlers)
		{
			return EmptyExceptionHandlers;
		}
		List<ExceptionHandler> exceptionHandlers = new List<ExceptionHandler>(source.Body.ExceptionHandlers.Count);
		foreach (Mono.Cecil.Cil.ExceptionHandler exceptionHandler in source.Body.ExceptionHandlers)
		{
			exceptionHandlers.Add(new ExceptionHandler((exceptionHandler.CatchType == null) ? null : assemblyDef.ResolveReference(exceptionHandler.CatchType), (ExceptionHandlerType)exceptionHandler.HandlerType, Lookup(instructionDefMap, exceptionHandler.TryStart), Lookup(instructionDefMap, exceptionHandler.TryEnd), Lookup(instructionDefMap, exceptionHandler.FilterStart), Lookup(instructionDefMap, exceptionHandler.HandlerStart), Lookup(instructionDefMap, exceptionHandler.HandlerEnd)));
		}
		return exceptionHandlers.AsReadOnly();
	}

	private static Instruction Lookup(Dictionary<Mono.Cecil.Cil.Instruction, Instruction> map, Mono.Cecil.Cil.Instruction instruction)
	{
		if (instruction == null)
		{
			return null;
		}
		if (!map.ContainsKey(instruction))
		{
			throw new InvalidOperationException();
		}
		return map[instruction];
	}
}
