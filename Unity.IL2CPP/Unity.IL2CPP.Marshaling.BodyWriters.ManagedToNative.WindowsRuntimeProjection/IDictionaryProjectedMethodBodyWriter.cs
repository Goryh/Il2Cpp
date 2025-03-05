using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

internal sealed class IDictionaryProjectedMethodBodyWriter
{
	private readonly MinimalContext _context;

	private readonly TypeDefinition _iDictionaryTypeDef;

	private readonly TypeDefinition _iMapTypeDef;

	private readonly TypeResolver _iDictionaryInstanceTypeResolver;

	private readonly TypeResolver _iMapInstanceTypeResolver;

	private readonly MethodDefinition _argumentNullExceptionConstructor;

	private readonly MethodDefinition _argumentExceptionConstructor;

	private readonly MethodDefinition _hresultGetter;

	private readonly EditContext _typeEditContext;

	public IDictionaryProjectedMethodBodyWriter(MinimalContext context, EditContext typeEditContext, TypeDefinition iDictionaryTypeDef, TypeDefinition iMapTypeDef)
	{
		_context = context;
		_typeEditContext = typeEditContext;
		_iDictionaryTypeDef = iDictionaryTypeDef;
		_iMapTypeDef = iMapTypeDef;
		IDataModelService typeFactory = context.Global.Services.TypeFactory;
		TypeDefinition iDictionaryTypeDef2 = _iDictionaryTypeDef;
		TypeReference declaringType = _iDictionaryTypeDef.DeclaringType;
		TypeReference[] genericArguments = _iDictionaryTypeDef.GenericParameters.ToArray();
		GenericInstanceType iDictionaryInstance = typeFactory.CreateGenericInstanceType(iDictionaryTypeDef2, declaringType, genericArguments);
		_iDictionaryInstanceTypeResolver = typeFactory.ResolverFor(iDictionaryInstance);
		TypeDefinition iMapTypeDef2 = _iMapTypeDef;
		TypeReference declaringType2 = _iMapTypeDef.DeclaringType;
		genericArguments = _iDictionaryTypeDef.GenericParameters.ToArray();
		GenericInstanceType iMapInstance = typeFactory.CreateGenericInstanceType(iMapTypeDef2, declaringType2, genericArguments);
		_iMapInstanceTypeResolver = typeFactory.ResolverFor(iMapInstance);
		TypeDefinition argumentNullException = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentNullException);
		_argumentNullExceptionConstructor = argumentNullException.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		TypeDefinition argumentException = context.Global.Services.TypeProvider.GetSystemType(SystemType.ArgumentException);
		_argumentExceptionConstructor = argumentException.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		PropertyDefinition hresultProperty = context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
		_hresultGetter = hresultProperty.GetMethod;
	}

	public void WriteGetKeys(MethodDefinition method)
	{
		WriteGetReadOnlyCollection(method, DictionaryCollectionTypesGenerator.CollectionKind.Key);
	}

	public void WriteGetValues(MethodDefinition method)
	{
		WriteGetReadOnlyCollection(method, DictionaryCollectionTypesGenerator.CollectionKind.Value);
	}

	public void WriteContainsKey(MethodDefinition method)
	{
		MethodDefinition hasKeyMethodDef = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "HasKey");
		MethodReference hasKeyMethod = _iMapInstanceTypeResolver.Resolve(hasKeyMethodDef);
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		Instruction loadThisInstruction = ilProcessor.Create(OpCodes.Ldarg_0);
		WriteKeyNullCheck(ilProcessor, loadThisInstruction, 0);
		ilProcessor.Append(loadThisInstruction);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Callvirt, hasKeyMethod);
		ilProcessor.Emit(OpCodes.Ret);
	}

	public void WriteGetItem(MethodDefinition method)
	{
		MethodDefinition lookupMethodDef = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Lookup");
		MethodReference lookupMethod = _iMapInstanceTypeResolver.Resolve(lookupMethodDef);
		MethodDefinition keyNotFoundExceptionCtor = _context.Global.Services.TypeProvider.GetSystemType(SystemType.KeyNotFoundException).Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		Instruction loadThisInstruction = ilProcessor.Create(OpCodes.Ldarg_0);
		WriteKeyNullCheck(ilProcessor, loadThisInstruction, 0);
		ilProcessor.Append(loadThisInstruction);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Callvirt, lookupMethod);
		ilProcessor.Emit(OpCodes.Ret);
		Instruction getHResult = ilProcessor.Create(OpCodes.Call, _hresultGetter);
		Instruction throwKeyNotFoundException = ilProcessor.Create(OpCodes.Ldstr, "The given key was not present in the dictionary.");
		ilProcessor.Append(getHResult);
		ilProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
		ilProcessor.Emit(OpCodes.Beq, throwKeyNotFoundException);
		ilProcessor.Emit(OpCodes.Rethrow);
		ilProcessor.Append(throwKeyNotFoundException);
		ilProcessor.Emit(OpCodes.Newobj, keyNotFoundExceptionCtor);
		ilProcessor.Emit(OpCodes.Throw);
		_typeEditContext.AddExceptionHandlerToMethod(method, _context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Catch, loadThisInstruction, getHResult, null, getHResult, throwKeyNotFoundException);
	}

	public void WriteTryGetValue(MethodDefinition method)
	{
		MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
		MethodDefinition lookupMethodDef = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Lookup");
		MethodReference lookupMethod = _iMapInstanceTypeResolver.Resolve(lookupMethodDef);
		GenericParameter valueType = _iDictionaryTypeDef.GenericParameters[1];
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
		Instruction tryLookupLabel = iLProcessor.Create(OpCodes.Ldarg_2);
		iLProcessor.Emit(OpCodes.Brtrue, tryLookupLabel);
		Instruction keyNotFoundLabel = iLProcessor.Create(OpCodes.Ldarg_2);
		iLProcessor.Append(keyNotFoundLabel);
		iLProcessor.Emit(OpCodes.Initobj, valueType);
		iLProcessor.Emit(OpCodes.Ldc_I4_0);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(tryLookupLabel);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Callvirt, lookupMethod);
		iLProcessor.Emit(OpCodes.Stobj, valueType);
		iLProcessor.Emit(OpCodes.Ldc_I4_1);
		iLProcessor.Emit(OpCodes.Ret);
		Instruction catchStartLabel = iLProcessor.Create(OpCodes.Call, _hresultGetter);
		iLProcessor.Append(catchStartLabel);
		iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
		iLProcessor.Emit(OpCodes.Beq, keyNotFoundLabel);
		iLProcessor.Emit(OpCodes.Rethrow);
		_typeEditContext.AddExceptionHandlerToMethod(method, _context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Catch, tryLookupLabel, catchStartLabel, null, catchStartLabel, null);
	}

	public void WriteAdd(MethodDefinition method)
	{
		MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		MethodDefinition insertMethodDef = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Insert");
		MethodReference insertMethod = _iMapInstanceTypeResolver.Resolve(insertMethodDef);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
		Instruction loadThisInstruction = iLProcessor.Create(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Brfalse, loadThisInstruction);
		iLProcessor.Emit(OpCodes.Ldstr, "An item with the same key has already been added.");
		iLProcessor.Emit(OpCodes.Newobj, _argumentExceptionConstructor);
		iLProcessor.Emit(OpCodes.Throw);
		iLProcessor.Append(loadThisInstruction);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Ldarg_2);
		iLProcessor.Emit(OpCodes.Callvirt, insertMethod);
		iLProcessor.Emit(OpCodes.Pop);
		iLProcessor.Emit(OpCodes.Ret);
	}

	internal void WriteRemove(MethodDefinition method)
	{
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
		MethodReference mapRemoveMethod = _iMapInstanceTypeResolver.Resolve(_iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Remove"));
		Instruction loadThisInstruction = iLProcessor.Create(OpCodes.Ldarg_0);
		Instruction removeKey = iLProcessor.Create(OpCodes.Nop);
		Instruction checkHResult = iLProcessor.Create(OpCodes.Nop);
		Instruction rethrowException = iLProcessor.Create(OpCodes.Nop);
		iLProcessor.Append(loadThisInstruction);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
		iLProcessor.Emit(OpCodes.Brtrue, removeKey);
		iLProcessor.Emit(OpCodes.Ldc_I4_0);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(removeKey);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Callvirt, mapRemoveMethod);
		iLProcessor.Emit(OpCodes.Ldc_I4_1);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(checkHResult);
		iLProcessor.Emit(OpCodes.Call, _hresultGetter);
		iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
		iLProcessor.Emit(OpCodes.Bne_Un, rethrowException);
		iLProcessor.Emit(OpCodes.Ldc_I4_0);
		iLProcessor.Emit(OpCodes.Ret);
		iLProcessor.Append(rethrowException);
		iLProcessor.Emit(OpCodes.Rethrow);
		_typeEditContext.AddExceptionHandlerToMethod(method, _context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Catch, removeKey, checkHResult, null, checkHResult, rethrowException);
	}

	internal void WriteSetItem(MethodDefinition method)
	{
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		MethodReference mapInsertMethod = _iMapInstanceTypeResolver.Resolve(_iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Insert"));
		Instruction loadThisInstruction = ilProcessor.Create(OpCodes.Ldarg_0);
		WriteKeyNullCheck(ilProcessor, loadThisInstruction, 0);
		ilProcessor.Append(loadThisInstruction);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldarg_2);
		ilProcessor.Emit(OpCodes.Callvirt, mapInsertMethod);
		ilProcessor.Emit(OpCodes.Pop);
		ilProcessor.Emit(OpCodes.Ret);
	}

	private MethodReference GetContainsKeyMethod(TypeDefinition adapterType)
	{
		string containsKeyMethodName = _iDictionaryTypeDef.FullName + ".ContainsKey";
		MethodDefinition containsKeyMethodDef = adapterType.Methods.Single((MethodDefinition m) => m.Name == containsKeyMethodName);
		return _iDictionaryInstanceTypeResolver.Resolve(containsKeyMethodDef);
	}

	private void WriteGetReadOnlyCollection(MethodDefinition method, DictionaryCollectionTypesGenerator.CollectionKind collectionKind)
	{
		bool implementICollection = _iDictionaryTypeDef.FullName == "System.Collections.Generic.IDictionary`2";
		TypeDefinition collectionType = new DictionaryCollectionTypesGenerator(_context, _typeEditContext, _iDictionaryTypeDef, collectionKind).EmitDictionaryKeyCollection(method.DeclaringType.Module, implementICollection);
		GenericInstanceType collectionTypeInstance = _context.Global.Services.TypeFactory.CreateGenericInstanceTypeFromDefinition(collectionType, method.DeclaringType.GenericParameters[0], method.DeclaringType.GenericParameters[1]);
		MethodDefinition collectionConstructor = collectionType.Methods.Single((MethodDefinition m) => m.IsConstructor);
		MethodReference collectionConstructorInstance = _context.Global.Services.TypeFactory.ResolverFor(collectionTypeInstance).Resolve(collectionConstructor);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Newobj, collectionConstructorInstance);
		iLProcessor.Emit(OpCodes.Ret);
	}

	private void WriteKeyNullCheck(ILProcessor ilProcessor, Instruction labelAfterThrow, int parameterIndex)
	{
		MethodDefinition method = ilProcessor.Body.Method;
		ilProcessor.Emit(OpCodes.Ldarg, parameterIndex + (method.HasThis ? 1 : 0));
		ilProcessor.Emit(OpCodes.Box, method.Parameters[parameterIndex].ParameterType);
		ilProcessor.Emit(OpCodes.Brtrue, labelAfterThrow);
		ilProcessor.Emit(OpCodes.Ldstr, "key");
		ilProcessor.Emit(OpCodes.Newobj, _argumentNullExceptionConstructor);
		ilProcessor.Emit(OpCodes.Throw);
	}
}
