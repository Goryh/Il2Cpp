using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP.DataModel;

public static class OpCodes
{
	private static readonly ReadOnlyDictionary<Mono.Cecil.Cil.OpCode, OpCode> _opCodeMap;

	public static readonly OpCode Nop;

	public static readonly OpCode Break;

	public static readonly OpCode Ldarg_0;

	public static readonly OpCode Ldarg_1;

	public static readonly OpCode Ldarg_2;

	public static readonly OpCode Ldarg_3;

	public static readonly OpCode Ldloc_0;

	public static readonly OpCode Ldloc_1;

	public static readonly OpCode Ldloc_2;

	public static readonly OpCode Ldloc_3;

	public static readonly OpCode Stloc_0;

	public static readonly OpCode Stloc_1;

	public static readonly OpCode Stloc_2;

	public static readonly OpCode Stloc_3;

	public static readonly OpCode Ldarg_S;

	public static readonly OpCode Ldarga_S;

	public static readonly OpCode Starg_S;

	public static readonly OpCode Ldloc_S;

	public static readonly OpCode Ldloca_S;

	public static readonly OpCode Stloc_S;

	public static readonly OpCode Ldnull;

	public static readonly OpCode Ldc_I4_M1;

	public static readonly OpCode Ldc_I4_0;

	public static readonly OpCode Ldc_I4_1;

	public static readonly OpCode Ldc_I4_2;

	public static readonly OpCode Ldc_I4_3;

	public static readonly OpCode Ldc_I4_4;

	public static readonly OpCode Ldc_I4_5;

	public static readonly OpCode Ldc_I4_6;

	public static readonly OpCode Ldc_I4_7;

	public static readonly OpCode Ldc_I4_8;

	public static readonly OpCode Ldc_I4_S;

	public static readonly OpCode Ldc_I4;

	public static readonly OpCode Ldc_I8;

	public static readonly OpCode Ldc_R4;

	public static readonly OpCode Ldc_R8;

	public static readonly OpCode Dup;

	public static readonly OpCode Pop;

	public static readonly OpCode Jmp;

	public static readonly OpCode Call;

	public static readonly OpCode Calli;

	public static readonly OpCode Ret;

	public static readonly OpCode Br_S;

	public static readonly OpCode Brfalse_S;

	public static readonly OpCode Brtrue_S;

	public static readonly OpCode Beq_S;

	public static readonly OpCode Bge_S;

	public static readonly OpCode Bgt_S;

	public static readonly OpCode Ble_S;

	public static readonly OpCode Blt_S;

	public static readonly OpCode Bne_Un_S;

	public static readonly OpCode Bge_Un_S;

	public static readonly OpCode Bgt_Un_S;

	public static readonly OpCode Ble_Un_S;

	public static readonly OpCode Blt_Un_S;

	public static readonly OpCode Br;

	public static readonly OpCode Brfalse;

	public static readonly OpCode Brtrue;

	public static readonly OpCode Beq;

	public static readonly OpCode Bge;

	public static readonly OpCode Bgt;

	public static readonly OpCode Ble;

	public static readonly OpCode Blt;

	public static readonly OpCode Bne_Un;

	public static readonly OpCode Bge_Un;

	public static readonly OpCode Bgt_Un;

	public static readonly OpCode Ble_Un;

	public static readonly OpCode Blt_Un;

	public static readonly OpCode Switch;

	public static readonly OpCode Ldind_I1;

	public static readonly OpCode Ldind_U1;

	public static readonly OpCode Ldind_I2;

	public static readonly OpCode Ldind_U2;

	public static readonly OpCode Ldind_I4;

	public static readonly OpCode Ldind_U4;

	public static readonly OpCode Ldind_I8;

	public static readonly OpCode Ldind_I;

	public static readonly OpCode Ldind_R4;

	public static readonly OpCode Ldind_R8;

	public static readonly OpCode Ldind_Ref;

	public static readonly OpCode Stind_Ref;

	public static readonly OpCode Stind_I1;

	public static readonly OpCode Stind_I2;

	public static readonly OpCode Stind_I4;

	public static readonly OpCode Stind_I8;

	public static readonly OpCode Stind_R4;

	public static readonly OpCode Stind_R8;

	public static readonly OpCode Add;

	public static readonly OpCode Sub;

	public static readonly OpCode Mul;

	public static readonly OpCode Div;

	public static readonly OpCode Div_Un;

	public static readonly OpCode Rem;

	public static readonly OpCode Rem_Un;

	public static readonly OpCode And;

	public static readonly OpCode Or;

	public static readonly OpCode Xor;

	public static readonly OpCode Shl;

	public static readonly OpCode Shr;

