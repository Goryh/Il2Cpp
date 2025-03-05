using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;

namespace Unity.IL2CPP.MethodWriting;

[DebuggerDisplay("{_methodDefinition.FullName")]
public class ResolvedMethodContext
{
	private readonly ResolvedTypeFactory _resolvedTypeFactory;

	private readonly MethodDefinition _methodDefinition;

	public readonly ReadOnlyCollection<ResolvedVariable> LocalVariables;

	public readonly ReadOnlyCollection<ResolvedParameter> Parameters;

	public readonly ReadOnlyCollection<ResolvedInstructionBlock> Blocks;

	private ResolvedMethodContext(ResolvedTypeFactory resolvedTypeFactory, ReadOnlyCollection<ResolvedVariable> localVariables, ReadOnlyCollection<ResolvedParameter> parameters, ReadOnlyCollection<InstructionBlock> blocks, MethodDefinition methodDefinition)
	{
		_resolvedTypeFactory = resolvedTypeFactory;
		_methodDefinition = methodDefinition;
		LocalVariables = localVariables;
		Parameters = parameters;
		Blocks = blocks.Select(ResolveBlock).ToList().AsReadOnly();
	}

	private ResolvedInstructionBlock ResolveBlock(InstructionBlock block)
	{
		List<ResolvedInstruction> resolvedList = new List<ResolvedInstruction>();
		ResolvedInstruction next = null;
		for (Instruction ins = block.Last; ins != null; ins = ins.Previous)
		{
			ResolvedInstruction resolved = CreateInstruction(ins, next);
			resolvedList.Add(resolved);
			next = resolved;
			if (ins == block.First)
			{
				break;
			}
		}
		resolvedList.Reverse();
		return new ResolvedInstructionBlock(block, resolvedList.AsReadOnly());
	}

	private ResolvedInstruction CreateInstruction(Instruction ins, ResolvedInstruction next)
	{
		if (ins.OpCode == OpCodes.Ldtoken)
		{
			return new ResolvedInstruction(ins, next);
		}
		object operand = ins.Operand;
		if (!(operand is TypeReference t))
		{
			if (!(operand is FieldReference f))
			{
				if (!(operand is MethodReference m))
				{
					if (!(operand is CallSite c))
					{
						if (!(operand is VariableDefinition v))
						{
							if (operand is ParameterDefinition p)
							{
								return new ParameterInfoResolvedInstruction(Parameters[p.Index + (_methodDefinition.HasThis ? 1 : 0)], ins, next);
							}
							switch (ins.OpCode.Code)
							{
							case Code.Ldarg_0:
								return new ParameterInfoResolvedInstruction(Parameters[0], ins, next);
							case Code.Ldarg_1:
								return new ParameterInfoResolvedInstruction(Parameters[1], ins, next);
							case Code.Ldarg_2:
								return new ParameterInfoResolvedInstruction(Parameters[2], ins, next);
							case Code.Ldarg_3:
								return new ParameterInfoResolvedInstruction(Parameters[3], ins, next);
							case Code.Ldloc_0:
							case Code.Stloc_0:
								return new VariableInfoResolvedInstruction(LocalVariables[0], ins, next);
							case Code.Ldloc_1:
							case Code.Stloc_1:
								return new VariableInfoResolvedInstruction(LocalVariables[1], ins, next);
							case Code.Ldloc_2:
							case Code.Stloc_2:
								return new VariableInfoResolvedInstruction(LocalVariables[2], ins, next);
							case Code.Ldloc_3:
							case Code.Stloc_3:
								return new VariableInfoResolvedInstruction(LocalVariables[3], ins, next);
							default:
								return new ResolvedInstruction(ins, next);
							}
						}
						return new VariableInfoResolvedInstruction(LocalVariables[v.Index], ins, next);
					}
					return new CallSiteInfoResolvedInstruction(_resolvedTypeFactory.Create(c), ins, next);
				}
				return new MethodInfoResolvedInstruction(_resolvedTypeFactory.Create(m), ins, next);
			}
			return new FieldInfoResolvedInstruction(_resolvedTypeFactory.Create(f), ins, next);
		}
		return new TypeInfoResolvedInstruction(_resolvedTypeFactory.Create(t), ins, next);
	}

	public static ResolvedMethodContext Create(ResolvedTypeFactory resolvedTypeFactory, MethodDefinition methodDefinition, MethodReference methodReference, ReadOnlyCollection<InstructionBlock> blocks)
	{
		return new ResolvedMethodContext(resolvedTypeFactory, ResolveVariables(resolvedTypeFactory, methodDefinition, methodReference), ResolveParameters(resolvedTypeFactory, methodDefinition, methodReference), blocks, methodDefinition);
	}

	private static ReadOnlyCollection<ResolvedVariable> ResolveVariables(ResolvedTypeFactory resolvedTypeFactory, MethodDefinition methodDefinition, MethodReference methodReference)
	{
		if (!methodDefinition.HasBody || !methodDefinition.Body.HasVariables)
		{
			return Array.Empty<ResolvedVariable>().AsReadOnly();
		}
		return methodDefinition.Body.Variables.Select((VariableDefinition v) => resolvedTypeFactory.Create(v, methodReference)).ToArray().AsReadOnly();
	}

	private static ReadOnlyCollection<ResolvedParameter> ResolveParameters(ResolvedTypeFactory resolvedTypeFactory, MethodDefinition methodDefinition, MethodReference methodReference)
	{
		IEnumerable<ResolvedParameter> enumerable2;
		if (!methodDefinition.HasParameters)
		{
			IEnumerable<ResolvedParameter> enumerable = Array.Empty<ResolvedParameter>();
			enumerable2 = enumerable;
		}
		else
		{
			enumerable2 = methodDefinition.Parameters.Select((ParameterDefinition p) => resolvedTypeFactory.Create(p, methodDefinition, methodReference));
		}
		IEnumerable<ResolvedParameter> resolvedParameters = enumerable2;
		if (methodDefinition.HasThis)
		{
			ResolvedTypeInfo thisType = resolvedTypeFactory.ResolveThisType(methodDefinition);
			if (thisType.GetRuntimeStorage(resolvedTypeFactory.TypeFactory).IsByValue())
			{
				thisType = thisType.MakeByReferenceType(resolvedTypeFactory.TypeFactory);
			}
			resolvedParameters = new ResolvedParameter[1]
			{
				new ResolvedParameter(-1, "__this", "__this", thisType)
			}.Concat(resolvedParameters);
		}
		return resolvedParameters.ToArray().AsReadOnly();
	}
}
