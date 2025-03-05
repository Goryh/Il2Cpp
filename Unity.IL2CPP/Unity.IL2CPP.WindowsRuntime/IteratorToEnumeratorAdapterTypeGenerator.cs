using System;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class IteratorToEnumeratorAdapterTypeGenerator
{
	private const TypeAttributes AdapterTypeAttributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

	private readonly MinimalContext _context;

	private readonly EditContext _editContext;

	private readonly TypeDefinition _adapterType;

	private readonly TypeReference _iteratorType;

	private readonly TypeReference _ienumeratorType;

	private readonly TypeResolver _typeResolver;

	private readonly TypeReference _currentFieldType;

	private readonly MethodReference _getCurrentMethod;

	private readonly MethodReference _getHasCurrentMethod;

	private readonly MethodReference _moveNextMethod;

	private readonly MethodReference _invalidOperationExceptionConstructor;

	public IteratorToEnumeratorAdapterTypeGenerator(MinimalContext context, EditContext editContext, TypeDefinition iteratorType, TypeDefinition ienumeratorType)
	{
		_context = context;
		_editContext = editContext;
		_currentFieldType = context.Global.Services.TypeProvider.ObjectTypeReference;
		_iteratorType = iteratorType;
		_ienumeratorType = ienumeratorType;
		string adapterTypeName = context.Global.Services.Naming.ForWindowsRuntimeAdapterTypeName(iteratorType, ienumeratorType);
		if (ienumeratorType.HasGenericParameters)
		{
			_adapterType = editContext.BuildClass("System.Runtime.InteropServices.WindowsRuntime", adapterTypeName, TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit).CloneGenericParameters(ienumeratorType).Complete();
			GenericInstanceType iteratorInstance = context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(iteratorType, _adapterType.GenericParameters);
			GenericInstanceType ienumeratorInstance = context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(ienumeratorType, _adapterType.GenericParameters);
			_iteratorType = iteratorInstance;
			_ienumeratorType = ienumeratorInstance;
			_currentFieldType = _adapterType.GenericParameters[0];
		}
		else
		{
			_adapterType = editContext.BuildClass("System.Runtime.InteropServices.WindowsRuntime", adapterTypeName, TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit).Complete();
		}
		_typeResolver = context.Global.Services.TypeFactory.ResolverFor(_iteratorType);
		_getCurrentMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "get_Current"));
		_getHasCurrentMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "get_HasCurrent"));
		_moveNextMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "MoveNext"));
		TypeDefinition invalidOperationExceptionType = context.Global.Services.TypeProvider.GetSystemType(SystemType.InvalidOperationException);
		_invalidOperationExceptionConstructor = invalidOperationExceptionType.Methods.First((MethodDefinition m) => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
	}

	public TypeDefinition Generate()
	{
		_editContext.AddInterfaceImplementationToType(_adapterType, _ienumeratorType);
		FieldReference iteratorField = _typeResolver.Resolve(_editContext.BuildField("iterator", FieldAttributes.Private, _iteratorType).Complete(_adapterType));
		FieldReference initializedField = _typeResolver.Resolve(_editContext.BuildField("initialized", FieldAttributes.Private, _context.Global.Services.TypeProvider.BoolTypeReference).Complete(_adapterType));
		FieldReference hadCurrentField = _typeResolver.Resolve(_editContext.BuildField("hadCurrent", FieldAttributes.Private, _context.Global.Services.TypeProvider.BoolTypeReference).Complete(_adapterType));
		FieldReference currentField = _typeResolver.Resolve(_editContext.BuildField("current", FieldAttributes.Private, _currentFieldType).Complete(_adapterType));
		foreach (MethodDefinition method in _adapterType.Methods)
		{
			_editContext.ChangeAttributes(method, method.Attributes & ~MethodAttributes.Abstract);
			switch (method.Name)
			{
			case "System.Collections.IEnumerator.MoveNext":
				WriteMethodMoveNext(method, iteratorField, initializedField, hadCurrentField, currentField);
				continue;
			case "System.Collections.IEnumerator.get_Current":
			case "System.Collections.Generic.IEnumerator`1.get_Current":
				WriteMethodGetCurrent(method, initializedField, hadCurrentField, currentField);
				continue;
			case "System.Collections.IEnumerator.Reset":
				WriteMethodReset(method);
				continue;
			case "System.IDisposable.Dispose":
				WriteDisposeMethod(method);
				continue;
			}
			throw new NotSupportedException($"Interface '{_ienumeratorType.FullName}' contains unsupported method '{method.Name}'.");
		}
		MethodDefinition constructor = _editContext.BuildMethod(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName).AddParameter("iterator", ParameterAttributes.None, _iteratorType).WithEmptyBody()
			.Complete(_adapterType);
		WriteConstructor(constructor, iteratorField, hadCurrentField);
		return _adapterType;
	}

	private void WriteConstructor(MethodDefinition method, FieldReference iteratorField, FieldReference hadCurrentField)
	{
		MethodBody body = method.Body;
		ILProcessor iLProcessor = body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(method: _context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.IsConstructor && !m.IsStatic && !m.HasParameters), opcode: OpCodes.Call);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Stfld, iteratorField);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldc_I4_1);
		iLProcessor.Emit(OpCodes.Stfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Ret);
		body.OptimizeMacros();
	}

	private void WriteMethodMoveNext(MethodDefinition method, FieldReference iteratorField, FieldReference initializedField, FieldReference hadCurrentField, FieldReference currentField)
	{
		MethodBody body = method.Body;
		ILProcessor iLProcessor = body.GetILProcessor();
		Instruction dummyTargetInstruction = Instruction.Create(OpCodes.Nop);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Brtrue_S, dummyTargetInstruction);
		Instruction hadCurrentBranchInstruction = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldc_I4_0);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		Instruction tryStart = body.Instructions.Last();
		_editContext.ChangeInstructionOperand(hadCurrentBranchInstruction, tryStart);
		iLProcessor.Emit(OpCodes.Ldfld, initializedField);
		iLProcessor.Emit(OpCodes.Brtrue_S, dummyTargetInstruction);
		Instruction skipInitializeBranchInstruction = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, iteratorField);
		iLProcessor.Emit(OpCodes.Callvirt, _getHasCurrentMethod);
		iLProcessor.Emit(OpCodes.Stfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldc_I4_1);
		iLProcessor.Emit(OpCodes.Stfld, initializedField);
		iLProcessor.Emit(OpCodes.Br_S, dummyTargetInstruction);
		Instruction skipMoveNextBranchInstruction = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		_editContext.ChangeInstructionOperand(skipInitializeBranchInstruction, body.Instructions.Last());
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, iteratorField);
		iLProcessor.Emit(OpCodes.Callvirt, _moveNextMethod);
		iLProcessor.Emit(OpCodes.Stfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		_editContext.ChangeInstructionOperand(skipMoveNextBranchInstruction, body.Instructions.Last());
		iLProcessor.Emit(OpCodes.Ldfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Brfalse_S, dummyTargetInstruction);
		Instruction skipGetCurrentBranchInstruction = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, iteratorField);
		iLProcessor.Emit(OpCodes.Callvirt, _getCurrentMethod);
		iLProcessor.Emit(OpCodes.Stfld, currentField);
		iLProcessor.Emit(OpCodes.Leave_S, dummyTargetInstruction);
		Instruction leaveInstruction = body.Instructions.Last();
		_editContext.ChangeInstructionOperand(skipGetCurrentBranchInstruction, leaveInstruction);
		iLProcessor.Emit(method: _context.Global.Services.TypeProvider.GetSystemType(SystemType.Marshal).Methods.Single((MethodDefinition m) => m.Name == "GetHRForException"), opcode: OpCodes.Call);
		Instruction tryEnd = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldc_I4, -2147483636);
		iLProcessor.Emit(OpCodes.Bne_Un_S, dummyTargetInstruction);
		Instruction skipThrowBranchInstruction = body.Instructions.Last();
		iLProcessor.Emit(OpCodes.Ldstr, "Collection was modified; enumeration operation may not execute.");
		iLProcessor.Emit(OpCodes.Newobj, _invalidOperationExceptionConstructor);
		iLProcessor.Emit(OpCodes.Throw);
		iLProcessor.Emit(OpCodes.Rethrow);
		_editContext.ChangeInstructionOperand(skipThrowBranchInstruction, body.Instructions.Last());
		iLProcessor.Emit(OpCodes.Ldarg_0);
		Instruction handlerEnd = body.Instructions.Last();
		_editContext.ChangeInstructionOperand(leaveInstruction, handlerEnd);
		iLProcessor.Emit(OpCodes.Ldfld, hadCurrentField);
		iLProcessor.Emit(OpCodes.Ret);
		_editContext.AddExceptionHandlerToMethod(method, _context.Global.Services.TypeProvider.GetSystemType(SystemType.Exception), ExceptionHandlerType.Catch, tryStart, tryEnd, null, tryEnd, handlerEnd);
		body.OptimizeMacros();
	}

	private void WriteMethodGetCurrent(MethodDefinition method, FieldReference initializedField, FieldReference hadCurrentField, FieldReference currentField)
	{
		MethodBody body = method.Body;
		ILProcessor processor = body.GetILProcessor();
		Instruction dummyTargetInstruction = Instruction.Create(OpCodes.Nop);
		processor.Emit(OpCodes.Ldarg_0);
		processor.Emit(OpCodes.Ldfld, initializedField);
		processor.Emit(OpCodes.Brtrue_S, dummyTargetInstruction);
		Instruction initializedBranchInstruction = body.Instructions.Last();
		processor.Emit(OpCodes.Ldstr, "Enumeration has not started. Call MoveNext.");
		processor.Emit(OpCodes.Newobj, _invalidOperationExceptionConstructor);
		processor.Emit(OpCodes.Throw);
		processor.Emit(OpCodes.Ldarg_0);
		_editContext.ChangeInstructionOperand(initializedBranchInstruction, body.Instructions.Last());
		processor.Emit(OpCodes.Ldfld, hadCurrentField);
		processor.Emit(OpCodes.Brtrue_S, dummyTargetInstruction);
		Instruction hadCurrentBranchInstruction = body.Instructions.Last();
		processor.Emit(OpCodes.Ldstr, "Enumeration already finished.");
		processor.Emit(OpCodes.Newobj, _invalidOperationExceptionConstructor);
		processor.Emit(OpCodes.Throw);
		processor.Emit(OpCodes.Ldarg_0);
		_editContext.ChangeInstructionOperand(hadCurrentBranchInstruction, body.Instructions.Last());
		processor.Emit(OpCodes.Ldfld, currentField);
		if (currentField.FieldType.IsGenericParameter && method.ReturnType.MetadataType == MetadataType.Object)
		{
			processor.Emit(OpCodes.Box, currentField.FieldType);
		}
		processor.Emit(OpCodes.Ret);
		body.OptimizeMacros();
	}

	private void WriteMethodReset(MethodDefinition method)
	{
		MethodBody body = method.Body;
		ILProcessor iLProcessor = body.GetILProcessor();
		iLProcessor.Emit(method: _context.Global.Services.TypeProvider.GetSystemType(SystemType.NotSupportedException).Methods.First((MethodDefinition m) => m.IsConstructor && !m.IsStatic && !m.HasParameters), opcode: OpCodes.Newobj);
		iLProcessor.Emit(OpCodes.Throw);
		body.OptimizeMacros();
	}

	private void WriteDisposeMethod(MethodDefinition method)
	{
		MethodBody body = method.Body;
		body.GetILProcessor().Emit(OpCodes.Ret);
		body.OptimizeMacros();
	}
}
