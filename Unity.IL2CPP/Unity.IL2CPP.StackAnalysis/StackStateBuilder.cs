using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.StackAnalysis;

public class StackStateBuilder
{
	private readonly ReadOnlyContext _context;

	private readonly MethodDefinition _methodDefinition;

	private readonly ResolvedTypeFactory _typeFactory;

	private readonly ResolvedMethodContext _resolvedMethodInfo;

	private readonly Stack<Entry> _simulationStack;

	private ResolvedTypeInfo Int32TypeReference => _context.Global.Services.TypeProvider.Resolved.Int32TypeReference;

	private ResolvedTypeInfo UInt32TypeReference => _context.Global.Services.TypeProvider.Resolved.UInt32TypeReference;

	private ResolvedTypeInfo SByteTypeReference => _context.Global.Services.TypeProvider.Resolved.SByteTypeReference;

	private ResolvedTypeInfo IntPtrTypeReference => _context.Global.Services.TypeProvider.Resolved.IntPtrTypeReference;

	private ResolvedTypeInfo UIntPtrTypeReference => _context.Global.Services.TypeProvider.Resolved.UIntPtrTypeReference;

	private ResolvedTypeInfo Int64TypeReference => _context.Global.Services.TypeProvider.Resolved.Int64TypeReference;

	private ResolvedTypeInfo SingleTypeReference => _context.Global.Services.TypeProvider.Resolved.SingleTypeReference;

	private ResolvedTypeInfo DoubleTypeReference => _context.Global.Services.TypeProvider.Resolved.DoubleTypeReference;

	private ResolvedTypeInfo ObjectTypeReference => _context.Global.Services.TypeProvider.Resolved.ObjectTypeReference;

	private ResolvedTypeInfo StringTypeReference => _context.Global.Services.TypeProvider.Resolved.StringTypeReference;

	private ResolvedTypeInfo SystemIntPtr => _context.Global.Services.TypeProvider.Resolved.SystemIntPtr;

	private ResolvedTypeInfo SystemUIntPtr => _context.Global.Services.TypeProvider.Resolved.SystemUIntPtr;

	public static StackState StackStateFor(ReadOnlyContext context, MethodDefinition method, ResolvedTypeFactory typeFactory, ResolvedMethodContext resolvedMethodInfo, ResolvedInstructionBlock block, StackState initialState)
	{
		return new StackStateBuilder(context, method, typeFactory, resolvedMethodInfo, initialState).Build(block);
	}

	private StackStateBuilder(ReadOnlyContext context, MethodDefinition method, ResolvedTypeFactory typeFactory, ResolvedMethodContext resolvedMethodInfo, StackState initialState)
	{
		_context = context;
		_methodDefinition = method;
		_typeFactory = typeFactory;
		_resolvedMethodInfo = resolvedMethodInfo;
		_simulationStack = new Stack<Entry>();
		foreach (Entry entry in initialState.Entries.Reverse())
		{
			_simulationStack.Push(entry.Clone());
		}
	}