	public static readonly OpCode Shr_Un;

	public static readonly OpCode Neg;

	public static readonly OpCode Not;

	public static readonly OpCode Conv_I1;

	public static readonly OpCode Conv_I2;

	public static readonly OpCode Conv_I4;

	public static readonly OpCode Conv_I8;

	public static readonly OpCode Conv_R4;

	public static readonly OpCode Conv_R8;

	public static readonly OpCode Conv_U4;

	public static readonly OpCode Conv_U8;

	public static readonly OpCode Callvirt;

	public static readonly OpCode Cpobj;

	public static readonly OpCode Ldobj;

	public static readonly OpCode Ldstr;

	public static readonly OpCode Newobj;

	public static readonly OpCode Castclass;

	public static readonly OpCode Isinst;

	public static readonly OpCode Conv_R_Un;

	public static readonly OpCode Unbox;

	public static readonly OpCode Throw;

	public static readonly OpCode Ldfld;

	public static readonly OpCode Ldflda;

	public static readonly OpCode Stfld;

	public static readonly OpCode Ldsfld;

	public static readonly OpCode Ldsflda;

	public static readonly OpCode Stsfld;

	public static readonly OpCode Stobj;

	public static readonly OpCode Conv_Ovf_I1_Un;

	public static readonly OpCode Conv_Ovf_I2_Un;

	public static readonly OpCode Conv_Ovf_I4_Un;

	public static readonly OpCode Conv_Ovf_I8_Un;

	public static readonly OpCode Conv_Ovf_U1_Un;

	public static readonly OpCode Conv_Ovf_U2_Un;

	public static readonly OpCode Conv_Ovf_U4_Un;

	public static readonly OpCode Conv_Ovf_U8_Un;

	public static readonly OpCode Conv_Ovf_I_Un;

	public static readonly OpCode Conv_Ovf_U_Un;

	public static readonly OpCode Box;

	public static readonly OpCode Newarr;

	public static readonly OpCode Ldlen;

	public static readonly OpCode Ldelema;

	public static readonly OpCode Ldelem_I1;

	public static readonly OpCode Ldelem_U1;

	public static readonly OpCode Ldelem_I2;

	public static readonly OpCode Ldelem_U2;

	public static readonly OpCode Ldelem_I4;

	public static readonly OpCode Ldelem_U4;

	public static readonly OpCode Ldelem_I8;

	public static readonly OpCode Ldelem_I;

	public static readonly OpCode Ldelem_R4;

	public static readonly OpCode Ldelem_R8;

	public static readonly OpCode Ldelem_Ref;

	public static readonly OpCode Stelem_I;

	public static readonly OpCode Stelem_I1;

	public static readonly OpCode Stelem_I2;

	public static readonly OpCode Stelem_I4;

	public static readonly OpCode Stelem_I8;

	public static readonly OpCode Stelem_R4;

	public static readonly OpCode Stelem_R8;

	public static readonly OpCode Stelem_Ref;

	public static readonly OpCode Ldelem_Any;

	public static readonly OpCode Stelem_Any;

	public static readonly OpCode Unbox_Any;

	public static readonly OpCode Conv_Ovf_I1;

	public static readonly OpCode Conv_Ovf_U1;

	public static readonly OpCode Conv_Ovf_I2;

	public static readonly OpCode Conv_Ovf_U2;

	public static readonly OpCode Conv_Ovf_I4;

	public static readonly OpCode Conv_Ovf_U4;

	public static readonly OpCode Conv_Ovf_I8;

	public static readonly OpCode Conv_Ovf_U8;

	public static readonly OpCode Refanyval;

	public static readonly OpCode Ckfinite;

	public static readonly OpCode Mkrefany;

	public static readonly OpCode Ldtoken;

	public static readonly OpCode Conv_U2;

	public static readonly OpCode Conv_U1;

	public static readonly OpCode Conv_I;

	public static readonly OpCode Conv_Ovf_I;

	public static readonly OpCode Conv_Ovf_U;

	public static readonly OpCode Add_Ovf;

	public static readonly OpCode Add_Ovf_Un;

	public static readonly OpCode Mul_Ovf;

	public static readonly OpCode Mul_Ovf_Un;

	public static readonly OpCode Sub_Ovf;

	public static readonly OpCode Sub_Ovf_Un;

	public static readonly OpCode Endfinally;

	public static readonly OpCode Leave;

	public static readonly OpCode Leave_S;

	public static readonly OpCode Stind_I;

	public static readonly OpCode Conv_U;

	public static readonly OpCode Arglist;

	public static readonly OpCode Ceq;

	public static readonly OpCode Cgt;

