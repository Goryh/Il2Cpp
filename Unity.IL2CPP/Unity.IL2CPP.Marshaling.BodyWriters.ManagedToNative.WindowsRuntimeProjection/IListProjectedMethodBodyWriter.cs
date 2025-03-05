using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

internal sealed class IListProjectedMethodBodyWriter
{
	private readonly MinimalContext _context;

	private readonly TypeDefinition _iVectorType;

	private readonly EditContext _typeEditContext;

	public IListProjectedMethodBodyWriter(MinimalContext context, EditContext typeEditContext, TypeDefinition iVectorType)
	{
		_context = context;
		_iVectorType = iVectorType;
		_typeEditContext = typeEditContext;
	}

	public void WriteGetItem(MethodDefinition method)
	{
		MethodDefinition getAtMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "GetAt");
		WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], getAtMethod);
	}

	public void WriteIndexOf(MethodDefinition method)
	{
		MethodReference indexOfMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
		if (_iVectorType.HasGenericParameters)
		{
			GenericInstanceType vectorViewInstance = _context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(_iVectorType, method.DeclaringType.GenericParameters[0]);
			indexOfMethod = _context.Global.Services.TypeFactory.ResolverFor(vectorViewInstance).Resolve(indexOfMethod);
		}
		MethodDefinition invalidOperationExceptionCtor = _context.Global.Services.TypeProvider.GetSystemType(SystemType.InvalidOperationException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		VariableDefinition indexVariable = _typeEditContext.AddVariableToMethod(method, _context.Global.Services.TypeProvider.UInt32TypeReference);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		Instruction checkIndex = iLProcessor.Create(OpCodes.Nop);
		Instruction throwException = iLProcessor.Create(OpCodes.Nop);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Ldloca, indexVariable);
		iLProcessor.Emit(OpCodes.Callvirt, indexOfMethod);
		iLProcessor.Emit(OpCodes.Brtrue, checkIndex);
		iLProcessor.Emit(OpCodes.Ldc_I4_M1);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(checkIndex);
		iLProcessor.Emit(OpCodes.Ldloc, indexVariable);
		iLProcessor.Emit(OpCodes.Ldc_I4, int.MaxValue);
		iLProcessor.Emit(OpCodes.Bgt_Un, throwException);
		iLProcessor.Emit(OpCodes.Ldloc, indexVariable);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(throwException);
		iLProcessor.Emit(OpCodes.Ldstr, "The backing collection is too large.");
		iLProcessor.Emit(OpCodes.Newobj, invalidOperationExceptionCtor);
		iLProcessor.Emit(OpCodes.Throw);
	}

	public void WriteInsert(MethodDefinition method)
	{
		MethodDefinition insertAtMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "InsertAt");
		WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], insertAtMethod);
	}

	public void WriteRemoveAt(MethodDefinition method)
	{
		MethodDefinition removeAtMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "RemoveAt");
		WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], removeAtMethod);
	}

	public void WriteSetItem(MethodDefinition method)
	{
		MethodDefinition setAtMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "SetAt");
		WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], setAtMethod);
	}

	private void WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(MethodDefinition currentMethod, ParameterDefinition indexParameter, MethodDefinition vectorMethod)
	{
		MethodReference vectorMethodInstance;
		if (_iVectorType.HasGenericParameters)
		{
			GenericInstanceType vectorViewInstance = _context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(_iVectorType, currentMethod.DeclaringType.GenericParameters[0]);
			vectorMethodInstance = _context.Global.Services.TypeFactory.ResolverFor(vectorViewInstance).Resolve(vectorMethod);
		}
		else
		{
			vectorMethodInstance = vectorMethod;
		}
		PropertyDefinition hresultProperty = _context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
		MethodDefinition argumentOutOfRangeExceptionCtor = _context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentOutOfRangeException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		ILProcessor ilProcessor = currentMethod.Body.GetILProcessor();
		Instruction throwLabel = ilProcessor.Create(OpCodes.Nop);
		ilProcessor.Emit(OpCodes.Ldarg, indexParameter.Index + 1);
		ilProcessor.Emit(OpCodes.Ldc_I4_0);
		ilProcessor.Emit(OpCodes.Blt, throwLabel);
		Instruction loadThis = ilProcessor.Create(OpCodes.Ldarg_0);
		ilProcessor.Append(loadThis);
		for (int i = 0; i < vectorMethod.Parameters.Count; i++)
		{
			ilProcessor.Emit(OpCodes.Ldarg, i + 1);
		}
		ilProcessor.Emit(OpCodes.Callvirt, vectorMethodInstance);
		ilProcessor.Emit(OpCodes.Ret);
		Instruction getHResult = ilProcessor.Create(OpCodes.Call, hresultProperty.GetMethod);
		ilProcessor.Append(getHResult);
		ilProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
		ilProcessor.Emit(OpCodes.Beq, throwLabel);
		ilProcessor.Emit(OpCodes.Rethrow);
		ilProcessor.Append(throwLabel);
		ilProcessor.Emit(OpCodes.Ldstr, "index");
		ilProcessor.Emit(OpCodes.Newobj, argumentOutOfRangeExceptionCtor);
		ilProcessor.Emit(OpCodes.Throw);
		_typeEditContext.AddExceptionHandlerToMethod(currentMethod, _context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Catch, loadThis, getHResult, null, getHResult, throwLabel);
	}
}