	private StackState Build(ResolvedInstructionBlock block)
	{
		StackState stackState = new StackState();
		ResolvedInstruction previous = null;
		foreach (ResolvedInstruction instruction in block.Instructions)
		{
			SetupCatchBlockIfNeeded(instruction);
			switch (instruction.OpCode.Code)
			{
			case Code.Ldarg_0:
			case Code.Ldarg_1:
			case Code.Ldarg_2:
			case Code.Ldarg_3:
				LoadArg(instruction, instruction.ParameterInfo);
				break;
			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
				LoadLocal(instruction, instruction.VariableInfo);
				break;
			case Code.Stloc_0:
				PopEntry();
				break;
			case Code.Stloc_1:
				PopEntry();
				break;
			case Code.Stloc_2:
				PopEntry();
				break;
			case Code.Stloc_3:
				PopEntry();
				break;
			case Code.Ldarg_S:
				LoadArg(instruction, instruction.ParameterInfo);
				break;
			case Code.Ldarga_S:
				LoadArgumentAddress(instruction, instruction.ParameterInfo);
				break;
			case Code.Starg_S:
				PopEntry();
				break;
			case Code.Ldloc_S:
				LoadLocal(instruction, instruction.VariableInfo);
				break;
			case Code.Ldloca_S:
				LoadLocalAddress(instruction, instruction.VariableInfo);
				break;
			case Code.Stloc_S:
				PopEntry();
				break;
			case Code.Ldnull:
				PushNullStackEntry(instruction);
				break;
			case Code.Ldc_I4_M1:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_0:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_1:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_2:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_3:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_4:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_5:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_6:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_7:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_8:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4_S:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I4:
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldc_I8:
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Ldc_R4:
				PushStackEntry(instruction, SingleTypeReference);
				break;
			case Code.Ldc_R8:
				PushStackEntry(instruction, DoubleTypeReference);
				break;
			case Code.Dup:
				_simulationStack.Push(_simulationStack.Peek().Clone());
				break;
			case Code.Pop:
				PopEntry();
				break;
			case Code.Call:
				CallMethod(instruction, previous, instruction.MethodInfo);
				break;
			case Code.Calli:
				CallIndirectMethod(instruction, (CallSite)instruction.Operand);
				break;
			case Code.Ret:
				if (ReturnsValue())
				{
					PopEntry();
				}
				break;
			case Code.Brfalse_S:
				StackInstructionOptimizations.OptimizeBrFalse(_context, instruction, block, PopEntry(), _resolvedMethodInfo.Blocks);
				break;
			case Code.Brtrue_S:
				StackInstructionOptimizations.OptimizeBrTrue(_context, instruction, block, PopEntry(), _resolvedMethodInfo.Blocks);
				break;
			case Code.Beq_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Bge_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Bgt_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Ble_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Blt_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Bne_Un_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Bge_Un_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Bgt_Un_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Ble_Un_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Blt_Un_S:
				PopEntry();
				PopEntry();
				break;
			case Code.Brfalse:
				StackInstructionOptimizations.OptimizeBrFalse(_context, instruction, block, PopEntry(), _resolvedMethodInfo.Blocks);
				break;
			case Code.Brtrue:
				StackInstructionOptimizations.OptimizeBrTrue(_context, instruction, block, PopEntry(), _resolvedMethodInfo.Blocks);
				break;
			case Code.Beq:
				PopEntry();
				PopEntry();
				break;
			case Code.Bge:
				PopEntry();
				PopEntry();
				break;
			case Code.Bgt:
				PopEntry();
				PopEntry();
				break;
			case Code.Ble:
				PopEntry();
				PopEntry();
				break;
			case Code.Blt:
				PopEntry();
				PopEntry();
				break;
			case Code.Bne_Un:
				PopEntry();
				PopEntry();
				break;
			case Code.Bge_Un:
				PopEntry();
				PopEntry();
				break;
			case Code.Bgt_Un:
				PopEntry();
				PopEntry();
				break;
			case Code.Ble_Un:
				PopEntry();
				PopEntry();
				break;
			case Code.Blt_Un:
				PopEntry();
				PopEntry();
				break;
			case Code.Switch:
				PopEntry();
				break;
			case Code.Ldind_I1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_U1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_I2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_U2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_I4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_U4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldind_I8:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Ldind_I:
			{
				ResolvedTypeInfo elementType2 = PopEntry().Types.First().GetElementType();
				if (elementType2.IsIntegralPointerType())
				{
					PushStackEntry(instruction, elementType2);
				}
				else
				{
					PushStackEntry(instruction, SystemIntPtr);
				}
				break;
			}
			case Code.Ldind_R4:
				PopEntry();
				PushStackEntry(instruction, SingleTypeReference);
				break;
			case Code.Ldind_R8:
				PopEntry();
				PushStackEntry(instruction, DoubleTypeReference);
				break;
			case Code.Ldind_Ref:
			{
				ResolvedTypeInfo byRef = PopEntry().Types.First();
				PushStackEntry(instruction, byRef.GetElementType());
				break;
			}
			case Code.Stind_Ref:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_I1:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_I2:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_I4:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_I8:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_R4:
				PopEntry();
				PopEntry();
				break;
			case Code.Stind_R8:
				PopEntry();
				PopEntry();
				break;
			case Code.Add:
				_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForAdd));
				break;
			case Code.Sub:
				_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForSub));
				break;
			case Code.Mul:
				_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForMul));
				break;
			case Code.Div:
			{
				PopEntry();
				Entry leftEntry14 = PopEntry();
				_simulationStack.Push(leftEntry14.Clone());
				break;
			}
			case Code.Div_Un:
				PopEntry();
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Rem:
			{
				PopEntry();
				Entry leftEntry13 = PopEntry();
				_simulationStack.Push(leftEntry13.Clone());
				break;
			}
			case Code.Rem_Un:
				PopEntry();
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.And:
			{
				PopEntry();
				Entry leftEntry12 = PopEntry();
				_simulationStack.Push(leftEntry12.Clone());
				break;
			}
			case Code.Or:
			{
				PopEntry();
				Entry leftEntry11 = PopEntry();
				_simulationStack.Push(leftEntry11.Clone());
				break;
			}
			case Code.Xor:
			{
				PopEntry();
				Entry leftEntry10 = PopEntry();
				_simulationStack.Push(leftEntry10.Clone());
				break;
			}
			case Code.Shl:
			{
				PopEntry();
				Entry leftEntry9 = PopEntry();
				_simulationStack.Push(leftEntry9.Clone());
				break;
			}
			case Code.Shr:
			{
				PopEntry();
				Entry leftEntry8 = PopEntry();
				_simulationStack.Push(leftEntry8.Clone());
				break;
			}
			case Code.Shr_Un:
			{
				PopEntry();
				Entry leftEntry7 = PopEntry();
				_simulationStack.Push(leftEntry7.Clone());
				break;
			}
			case Code.Neg:
			{
				ResolvedTypeInfo stackType = StackTypeConverter.StackTypeFor(_context, PopEntry().Types.First());
				PushStackEntry(instruction, StackAnalysisUtils.CalculateResultTypeForNegate(_context, stackType));
				break;
			}
			case Code.Not:
				PushStackEntry(instruction, StackTypeConverter.StackTypeFor(_context, PopEntry().Types.First()));
				break;
			case Code.Conv_I1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_I2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_I4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_I8:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Conv_R4:
				PopEntry();
				PushStackEntry(instruction, SingleTypeReference);
				break;
			case Code.Conv_R8:
				PopEntry();
				PushStackEntry(instruction, DoubleTypeReference);
				break;
			case Code.Conv_U4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_U8:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Callvirt:
				CallMethod(instruction, previous, instruction.MethodInfo);
				break;
			case Code.Cpobj:
				PopEntry();
				PopEntry();
				break;
			case Code.Ldobj:
				PopEntry();
				PushStackEntry(instruction, instruction.TypeInfo);
				break;
			case Code.Ldstr:
				PushStackEntry(instruction, StringTypeReference);
				break;
			case Code.Newobj:
			{
				ResolvedMethodInfo methodReference = instruction.MethodInfo;
				CallConstructor(instruction, methodReference);
				PushStackEntry(instruction, methodReference.DeclaringType);
				break;
			}
			case Code.Castclass:
				PopEntry();
				PushStackEntry(instruction, instruction.TypeInfo);
				break;
			case Code.Isinst:
				PopEntry();
				PushStackEntry(instruction, (instruction.TypeInfo.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType || instruction.TypeInfo.GetRuntimeStorage(_context).IsVariableSized()) ? ObjectTypeReference : instruction.TypeInfo);
				break;
			case Code.Conv_R_Un:
				PopEntry();
				PushStackEntry(instruction, SingleTypeReference);
				break;
			case Code.Unbox:
				HandleStackStateForUnbox(instruction);
				break;
			case Code.Throw:
				PopEntry();
				break;
			case Code.Ldfld:
				PopEntry();
				PushStackEntry(instruction, instruction.FieldInfo.FieldType);
				break;
			case Code.Ldflda:
				PopEntry();
				PushStackEntry(instruction, instruction.FieldInfo.FieldType.MakeByReferenceType(_context));
				break;
			case Code.Stfld:
				PopEntry();
				PopEntry();
				break;
			case Code.Ldsfld:
				PushStackEntry(instruction, instruction.FieldInfo.FieldType);
				StackInstructionOptimizations.OptimizeLdsfld(_context, instruction);
				break;
			case Code.Ldsflda:
				PushStackEntry(instruction, instruction.FieldInfo.FieldType.MakeByReferenceType(_context));
				break;
			case Code.Stsfld:
				PopEntry();
				break;
			case Code.Stobj:
				PopEntry();
				PopEntry();
				break;
			case Code.Conv_Ovf_I1_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I2_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I4_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I8_Un:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Conv_Ovf_U1_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U2_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U4_Un:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U8_Un:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Conv_Ovf_I_Un:
				PopEntry();
				PushStackEntry(instruction, SystemIntPtr);
				break;
			case Code.Conv_Ovf_U_Un:
				PopEntry();
				PushStackEntry(instruction, SystemUIntPtr);
				break;
			case Code.Box:
				PopEntry();
				PushStackEntry(instruction, StackEntryForBoxedType(instruction.TypeInfo));
				StackInstructionOptimizations.OptimizeBox(_context, instruction, block);
				break;
			case Code.Newarr:
				PopEntry();
				PushStackEntry(instruction, instruction.TypeInfo.MakeArrayType(_context));
				break;
			case Code.Ldlen:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldelema:
				LoadElement(instruction, instruction.TypeInfo.MakeByReferenceType(_context));
				break;
			case Code.Ldelem_I1:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_U1:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_I2:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_U2:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_I4:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_U4:
				LoadElement(instruction, Int32TypeReference);
				break;
			case Code.Ldelem_I8:
				LoadElement(instruction, Int64TypeReference);
				break;
			case Code.Ldelem_I:
			{
				PopEntry();
				ResolvedTypeInfo elementType = PopEntry().Types.First().GetElementType();
				if (elementType.IsIntegralPointerType())
				{
					PushStackEntry(instruction, elementType);
				}
				else
				{
					PushStackEntry(instruction, Int32TypeReference);
				}
				break;
			}
			case Code.Ldelem_R4:
				LoadElement(instruction, SingleTypeReference);
				break;
			case Code.Ldelem_R8:
				LoadElement(instruction, DoubleTypeReference);
				break;
			case Code.Ldelem_Ref:
			{
				PopEntry();
				Entry array = PopEntry();
				ResolvedTypeInfo arrayTypeReference = array.Types.Single();
				ResolvedTypeInfo typeReferenceToPush = ((!arrayTypeReference.IsArray && !arrayTypeReference.IsTypeSpecification) ? arrayTypeReference.GetElementType() : ArrayUtilities.ArrayElementTypeOf(array.Types.Single()));
				PushStackEntry(instruction, typeReferenceToPush);
				break;
			}
			case Code.Stelem_I:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_I1:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_I2:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_I4:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_I8:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_R4:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_R8:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Stelem_Ref:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Ldelem_Any:
				PopEntry();
				PopEntry();
				PushStackEntry(instruction, instruction.TypeInfo);
				break;
			case Code.Stelem_Any:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Unbox_Any:
				HandleStackStateForUnboxAny(instruction);
				break;
			case Code.Conv_Ovf_I1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_U4:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_Ovf_I8:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Conv_Ovf_U8:
				PopEntry();
				PushStackEntry(instruction, Int64TypeReference);
				break;
			case Code.Refanyval:
				PopEntry();
				PushStackEntry(instruction, instruction.TypeInfo.MakeByReferenceType(_context));
				break;
			case Code.Ckfinite:
				throw new NotImplementedException("The chkfinite opcode is not implemented");
			case Code.Mkrefany:
				PopEntry();
				PushStackEntry(instruction, _context.Global.Services.TypeProvider.Resolved.TypedReference);
				break;
			case Code.Ldtoken:
				PushStackEntry(instruction, StackEntryForLdToken(instruction.Operand));
				break;
			case Code.Conv_U2:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_U1:
				PopEntry();
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Conv_I:
				PopEntry();
				PushStackEntry(instruction, SystemIntPtr);
				break;
			case Code.Conv_Ovf_I:
				PopEntry();
				PushStackEntry(instruction, SystemIntPtr);
				break;
			case Code.Conv_Ovf_U:
				PopEntry();
				PushStackEntry(instruction, SystemUIntPtr);
				break;
			case Code.Add_Ovf:
			{
				PopEntry();
				Entry leftEntry6 = PopEntry();
				_simulationStack.Push(leftEntry6.Clone());
				break;
			}
			case Code.Add_Ovf_Un:
			{
				PopEntry();
				Entry leftEntry5 = PopEntry();
				_simulationStack.Push(leftEntry5.Clone());
				break;
			}
			case Code.Mul_Ovf:
			{
				PopEntry();
				Entry leftEntry4 = PopEntry();
				_simulationStack.Push(leftEntry4.Clone());
				break;
			}
			case Code.Mul_Ovf_Un:
			{
				PopEntry();
				Entry leftEntry3 = PopEntry();
				_simulationStack.Push(leftEntry3.Clone());
				break;
			}
			case Code.Sub_Ovf:
			{
				PopEntry();
				Entry leftEntry2 = PopEntry();
				_simulationStack.Push(leftEntry2.Clone());
				break;
			}
			case Code.Sub_Ovf_Un:
			{
				PopEntry();
				Entry leftEntry = PopEntry();
				_simulationStack.Push(leftEntry.Clone());
				break;
			}
			case Code.Leave:
				EmptyStack();
				break;
			case Code.Leave_S:
				EmptyStack();
				break;
			case Code.Stind_I:
				PopEntry();
				PopEntry();
				break;
			case Code.Conv_U:
				PopEntry();
				PushStackEntry(instruction, SystemUIntPtr);
				break;
			case Code.Arglist:
				PushStackEntry(instruction, SystemIntPtr);
				break;
			case Code.Ceq:
			case Code.Cgt:
			case Code.Cgt_Un:
			case Code.Clt:
			case Code.Clt_Un:
				StackInstructionOptimizations.OptimizeComparison(_context, instruction, PopEntry(), PopEntry());
				PushStackEntry(instruction, Int32TypeReference);
				break;
			case Code.Ldftn:
				PushStackEntry(instruction, IntPtrTypeReference);
				break;
			case Code.Ldvirtftn:
				PopEntry();
				PushStackEntry(instruction, IntPtrTypeReference);
				break;
			case Code.Ldarg:
				LoadArg(instruction, instruction.ParameterInfo);
				break;
			case Code.Ldarga:
				LoadArgumentAddress(instruction, instruction.ParameterInfo);
				break;
			case Code.Starg:
				PopEntry();
				break;
			case Code.Ldloc:
				LoadLocal(instruction, instruction.VariableInfo);
				break;
			case Code.Ldloca:
				LoadLocalAddress(instruction, instruction.VariableInfo);
				break;
			case Code.Stloc:
				PopEntry();
				break;
			case Code.Localloc:
				PopEntry();
				PushStackEntry(instruction, SByteTypeReference.MakePointerType(_context));
				break;
			case Code.Endfilter:
				PopEntry();
				break;
			case Code.Initobj:
				PopEntry();
				break;
			case Code.Cpblk:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.Initblk:
				PopEntry();
				PopEntry();
				PopEntry();
				break;
			case Code.No:
				throw new NotImplementedException("The 'no' opcode is not implemented");
			case Code.Sizeof:
				PushStackEntry(instruction, UInt32TypeReference);
				break;
			case Code.Refanytype:
				PopEntry();
				PushStackEntry(instruction, _context.Global.Services.TypeProvider.Resolved.RuntimeTypeHandleTypeReference);
				break;
			}
			previous = instruction;
		}
		foreach (Entry entry in _simulationStack.Reverse())
		{
			stackState.Entries.Push(entry.Clone());
		}
		return stackState;
	}

	private void HandleStackStateForUnboxAny(ResolvedInstruction instruction)
	{
		PopEntry();
		PushStackEntry(instruction, instruction.TypeInfo);
	}

	private void HandleStackStateForUnbox(ResolvedInstruction instruction)
	{
		PopEntry();
		PushStackEntry(instruction, instruction.TypeInfo.MakeByReferenceType(_context));
	}

	private Entry GetResultEntryUsing(StackAnalysisUtils.ResultTypeAnalysisMethod getResultType)
	{
		Entry rightEntry = PopEntry();
		Entry leftEntry = PopEntry();
		return new Entry
		{
			Types = { getResultType(_context, leftEntry.Types.First(), rightEntry.Types.First()) }
		};
	}

	private ResolvedTypeInfo StackEntryForBoxedType(ResolvedTypeInfo operandType)
	{
		if (operandType == null)
		{
			return ObjectTypeReference;
		}
		if (!(operandType.ResolvedType is GenericParameter genericParameter))
		{
			return ObjectTypeReference;
		}
		if (genericParameter.Constraints.Count == 0)
		{
			return ObjectTypeReference;
		}
		ResolvedTypeInfo result = _typeFactory.Create(genericParameter);
		if (result.GetRuntimeStorage(_context) == RuntimeStorageKind.ValueType || result.GetRuntimeStorage(_context).IsVariableSized())
		{
			return ObjectTypeReference;
		}
		return result;
	}

	private ResolvedTypeInfo StackEntryForLdToken(object operand)
	{
		if (operand is TypeReference)
		{
			return _context.Global.Services.TypeProvider.Resolved.RuntimeTypeHandleTypeReference;
		}
		if (operand is FieldReference)
		{
			return _context.Global.Services.TypeProvider.Resolved.RuntimeFieldHandleTypeReference;
		}
		if (operand is MethodReference)
		{
			return _context.Global.Services.TypeProvider.Resolved.RuntimeMethodHandleTypeReference;
		}
		throw new ArgumentException();
	}

	private void LoadArgumentAddress(ResolvedInstruction instruction, ResolvedParameter parameter)
	{
		ResolvedTypeInfo parameterType = parameter.ParameterType;
		if (parameterType.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			PushStackEntry(instruction, parameterType);
		}
		else
		{
			PushStackEntry(instruction, parameterType.MakeByReferenceType(_context));
		}
	}

	private void SetupCatchBlockIfNeeded(ResolvedInstruction instruction)
	{
		MethodBody methodBody = _methodDefinition.Body;
		if (!methodBody.HasExceptionHandlers)
		{
			return;
		}
		foreach (ExceptionHandler exceptionHandler in methodBody.ExceptionHandlers)
		{
			if (exceptionHandler.HandlerType == ExceptionHandlerType.Catch && exceptionHandler.HandlerStart.Offset == instruction.Offset)
			{
				PushStackEntry(instruction, _typeFactory.Create(exceptionHandler.CatchType));
			}
			else if (exceptionHandler.HandlerType == ExceptionHandlerType.Filter && (exceptionHandler.FilterStart.Offset == instruction.Offset || exceptionHandler.HandlerStart.Offset == instruction.Offset))
			{
				PushStackEntry(instruction, _context.Global.Services.TypeProvider.Resolved.SystemException);
			}
		}
	}

	private bool ReturnsValue()
	{
		if (_methodDefinition.ReturnType != null)
		{
			return _methodDefinition.ReturnType.IsNotVoid;
		}
		return false;
	}

	private void LoadElement(ResolvedInstruction instruction, ResolvedTypeInfo typeReference)
	{
		PopEntry();
		PopEntry();
		PushStackEntry(instruction, typeReference);
	}

	private void LoadArg(ResolvedInstruction instruction, ResolvedParameter parameter)
	{
		PushStackEntry(instruction, parameter.ParameterType);
	}

	private void LoadLocal(ResolvedInstruction instruction, ResolvedVariable variable)
	{
		PushStackEntry(instruction, variable.VariableType);
	}

	private void LoadLocalAddress(ResolvedInstruction instruction, ResolvedVariable variable)
	{
		ResolvedTypeInfo type = variable.VariableType;
		if (type.GetRuntimeStorage(_context) == RuntimeStorageKind.VariableSizedAny)
		{
			PushStackEntry(instruction, type);
		}
		else
		{
			PushStackEntry(instruction, type.MakeByReferenceType(_context));
		}
	}

	private void CallMethod(ResolvedInstruction instruction, ResolvedInstruction previous, ResolvedMethodInfo methodReference)
	{
		int argumentCount = methodReference.Parameters.Count + (methodReference.HasThis ? 1 : 0);
		List<Entry> arguments = new List<Entry>(argumentCount);
		for (int i = 0; i < argumentCount; i++)
		{
			arguments.Add(PopEntry());
		}
		arguments.Reverse();
		StackInstructionOptimizations.OptimizeMethodCall(_context, instruction, previous, arguments);
		ResolvedTypeInfo returnType = methodReference.ReturnType;
		if (returnType != null && returnType.IsNotVoid())
		{
			PushStackEntry(instruction, returnType);
		}
	}

	private void CallMethod(ResolvedInstruction instruction, CallSite callSite)
	{
		for (int i = 0; i < callSite.Parameters.Count; i++)
		{
			PopEntry();
		}
		if (callSite.HasThis)
		{
			PopEntry();
		}
		TypeReference returnType = callSite.ReturnType;
		if (returnType != null && returnType.IsNotVoid)
		{
			PushStackEntry(instruction, _typeFactory.Create(callSite.ReturnType));
		}
	}

	private void CallConstructor(ResolvedInstruction instruction, ResolvedMethodInfo methodReference)
	{
		for (int i = 0; i < methodReference.Parameters.Count; i++)
		{
			PopEntry();
		}
		ResolvedTypeInfo returnType = methodReference.ReturnType;
		if (returnType != null && returnType.IsNotVoid())
		{
			PushStackEntry(instruction, returnType);
		}
	}

	private void CallIndirectMethod(ResolvedInstruction instruction, CallSite callSite)
	{
		PopEntry();
		CallMethod(instruction, callSite);
	}

	private void PushNullStackEntry(ResolvedInstruction instruction)
	{
		_simulationStack.Push(Entry.ForNull(ObjectTypeReference, instruction));
	}

	private void PushStackEntry(ResolvedInstruction instruction, ResolvedTypeInfo typeReference)
	{
		if (typeReference == null)
		{
			throw new ArgumentNullException("typeReference");
		}
		TypeDefinition typeDefinition = typeReference.ResolvedType.Resolve();
		if (typeReference.ResolvedType.ContainsGenericParameter && (typeDefinition == null || !typeDefinition.IsEnum))
		{
			throw new NotImplementedException();
		}
		_simulationStack.Push(Entry.For(instruction, typeReference));
	}

	private Entry PopEntry()
	{
		return _simulationStack.Pop();
	}

	private void EmptyStack()
	{
		while (_simulationStack.Count > 0)
		{
			PopEntry();
		}
	}
}