	public static readonly OpCode Cgt_Un;

	public static readonly OpCode Clt;

	public static readonly OpCode Clt_Un;

	public static readonly OpCode Ldftn;

	public static readonly OpCode Ldvirtftn;

	public static readonly OpCode Ldarg;

	public static readonly OpCode Ldarga;

	public static readonly OpCode Starg;

	public static readonly OpCode Ldloc;

	public static readonly OpCode Ldloca;

	public static readonly OpCode Stloc;

	public static readonly OpCode Localloc;

	public static readonly OpCode Endfilter;

	public static readonly OpCode Unaligned;

	public static readonly OpCode Volatile;

	public static readonly OpCode Tail;

	public static readonly OpCode Initobj;

	public static readonly OpCode Constrained;

	public static readonly OpCode Cpblk;

	public static readonly OpCode Initblk;

	public static readonly OpCode No;

	public static readonly OpCode Rethrow;

	public static readonly OpCode Sizeof;

	public static readonly OpCode Refanytype;

	public static readonly OpCode Readonly;

	internal static OpCode TranslateOpCode(Mono.Cecil.Cil.OpCode opcode)
	{
		return _opCodeMap[opcode];
	}

	static OpCodes()
	{
		Nop = new OpCode(Mono.Cecil.Cil.OpCodes.Nop);
		Break = new OpCode(Mono.Cecil.Cil.OpCodes.Break);
		Ldarg_0 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg_0);
		Ldarg_1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg_1);
		Ldarg_2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg_2);
		Ldarg_3 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg_3);
		Ldloc_0 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc_0);
		Ldloc_1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc_1);
		Ldloc_2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc_2);
		Ldloc_3 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc_3);
		Stloc_0 = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc_0);
		Stloc_1 = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc_1);
		Stloc_2 = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc_2);
		Stloc_3 = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc_3);
		Ldarg_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg_S);
		Ldarga_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarga_S);
		Starg_S = new OpCode(Mono.Cecil.Cil.OpCodes.Starg_S);
		Ldloc_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc_S);
		Ldloca_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloca_S);
		Stloc_S = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc_S);
		Ldnull = new OpCode(Mono.Cecil.Cil.OpCodes.Ldnull);
		Ldc_I4_M1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_M1);
		Ldc_I4_0 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
		Ldc_I4_1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
		Ldc_I4_2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_2);
		Ldc_I4_3 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_3);
		Ldc_I4_4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_4);
		Ldc_I4_5 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_5);
		Ldc_I4_6 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_6);
		Ldc_I4_7 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_7);
		Ldc_I4_8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_8);
		Ldc_I4_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4_S);
		Ldc_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I4);
		Ldc_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_I8);
		Ldc_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_R4);
		Ldc_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldc_R8);
		Dup = new OpCode(Mono.Cecil.Cil.OpCodes.Dup);
		Pop = new OpCode(Mono.Cecil.Cil.OpCodes.Pop);
		Jmp = new OpCode(Mono.Cecil.Cil.OpCodes.Jmp);
		Call = new OpCode(Mono.Cecil.Cil.OpCodes.Call);
		Calli = new OpCode(Mono.Cecil.Cil.OpCodes.Calli);
		Ret = new OpCode(Mono.Cecil.Cil.OpCodes.Ret);
		Br_S = new OpCode(Mono.Cecil.Cil.OpCodes.Br_S);
		Brfalse_S = new OpCode(Mono.Cecil.Cil.OpCodes.Brfalse_S);
		Brtrue_S = new OpCode(Mono.Cecil.Cil.OpCodes.Brtrue_S);
		Beq_S = new OpCode(Mono.Cecil.Cil.OpCodes.Beq_S);
		Bge_S = new OpCode(Mono.Cecil.Cil.OpCodes.Bge_S);
		Bgt_S = new OpCode(Mono.Cecil.Cil.OpCodes.Bgt_S);
		Ble_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ble_S);
		Blt_S = new OpCode(Mono.Cecil.Cil.OpCodes.Blt_S);
		Bne_Un_S = new OpCode(Mono.Cecil.Cil.OpCodes.Bne_Un_S);
		Bge_Un_S = new OpCode(Mono.Cecil.Cil.OpCodes.Bge_Un_S);
		Bgt_Un_S = new OpCode(Mono.Cecil.Cil.OpCodes.Bgt_Un_S);
		Ble_Un_S = new OpCode(Mono.Cecil.Cil.OpCodes.Ble_Un_S);
		Blt_Un_S = new OpCode(Mono.Cecil.Cil.OpCodes.Blt_Un_S);
		Br = new OpCode(Mono.Cecil.Cil.OpCodes.Br);
		Brfalse = new OpCode(Mono.Cecil.Cil.OpCodes.Brfalse);
		Brtrue = new OpCode(Mono.Cecil.Cil.OpCodes.Brtrue);
		Beq = new OpCode(Mono.Cecil.Cil.OpCodes.Beq);
		Bge = new OpCode(Mono.Cecil.Cil.OpCodes.Bge);
		Bgt = new OpCode(Mono.Cecil.Cil.OpCodes.Bgt);
		Ble = new OpCode(Mono.Cecil.Cil.OpCodes.Ble);
		Blt = new OpCode(Mono.Cecil.Cil.OpCodes.Blt);
		Bne_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Bne_Un);
		Bge_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Bge_Un);
		Bgt_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Bgt_Un);
		Ble_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Ble_Un);
		Blt_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Blt_Un);
		Switch = new OpCode(Mono.Cecil.Cil.OpCodes.Switch);
		Ldind_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_I1);
		Ldind_U1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_U1);
		Ldind_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_I2);
		Ldind_U2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_U2);
		Ldind_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_I4);
		Ldind_U4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_U4);
		Ldind_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_I8);
		Ldind_I = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_I);
		Ldind_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_R4);
		Ldind_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_R8);
		Ldind_Ref = new OpCode(Mono.Cecil.Cil.OpCodes.Ldind_Ref);
		Stind_Ref = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_Ref);
		Stind_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_I1);
		Stind_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_I2);
		Stind_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_I4);
		Stind_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_I8);
		Stind_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_R4);
		Stind_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_R8);
		Add = new OpCode(Mono.Cecil.Cil.OpCodes.Add);
		Sub = new OpCode(Mono.Cecil.Cil.OpCodes.Sub);
		Mul = new OpCode(Mono.Cecil.Cil.OpCodes.Mul);
		Div = new OpCode(Mono.Cecil.Cil.OpCodes.Div);
		Div_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Div_Un);
		Rem = new OpCode(Mono.Cecil.Cil.OpCodes.Rem);
		Rem_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Rem_Un);
		And = new OpCode(Mono.Cecil.Cil.OpCodes.And);
		Or = new OpCode(Mono.Cecil.Cil.OpCodes.Or);
		Xor = new OpCode(Mono.Cecil.Cil.OpCodes.Xor);
		Shl = new OpCode(Mono.Cecil.Cil.OpCodes.Shl);
		Shr = new OpCode(Mono.Cecil.Cil.OpCodes.Shr);
		Shr_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Shr_Un);
		Neg = new OpCode(Mono.Cecil.Cil.OpCodes.Neg);
		Not = new OpCode(Mono.Cecil.Cil.OpCodes.Not);
		Conv_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_I1);
		Conv_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_I2);
		Conv_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_I4);
		Conv_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_I8);
		Conv_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_R4);
		Conv_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_R8);
		Conv_U4 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_U4);
		Conv_U8 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_U8);
		Callvirt = new OpCode(Mono.Cecil.Cil.OpCodes.Callvirt);
		Cpobj = new OpCode(Mono.Cecil.Cil.OpCodes.Cpobj);
		Ldobj = new OpCode(Mono.Cecil.Cil.OpCodes.Ldobj);
		Ldstr = new OpCode(Mono.Cecil.Cil.OpCodes.Ldstr);
		Newobj = new OpCode(Mono.Cecil.Cil.OpCodes.Newobj);
		Castclass = new OpCode(Mono.Cecil.Cil.OpCodes.Castclass);
		Isinst = new OpCode(Mono.Cecil.Cil.OpCodes.Isinst);
		Conv_R_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_R_Un);
		Unbox = new OpCode(Mono.Cecil.Cil.OpCodes.Unbox);
		Throw = new OpCode(Mono.Cecil.Cil.OpCodes.Throw);
		Ldfld = new OpCode(Mono.Cecil.Cil.OpCodes.Ldfld);
		Ldflda = new OpCode(Mono.Cecil.Cil.OpCodes.Ldflda);
		Stfld = new OpCode(Mono.Cecil.Cil.OpCodes.Stfld);
		Ldsfld = new OpCode(Mono.Cecil.Cil.OpCodes.Ldsfld);
		Ldsflda = new OpCode(Mono.Cecil.Cil.OpCodes.Ldsflda);
		Stsfld = new OpCode(Mono.Cecil.Cil.OpCodes.Stsfld);
		Stobj = new OpCode(Mono.Cecil.Cil.OpCodes.Stobj);
		Conv_Ovf_I1_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I1_Un);
		Conv_Ovf_I2_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I2_Un);
		Conv_Ovf_I4_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I4_Un);
		Conv_Ovf_I8_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I8_Un);
		Conv_Ovf_U1_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U1_Un);
		Conv_Ovf_U2_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U2_Un);
		Conv_Ovf_U4_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U4_Un);
		Conv_Ovf_U8_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U8_Un);
		Conv_Ovf_I_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I_Un);
		Conv_Ovf_U_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U_Un);
		Box = new OpCode(Mono.Cecil.Cil.OpCodes.Box);
		Newarr = new OpCode(Mono.Cecil.Cil.OpCodes.Newarr);
		Ldlen = new OpCode(Mono.Cecil.Cil.OpCodes.Ldlen);
		Ldelema = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelema);
		Ldelem_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_I1);
		Ldelem_U1 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_U1);
		Ldelem_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_I2);
		Ldelem_U2 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_U2);
		Ldelem_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_I4);
		Ldelem_U4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_U4);
		Ldelem_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_I8);
		Ldelem_I = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_I);
		Ldelem_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_R4);
		Ldelem_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_R8);
		Ldelem_Ref = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_Ref);
		Stelem_I = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_I);
		Stelem_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_I1);
		Stelem_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_I2);
		Stelem_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_I4);
		Stelem_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_I8);
		Stelem_R4 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_R4);
		Stelem_R8 = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_R8);
		Stelem_Ref = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_Ref);
		Ldelem_Any = new OpCode(Mono.Cecil.Cil.OpCodes.Ldelem_Any);
		Stelem_Any = new OpCode(Mono.Cecil.Cil.OpCodes.Stelem_Any);
		Unbox_Any = new OpCode(Mono.Cecil.Cil.OpCodes.Unbox_Any);
		Conv_Ovf_I1 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I1);
		Conv_Ovf_U1 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U1);
		Conv_Ovf_I2 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I2);
		Conv_Ovf_U2 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U2);
		Conv_Ovf_I4 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I4);
		Conv_Ovf_U4 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U4);
		Conv_Ovf_I8 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I8);
		Conv_Ovf_U8 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U8);
		Refanyval = new OpCode(Mono.Cecil.Cil.OpCodes.Refanyval);
		Ckfinite = new OpCode(Mono.Cecil.Cil.OpCodes.Ckfinite);
		Mkrefany = new OpCode(Mono.Cecil.Cil.OpCodes.Mkrefany);
		Ldtoken = new OpCode(Mono.Cecil.Cil.OpCodes.Ldtoken);
		Conv_U2 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_U2);
		Conv_U1 = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_U1);
		Conv_I = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_I);
		Conv_Ovf_I = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_I);
		Conv_Ovf_U = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_Ovf_U);
		Add_Ovf = new OpCode(Mono.Cecil.Cil.OpCodes.Add_Ovf);
		Add_Ovf_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Add_Ovf_Un);
		Mul_Ovf = new OpCode(Mono.Cecil.Cil.OpCodes.Mul_Ovf);
		Mul_Ovf_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Mul_Ovf_Un);
		Sub_Ovf = new OpCode(Mono.Cecil.Cil.OpCodes.Sub_Ovf);
		Sub_Ovf_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Sub_Ovf_Un);
		Endfinally = new OpCode(Mono.Cecil.Cil.OpCodes.Endfinally);
		Leave = new OpCode(Mono.Cecil.Cil.OpCodes.Leave);
		Leave_S = new OpCode(Mono.Cecil.Cil.OpCodes.Leave_S);
		Stind_I = new OpCode(Mono.Cecil.Cil.OpCodes.Stind_I);
		Conv_U = new OpCode(Mono.Cecil.Cil.OpCodes.Conv_U);
		Arglist = new OpCode(Mono.Cecil.Cil.OpCodes.Arglist);
		Ceq = new OpCode(Mono.Cecil.Cil.OpCodes.Ceq);
		Cgt = new OpCode(Mono.Cecil.Cil.OpCodes.Cgt);
		Cgt_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Cgt_Un);
		Clt = new OpCode(Mono.Cecil.Cil.OpCodes.Clt);
		Clt_Un = new OpCode(Mono.Cecil.Cil.OpCodes.Clt_Un);
		Ldftn = new OpCode(Mono.Cecil.Cil.OpCodes.Ldftn);
		Ldvirtftn = new OpCode(Mono.Cecil.Cil.OpCodes.Ldvirtftn);
		Ldarg = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarg);
		Ldarga = new OpCode(Mono.Cecil.Cil.OpCodes.Ldarga);
		Starg = new OpCode(Mono.Cecil.Cil.OpCodes.Starg);
		Ldloc = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloc);
		Ldloca = new OpCode(Mono.Cecil.Cil.OpCodes.Ldloca);
		Stloc = new OpCode(Mono.Cecil.Cil.OpCodes.Stloc);
		Localloc = new OpCode(Mono.Cecil.Cil.OpCodes.Localloc);
		Endfilter = new OpCode(Mono.Cecil.Cil.OpCodes.Endfilter);
		Unaligned = new OpCode(Mono.Cecil.Cil.OpCodes.Unaligned);
		Volatile = new OpCode(Mono.Cecil.Cil.OpCodes.Volatile);
		Tail = new OpCode(Mono.Cecil.Cil.OpCodes.Tail);
		Initobj = new OpCode(Mono.Cecil.Cil.OpCodes.Initobj);
		Constrained = new OpCode(Mono.Cecil.Cil.OpCodes.Constrained);
		Cpblk = new OpCode(Mono.Cecil.Cil.OpCodes.Cpblk);
		Initblk = new OpCode(Mono.Cecil.Cil.OpCodes.Initblk);
		No = new OpCode(Mono.Cecil.Cil.OpCodes.No);
		Rethrow = new OpCode(Mono.Cecil.Cil.OpCodes.Rethrow);
		Sizeof = new OpCode(Mono.Cecil.Cil.OpCodes.Sizeof);
		Refanytype = new OpCode(Mono.Cecil.Cil.OpCodes.Refanytype);
		Readonly = new OpCode(Mono.Cecil.Cil.OpCodes.Readonly);
		_opCodeMap = new Dictionary<Mono.Cecil.Cil.OpCode, OpCode>
		{
			{
				Mono.Cecil.Cil.OpCodes.Nop,
				Nop
			},
			{
				Mono.Cecil.Cil.OpCodes.Break,
				Break
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg_0,
				Ldarg_0
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg_1,
				Ldarg_1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg_2,
				Ldarg_2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg_3,
				Ldarg_3
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc_0,
				Ldloc_0
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc_1,
				Ldloc_1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc_2,
				Ldloc_2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc_3,
				Ldloc_3
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc_0,
				Stloc_0
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc_1,
				Stloc_1
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc_2,
				Stloc_2
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc_3,
				Stloc_3
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg_S,
				Ldarg_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarga_S,
				Ldarga_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Starg_S,
				Starg_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc_S,
				Ldloc_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloca_S,
				Ldloca_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc_S,
				Stloc_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldnull,
				Ldnull
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_M1,
				Ldc_I4_M1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_0,
				Ldc_I4_0
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_1,
				Ldc_I4_1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_2,
				Ldc_I4_2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_3,
				Ldc_I4_3
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_4,
				Ldc_I4_4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_5,
				Ldc_I4_5
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_6,
				Ldc_I4_6
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_7,
				Ldc_I4_7
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_8,
				Ldc_I4_8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4_S,
				Ldc_I4_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I4,
				Ldc_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_I8,
				Ldc_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_R4,
				Ldc_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldc_R8,
				Ldc_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Dup,
				Dup
			},
			{
				Mono.Cecil.Cil.OpCodes.Pop,
				Pop
			},
			{
				Mono.Cecil.Cil.OpCodes.Jmp,
				Jmp
			},
			{
				Mono.Cecil.Cil.OpCodes.Call,
				Call
			},
			{
				Mono.Cecil.Cil.OpCodes.Calli,
				Calli
			},
			{
				Mono.Cecil.Cil.OpCodes.Ret,
				Ret
			},
			{
				Mono.Cecil.Cil.OpCodes.Br_S,
				Br_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Brfalse_S,
				Brfalse_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Brtrue_S,
				Brtrue_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Beq_S,
				Beq_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Bge_S,
				Bge_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Bgt_S,
				Bgt_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ble_S,
				Ble_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Blt_S,
				Blt_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Bne_Un_S,
				Bne_Un_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Bge_Un_S,
				Bge_Un_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Bgt_Un_S,
				Bgt_Un_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Ble_Un_S,
				Ble_Un_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Blt_Un_S,
				Blt_Un_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Br,
				Br
			},
			{
				Mono.Cecil.Cil.OpCodes.Brfalse,
				Brfalse
			},
			{
				Mono.Cecil.Cil.OpCodes.Brtrue,
				Brtrue
			},
			{
				Mono.Cecil.Cil.OpCodes.Beq,
				Beq
			},
			{
				Mono.Cecil.Cil.OpCodes.Bge,
				Bge
			},
			{
				Mono.Cecil.Cil.OpCodes.Bgt,
				Bgt
			},
			{
				Mono.Cecil.Cil.OpCodes.Ble,
				Ble
			},
			{
				Mono.Cecil.Cil.OpCodes.Blt,
				Blt
			},
			{
				Mono.Cecil.Cil.OpCodes.Bne_Un,
				Bne_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Bge_Un,
				Bge_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Bgt_Un,
				Bgt_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Ble_Un,
				Ble_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Blt_Un,
				Blt_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Switch,
				Switch
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_I1,
				Ldind_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_U1,
				Ldind_U1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_I2,
				Ldind_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_U2,
				Ldind_U2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_I4,
				Ldind_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_U4,
				Ldind_U4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_I8,
				Ldind_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_I,
				Ldind_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_R4,
				Ldind_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_R8,
				Ldind_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldind_Ref,
				Ldind_Ref
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_Ref,
				Stind_Ref
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_I1,
				Stind_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_I2,
				Stind_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_I4,
				Stind_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_I8,
				Stind_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_R4,
				Stind_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_R8,
				Stind_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Add,
				Add
			},
			{
				Mono.Cecil.Cil.OpCodes.Sub,
				Sub
			},
			{
				Mono.Cecil.Cil.OpCodes.Mul,
				Mul
			},
			{
				Mono.Cecil.Cil.OpCodes.Div,
				Div
			},
			{
				Mono.Cecil.Cil.OpCodes.Div_Un,
				Div_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Rem,
				Rem
			},
			{
				Mono.Cecil.Cil.OpCodes.Rem_Un,
				Rem_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.And,
				And
			},
			{
				Mono.Cecil.Cil.OpCodes.Or,
				Or
			},
			{
				Mono.Cecil.Cil.OpCodes.Xor,
				Xor
			},
			{
				Mono.Cecil.Cil.OpCodes.Shl,
				Shl
			},
			{
				Mono.Cecil.Cil.OpCodes.Shr,
				Shr
			},
			{
				Mono.Cecil.Cil.OpCodes.Shr_Un,
				Shr_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Neg,
				Neg
			},
			{
				Mono.Cecil.Cil.OpCodes.Not,
				Not
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_I1,
				Conv_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_I2,
				Conv_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_I4,
				Conv_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_I8,
				Conv_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_R4,
				Conv_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_R8,
				Conv_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_U4,
				Conv_U4
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_U8,
				Conv_U8
			},
			{
				Mono.Cecil.Cil.OpCodes.Callvirt,
				Callvirt
			},
			{
				Mono.Cecil.Cil.OpCodes.Cpobj,
				Cpobj
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldobj,
				Ldobj
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldstr,
				Ldstr
			},
			{
				Mono.Cecil.Cil.OpCodes.Newobj,
				Newobj
			},
			{
				Mono.Cecil.Cil.OpCodes.Castclass,
				Castclass
			},
			{
				Mono.Cecil.Cil.OpCodes.Isinst,
				Isinst
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_R_Un,
				Conv_R_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Unbox,
				Unbox
			},
			{
				Mono.Cecil.Cil.OpCodes.Throw,
				Throw
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldfld,
				Ldfld
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldflda,
				Ldflda
			},
			{
				Mono.Cecil.Cil.OpCodes.Stfld,
				Stfld
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldsfld,
				Ldsfld
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldsflda,
				Ldsflda
			},
			{
				Mono.Cecil.Cil.OpCodes.Stsfld,
				Stsfld
			},
			{
				Mono.Cecil.Cil.OpCodes.Stobj,
				Stobj
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I1_Un,
				Conv_Ovf_I1_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I2_Un,
				Conv_Ovf_I2_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I4_Un,
				Conv_Ovf_I4_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I8_Un,
				Conv_Ovf_I8_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U1_Un,
				Conv_Ovf_U1_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U2_Un,
				Conv_Ovf_U2_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U4_Un,
				Conv_Ovf_U4_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U8_Un,
				Conv_Ovf_U8_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I_Un,
				Conv_Ovf_I_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U_Un,
				Conv_Ovf_U_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Box,
				Box
			},
			{
				Mono.Cecil.Cil.OpCodes.Newarr,
				Newarr
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldlen,
				Ldlen
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelema,
				Ldelema
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_I1,
				Ldelem_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_U1,
				Ldelem_U1
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_I2,
				Ldelem_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_U2,
				Ldelem_U2
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_I4,
				Ldelem_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_U4,
				Ldelem_U4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_I8,
				Ldelem_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_I,
				Ldelem_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_R4,
				Ldelem_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_R8,
				Ldelem_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_Ref,
				Ldelem_Ref
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_I,
				Stelem_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_I1,
				Stelem_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_I2,
				Stelem_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_I4,
				Stelem_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_I8,
				Stelem_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_R4,
				Stelem_R4
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_R8,
				Stelem_R8
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_Ref,
				Stelem_Ref
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldelem_Any,
				Ldelem_Any
			},
			{
				Mono.Cecil.Cil.OpCodes.Stelem_Any,
				Stelem_Any
			},
			{
				Mono.Cecil.Cil.OpCodes.Unbox_Any,
				Unbox_Any
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I1,
				Conv_Ovf_I1
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U1,
				Conv_Ovf_U1
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I2,
				Conv_Ovf_I2
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U2,
				Conv_Ovf_U2
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I4,
				Conv_Ovf_I4
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U4,
				Conv_Ovf_U4
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I8,
				Conv_Ovf_I8
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U8,
				Conv_Ovf_U8
			},
			{
				Mono.Cecil.Cil.OpCodes.Refanyval,
				Refanyval
			},
			{
				Mono.Cecil.Cil.OpCodes.Ckfinite,
				Ckfinite
			},
			{
				Mono.Cecil.Cil.OpCodes.Mkrefany,
				Mkrefany
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldtoken,
				Ldtoken
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_U2,
				Conv_U2
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_U1,
				Conv_U1
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_I,
				Conv_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_I,
				Conv_Ovf_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_Ovf_U,
				Conv_Ovf_U
			},
			{
				Mono.Cecil.Cil.OpCodes.Add_Ovf,
				Add_Ovf
			},
			{
				Mono.Cecil.Cil.OpCodes.Add_Ovf_Un,
				Add_Ovf_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Mul_Ovf,
				Mul_Ovf
			},
			{
				Mono.Cecil.Cil.OpCodes.Mul_Ovf_Un,
				Mul_Ovf_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Sub_Ovf,
				Sub_Ovf
			},
			{
				Mono.Cecil.Cil.OpCodes.Sub_Ovf_Un,
				Sub_Ovf_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Endfinally,
				Endfinally
			},
			{
				Mono.Cecil.Cil.OpCodes.Leave,
				Leave
			},
			{
				Mono.Cecil.Cil.OpCodes.Leave_S,
				Leave_S
			},
			{
				Mono.Cecil.Cil.OpCodes.Stind_I,
				Stind_I
			},
			{
				Mono.Cecil.Cil.OpCodes.Conv_U,
				Conv_U
			},
			{
				Mono.Cecil.Cil.OpCodes.Arglist,
				Arglist
			},
			{
				Mono.Cecil.Cil.OpCodes.Ceq,
				Ceq
			},
			{
				Mono.Cecil.Cil.OpCodes.Cgt,
				Cgt
			},
			{
				Mono.Cecil.Cil.OpCodes.Cgt_Un,
				Cgt_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Clt,
				Clt
			},
			{
				Mono.Cecil.Cil.OpCodes.Clt_Un,
				Clt_Un
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldftn,
				Ldftn
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldvirtftn,
				Ldvirtftn
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarg,
				Ldarg
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldarga,
				Ldarga
			},
			{
				Mono.Cecil.Cil.OpCodes.Starg,
				Starg
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloc,
				Ldloc
			},
			{
				Mono.Cecil.Cil.OpCodes.Ldloca,
				Ldloca
			},
			{
				Mono.Cecil.Cil.OpCodes.Stloc,
				Stloc
			},
			{
				Mono.Cecil.Cil.OpCodes.Localloc,
				Localloc
			},
			{
				Mono.Cecil.Cil.OpCodes.Endfilter,
				Endfilter
			},
			{
				Mono.Cecil.Cil.OpCodes.Unaligned,
				Unaligned
			},
			{
				Mono.Cecil.Cil.OpCodes.Volatile,
				Volatile
			},
			{
				Mono.Cecil.Cil.OpCodes.Tail,
				Tail
			},
			{
				Mono.Cecil.Cil.OpCodes.Initobj,
				Initobj
			},
			{
				Mono.Cecil.Cil.OpCodes.Constrained,
				Constrained
			},
			{
				Mono.Cecil.Cil.OpCodes.Cpblk,
				Cpblk
			},
			{
				Mono.Cecil.Cil.OpCodes.Initblk,
				Initblk
			},
			{
				Mono.Cecil.Cil.OpCodes.No,
				No
			},
			{
				Mono.Cecil.Cil.OpCodes.Rethrow,
				Rethrow
			},
			{
				Mono.Cecil.Cil.OpCodes.Sizeof,
				Sizeof
			},
			{
				Mono.Cecil.Cil.OpCodes.Refanytype,
				Refanytype
			},
			{
				Mono.Cecil.Cil.OpCodes.Readonly,
				Readonly
			}
		}.AsReadOnly();
	}
}
