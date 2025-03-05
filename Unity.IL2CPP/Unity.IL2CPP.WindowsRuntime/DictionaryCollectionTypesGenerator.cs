using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

namespace Unity.IL2CPP.WindowsRuntime;

internal sealed class DictionaryCollectionTypesGenerator
{
	public enum CollectionKind
	{
		Key,
		Value
	}

	private readonly MinimalContext _context;

	private readonly EditContext _typeEditContext;

	private readonly TypeDefinition _iDictionaryTypeDef;

	private readonly TypeDefinition _iDisposableTypeDef;

	private readonly TypeDefinition _iEnumerableTypeDef;

	private readonly TypeDefinition _iEnumeratorTypeDef;

	private readonly TypeDefinition _iCollectionTypeDef;

	private readonly TypeDefinition _nonGenericIEnumeratorTypeDef;

	private readonly TypeDefinition _keyValuePairTypeDef;

	private readonly MethodDefinition _notSupportedExceptionCtor;

	private readonly CollectionKind _collectionKind;

	public DictionaryCollectionTypesGenerator(MinimalContext context, EditContext typeEditContext, TypeDefinition iDictionaryTypeDef, CollectionKind collectionKind)
	{
		_context = context;
		_typeEditContext = typeEditContext;
		_iDictionaryTypeDef = iDictionaryTypeDef;
		_collectionKind = collectionKind;
		_iDisposableTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IDisposable);
		_iEnumerableTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerable_1);
		_iEnumeratorTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator_1);
		_iCollectionTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.ICollection_1);
		_nonGenericIEnumeratorTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.IEnumerator);
		_keyValuePairTypeDef = context.Global.Services.TypeProvider.GetSystemType(SystemType.KeyValuePair);
		TypeDefinition notSupportedException = context.Global.Services.TypeProvider.GetSystemType(SystemType.NotSupportedException);
		_notSupportedExceptionCtor = notSupportedException.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
	}

	public TypeDefinition EmitDictionaryKeyCollection(ModuleDefinition module, bool implementICollection)
	{
		ITypeFactory typeFactory = _typeEditContext.Context.CreateThreadSafeFactoryForFullConstruction();
		TypeAttributes typeAttributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
		string typeName = $"{_iDictionaryTypeDef.Name.Substring(1, _iDictionaryTypeDef.Name.Length - 3)}{_collectionKind}Collection`2";
		TypeDefinition typeDefinition = _typeEditContext.BuildClass("System.Runtime.InteropServices.WindowsRuntime", typeName, typeAttributes).CloneGenericParameters(_keyValuePairTypeDef).Complete();
		GenericInstanceType iEnumerableInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iEnumerableTypeDef, typeDefinition.GenericParameters[(int)_collectionKind]);
		_typeEditContext.AddInterfaceImplementationToType(typeDefinition, iEnumerableInstance);
		FieldDefinition dictionaryField = AddDictionaryField(typeFactory, typeDefinition);
		GenericInstanceType typeInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(typeDefinition, typeDefinition.GenericParameters);
		TypeResolver typeInstanceResolver = typeFactory.ResolverFor(typeInstance);
		FieldReference dictionaryFieldInstance = typeInstanceResolver.Resolve(dictionaryField);
		EmitCollectionConstructor(typeDefinition, dictionaryFieldInstance);
		TypeDefinition enumeratorType = EmitEnumeratorType(typeFactory, module);
		MethodDefinition iEnumerableOfTGetEnumeratorMethod = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.IEnumerable`1.GetEnumerator");
		EmitIEnumerableOfTGetEnumeratorMethodBody(typeFactory, iEnumerableOfTGetEnumeratorMethod, enumeratorType, dictionaryFieldInstance);
		MethodDefinition iEnumerableGetEnumeratorMethod = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerable.GetEnumerator");
		EmitIEnumerableGetEnumeratorMethodBody(iEnumerableGetEnumeratorMethod, typeInstanceResolver.Resolve(iEnumerableOfTGetEnumeratorMethod));
		if (implementICollection)
		{
			GenericInstanceType iCollectionInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iCollectionTypeDef, typeDefinition.GenericParameters[(int)_collectionKind]);
			_typeEditContext.AddInterfaceImplementationToType(typeDefinition, iCollectionInstance);
			EmitGetCountMethodBody(typeFactory, typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.get_Count"), dictionaryFieldInstance);
			EmitIsReadOnlyMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.get_IsReadOnly"));
			EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Add"));
			EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Clear"));
			EmitContainsMethodBody(typeFactory, typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Contains"), dictionaryFieldInstance);
			EmitCopyToMethodBody(typeFactory, typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.CopyTo"), dictionaryFieldInstance);
			EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Remove"));
		}
		return typeDefinition;
	}

	private void EmitCollectionConstructor(TypeDefinition typeDefinition, FieldReference dictionaryField)
	{
		MethodAttributes constructorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		MethodDefinition methodDefinition = _typeEditContext.BuildMethod(".ctor", constructorAttributes).AddParameter("dictionary", ParameterAttributes.None, dictionaryField.FieldType).WithEmptyBody()
			.Complete(typeDefinition);
		ILProcessor iLProcessor = methodDefinition.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Call, GetObjectConstructor());
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Stfld, dictionaryField);
		iLProcessor.Emit(OpCodes.Ret);
		methodDefinition.Body.OptimizeMacros();
	}

	private void EmitIEnumerableOfTGetEnumeratorMethodBody(ITypeFactory typeFactory, MethodDefinition getEnumeratorMethod, TypeDefinition enumeratorType, FieldReference dictionaryField)
	{
		GenericInstanceType enumeratorTypeInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(enumeratorType, getEnumeratorMethod.DeclaringType.GenericParameters);
		MethodReference enumeratorConstructor = typeFactory.ResolverFor(enumeratorTypeInstance).Resolve(enumeratorType.Methods.Single((MethodDefinition m) => m.IsConstructor));
		_typeEditContext.ChangeAttributes(getEnumeratorMethod, getEnumeratorMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = getEnumeratorMethod.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, dictionaryField);
		iLProcessor.Emit(OpCodes.Newobj, enumeratorConstructor);
		iLProcessor.Emit(OpCodes.Ret);
		getEnumeratorMethod.Body.OptimizeMacros();
	}

	private void EmitIEnumerableGetEnumeratorMethodBody(MethodDefinition getEnumeratorMethod, MethodReference iEnumeratorOfTGetEnumeratorMethod)
	{
		_typeEditContext.ChangeAttributes(getEnumeratorMethod, getEnumeratorMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = getEnumeratorMethod.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Call, iEnumeratorOfTGetEnumeratorMethod);
		iLProcessor.Emit(OpCodes.Ret);
		getEnumeratorMethod.Body.OptimizeMacros();
	}

	private void EmitGetCountMethodBody(ITypeFactory typeFactory, MethodDefinition getCountMethod, FieldReference dictionaryFieldInstance)
	{
		EmitMethodForwardingToFieldMethodBody(getCountMethod, dictionaryFieldInstance, GetDictionaryGetCountMethod(typeFactory, getCountMethod.DeclaringType));
	}

	private MethodReference GetDictionaryGetCountMethod(ITypeFactory typeFactory, TypeDefinition adapterType)
	{
		GenericInstanceType keyValuePairInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_keyValuePairTypeDef, adapterType.GenericParameters);
		GenericInstanceType iCollectionInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iCollectionTypeDef, keyValuePairInstance);
		MethodDefinition iCollectionGetCountMethodDef = _iCollectionTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Count" && m.Parameters.Count == 0);
		return typeFactory.ResolverFor(iCollectionInstance).Resolve(iCollectionGetCountMethodDef);
	}

	private void EmitIsReadOnlyMethodBody(MethodDefinition isReadOnlyMethod)
	{
		_typeEditContext.ChangeAttributes(isReadOnlyMethod, isReadOnlyMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = isReadOnlyMethod.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldc_I4_1);
		iLProcessor.Emit(OpCodes.Ret);
		isReadOnlyMethod.Body.OptimizeMacros();
	}

	private void EmitContainsMethodBody(ITypeFactory typeFactory, MethodDefinition containsMethod, FieldReference dictionaryFieldInstance)
	{
		if (_collectionKind == CollectionKind.Key)
		{
			MethodDefinition containsKeyMethodDef = _iDictionaryTypeDef.Methods.Single((MethodDefinition m) => m.Name == "ContainsKey" && m.Parameters.Count == 1);
			MethodReference containsKeyMethod = typeFactory.ResolverFor(dictionaryFieldInstance.DeclaringType).Resolve(containsKeyMethodDef);
			EmitMethodForwardingToFieldMethodBody(containsMethod, dictionaryFieldInstance, containsKeyMethod);
			return;
		}
		_typeEditContext.ChangeAttributes(containsMethod, containsMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor ilProcessor = containsMethod.Body.GetILProcessor();
		GenericInstanceType obj = (GenericInstanceType)dictionaryFieldInstance.DeclaringType;
		TypeReference keyType = obj.GenericArguments[0];
		TypeReference valueType = obj.GenericArguments[1];
		GenericInstanceType keyValuePairInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_keyValuePairTypeDef, keyType, valueType);
		GenericInstanceType iEnumerableInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iEnumerableTypeDef, keyValuePairInstance);
		GenericInstanceType iEnumeratorInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iEnumeratorTypeDef, keyValuePairInstance);
		TypeResolver iEnumeratorResolver = typeFactory.ResolverFor(iEnumeratorInstance);
		TypeDefinition equalityComparerDef = _context.Global.Services.TypeProvider.GetSystemType(SystemType.EqualityComparer);
		GenericInstanceType equalityComparerInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(equalityComparerDef, valueType);
		TypeResolver typeResolver = typeFactory.ResolverFor(equalityComparerInstance);
		MethodDefinition getEnumeratorMethodDef = _iEnumerableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
		MethodReference getEnumeratorMethod = typeFactory.ResolverFor(iEnumerableInstance).Resolve(getEnumeratorMethodDef);
		MethodDefinition moveNextMethod = _nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
		MethodDefinition getCurrentMethodDef = _iEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
		MethodReference getCurrentMethod = iEnumeratorResolver.Resolve(getCurrentMethodDef);
		MethodDefinition getValueMethodDef = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Value" && m.Parameters.Count == 0);
		MethodReference getValueMethod = typeFactory.ResolverFor(keyValuePairInstance).Resolve(getValueMethodDef);
		MethodDefinition equalityComparerDefaultMethodDef = equalityComparerDef.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0);
		MethodReference equalityComparerDefaultMethod = typeResolver.Resolve(equalityComparerDefaultMethodDef);
		MethodDefinition equalityComparerEqualsMethodDef = equalityComparerDef.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2);
		MethodReference equalityComparerEqualsMethod = typeResolver.Resolve(equalityComparerEqualsMethodDef);
		MethodDefinition iDisposableDisposeMethod = _iDisposableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
		_typeEditContext.AddVariableToMethod(containsMethod, iEnumeratorInstance);
		_typeEditContext.AddVariableToMethod(containsMethod, equalityComparerInstance);
		_typeEditContext.AddVariableToMethod(containsMethod, keyValuePairInstance);
		Instruction returnFalseBranch = ilProcessor.Create(OpCodes.Nop);
		Instruction returnTrueBranch = ilProcessor.Create(OpCodes.Nop);
		Instruction loopStart = ilProcessor.Create(OpCodes.Nop);
		Instruction loopEnd = ilProcessor.Create(OpCodes.Nop);
		ilProcessor.Emit(OpCodes.Call, equalityComparerDefaultMethod);
		ilProcessor.Emit(OpCodes.Stloc_1);
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
		ilProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
		ilProcessor.Emit(OpCodes.Stloc_0);
		ilProcessor.Append(loopStart);
		ilProcessor.Emit(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Callvirt, moveNextMethod);
		ilProcessor.Emit(OpCodes.Brfalse, loopEnd);
		ilProcessor.Emit(OpCodes.Ldloc_1);
		ilProcessor.Emit(OpCodes.Ldarg_1);
		ilProcessor.Emit(OpCodes.Ldloc_0);
		ilProcessor.Emit(OpCodes.Callvirt, getCurrentMethod);
		ilProcessor.Emit(OpCodes.Stloc_2);
		ilProcessor.Emit(OpCodes.Ldloca_S, containsMethod.Body.Variables[2]);
		ilProcessor.Emit(OpCodes.Call, getValueMethod);
		ilProcessor.Emit(OpCodes.Callvirt, equalityComparerEqualsMethod);
		ilProcessor.Emit(OpCodes.Brfalse, loopStart);
		ilProcessor.Emit(OpCodes.Leave, returnTrueBranch);
		ilProcessor.Append(loopEnd);
		ilProcessor.Emit(OpCodes.Leave, returnFalseBranch);
		Instruction finallyStartInstruction = ilProcessor.Create(OpCodes.Ldloc_0);
		ilProcessor.Append(finallyStartInstruction);
		ilProcessor.Emit(OpCodes.Callvirt, iDisposableDisposeMethod);
		ilProcessor.Emit(OpCodes.Endfinally);
		ilProcessor.Append(returnFalseBranch);
		ilProcessor.Emit(OpCodes.Ldc_I4_0);
		ilProcessor.Emit(OpCodes.Ret);
		ilProcessor.Append(returnTrueBranch);
		ilProcessor.Emit(OpCodes.Ldc_I4_1);
		ilProcessor.Emit(OpCodes.Ret);
		_typeEditContext.AddExceptionHandlerToMethod(containsMethod, _context.Global.Services.TypeProvider.SystemException, ExceptionHandlerType.Finally, loopStart, finallyStartInstruction, null, finallyStartInstruction, returnFalseBranch);
		containsMethod.Body.OptimizeMacros();
	}

	private void EmitCopyToMethodBody(ITypeFactory typeFactory, MethodDefinition copyToMethod, FieldReference dictionaryFieldInstance)
	{
		_typeEditContext.ChangeAttributes(copyToMethod, copyToMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor ilProcessor = copyToMethod.Body.GetILProcessor();
		GenericInstanceType obj = (GenericInstanceType)dictionaryFieldInstance.DeclaringType;
		TypeReference keyType = obj.GenericArguments[0];
		TypeReference valueType = obj.GenericArguments[1];
		GenericInstanceType keyValuePairInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_keyValuePairTypeDef, keyType, valueType);
		MethodDefinition getKeyMethodDef = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Key" && m.Parameters.Count == 0);
		MethodReference getKeyMethod = typeFactory.ResolverFor(keyValuePairInstance).Resolve(getKeyMethodDef);
		MethodDefinition getValueMethodDef = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Value" && m.Parameters.Count == 0);
		MethodReference getValueMethod = typeFactory.ResolverFor(keyValuePairInstance).Resolve(getValueMethodDef);
		VariableDefinition keyValuePairLocalVariable = _typeEditContext.AddVariableToMethod(copyToMethod, keyValuePairInstance);
		ICollectionProjectedMethodBodyWriter.EmitCopyToLoop(_context, _typeEditContext, ilProcessor, keyValuePairInstance, delegate(ILProcessor processor)
		{
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
		}, delegate(ILProcessor processor)
		{
			processor.Emit(OpCodes.Stloc, keyValuePairLocalVariable);
			processor.Emit(OpCodes.Ldloca_S, keyValuePairLocalVariable);
			if (_collectionKind == CollectionKind.Key)
			{
				processor.Emit(OpCodes.Call, getKeyMethod);
			}
			else
			{
				processor.Emit(OpCodes.Call, getValueMethod);
			}
		});
		copyToMethod.Body.OptimizeMacros();
	}

	private void EmitThrowInvalidMutationExceptionMethodBody(MethodDefinition method)
	{
		_typeEditContext.ChangeAttributes(method, method.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = method.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldstr, GetInvalidMutationExceptionMessage());
		iLProcessor.Emit(OpCodes.Newobj, _notSupportedExceptionCtor);
		iLProcessor.Emit(OpCodes.Throw);
		method.Body.OptimizeMacros();
	}

	private TypeDefinition EmitEnumeratorType(ITypeFactory typeFactory, ModuleDefinition module)
	{
		TypeAttributes typeAttributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
		string typeName = $"{_iDictionaryTypeDef.Name.Substring(1, _iDictionaryTypeDef.Name.Length - 3)}{_collectionKind}Enumerator`2";
		TypeDefinition typeDefinition = _typeEditContext.BuildClass("System.Runtime.InteropServices.WindowsRuntime", typeName, typeAttributes).CloneGenericParameters(_keyValuePairTypeDef).Complete();
		GenericInstanceType iEnumeratorInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_iEnumeratorTypeDef, typeDefinition.GenericParameters[(int)_collectionKind]);
		_typeEditContext.AddInterfaceImplementationToType(typeDefinition, iEnumeratorInstance);
		FieldDefinition dictionaryField = AddDictionaryField(typeFactory, typeDefinition);
		FieldDefinition enumeratorField = AddEnumeratorField(typeFactory, typeDefinition, _iEnumeratorTypeDef);
		GenericInstanceType typeInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(typeDefinition, typeDefinition.GenericParameters);
		TypeResolver typeInstanceResolver = typeFactory.ResolverFor(typeInstance);
		FieldReference dictionaryFieldInstance = typeInstanceResolver.Resolve(dictionaryField);
		FieldReference enumeratorFieldInstance = typeInstanceResolver.Resolve(enumeratorField);
		MethodDefinition getEnumeratorMethodDef = _iEnumerableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator");
		MethodReference getEnumeratorMethod = typeFactory.ResolverFor(enumeratorFieldInstance.FieldType).Resolve(getEnumeratorMethodDef);
		EmitEnumeratorConstructor(typeDefinition, dictionaryFieldInstance, enumeratorFieldInstance, getEnumeratorMethod);
		EmitEnumeratorDisposeBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.IDisposable.Dispose"), enumeratorFieldInstance);
		EmitEnumeratorMoveNextMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.MoveNext"), enumeratorFieldInstance);
		MethodDefinition resetMethod = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.Reset");
		if (resetMethod != null)
		{
			EmitEnumeratorResetMethodBody(resetMethod, dictionaryFieldInstance, enumeratorFieldInstance, getEnumeratorMethod);
		}
		MethodDefinition iEnumeratorOfTCurrentMethod = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.IEnumerator`1.get_Current");
		EmitEnumeratorOfTGetCurrentMethod(iEnumeratorOfTCurrentMethod, enumeratorFieldInstance);
		EmitEnumeratorGetCurrentMethod(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.get_Current"), typeInstanceResolver.Resolve(iEnumeratorOfTCurrentMethod));
		return typeDefinition;
	}

	private void EmitEnumeratorConstructor(TypeDefinition typeDefinition, FieldReference dictionaryField, FieldReference enumeratorField, MethodReference getEnumeratorMethod)
	{
		MethodAttributes constructorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		MethodDefinition methodDefinition = _typeEditContext.BuildMethod(".ctor", constructorAttributes).AddParameter("dictionary", ParameterAttributes.None, dictionaryField.FieldType).WithEmptyBody()
			.Complete(typeDefinition);
		ILProcessor iLProcessor = methodDefinition.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Call, GetObjectConstructor());
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Stfld, dictionaryField);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_1);
		iLProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
		iLProcessor.Emit(OpCodes.Stfld, enumeratorField);
		iLProcessor.Emit(OpCodes.Ret);
		methodDefinition.Body.OptimizeMacros();
	}

	private void EmitEnumeratorDisposeBody(MethodDefinition method, FieldReference enumeratorFieldInstance)
	{
		MethodDefinition iDisposableDisposeMethod = _iDisposableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
		EmitMethodForwardingToFieldMethodBody(method, enumeratorFieldInstance, iDisposableDisposeMethod);
	}

	private void EmitEnumeratorMoveNextMethodBody(MethodDefinition method, FieldReference enumeratorFieldInstance)
	{
		MethodDefinition moveNextMethod = _nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
		EmitMethodForwardingToFieldMethodBody(method, enumeratorFieldInstance, moveNextMethod);
	}

	private void EmitEnumeratorResetMethodBody(MethodDefinition resetMethod, FieldReference dictionaryFieldInstance, FieldReference enumeratorFieldInstance, MethodReference getEnumeratorMethod)
	{
		_typeEditContext.ChangeAttributes(resetMethod, resetMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = resetMethod.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
		iLProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
		iLProcessor.Emit(OpCodes.Stfld, enumeratorFieldInstance);
		iLProcessor.Emit(OpCodes.Ret);
		resetMethod.Body.OptimizeMacros();
	}

	private void EmitEnumeratorOfTGetCurrentMethod(MethodDefinition getCurrentMethod, FieldReference enumeratorFieldInstance)
	{
		MethodDefinition iEnumeratorCurrentMethod = _iEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current");
		MethodReference iEnumeratorCurrentMethodInstance = _context.Global.Services.TypeFactory.ResolverFor(enumeratorFieldInstance.FieldType).Resolve(iEnumeratorCurrentMethod);
		GenericInstanceType keyValuePairInstance = (GenericInstanceType)((GenericInstanceType)enumeratorFieldInstance.FieldType).GenericArguments[0];
		string keyValuePairGetItemMethodName = ((_collectionKind == CollectionKind.Key) ? "get_Key" : "get_Value");
		MethodDefinition keyValuePairGetItemMethod = keyValuePairInstance.Resolve().Methods.Single((MethodDefinition m) => m.HasThis && m.Name == keyValuePairGetItemMethodName && m.Parameters.Count == 0);
		MethodReference keyValuePairGetItemMethodInstance = _context.Global.Services.TypeFactory.ResolverFor(keyValuePairInstance).Resolve(keyValuePairGetItemMethod);
		_typeEditContext.ChangeAttributes(getCurrentMethod, getCurrentMethod.Attributes & ~MethodAttributes.Abstract);
		MethodBody methodBody = getCurrentMethod.Body;
		ILProcessor iLProcessor = methodBody.GetILProcessor();
		_typeEditContext.AddVariableToMethod(getCurrentMethod, keyValuePairInstance);
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Ldfld, enumeratorFieldInstance);
		iLProcessor.Emit(OpCodes.Callvirt, iEnumeratorCurrentMethodInstance);
		iLProcessor.Emit(OpCodes.Stloc_0);
		iLProcessor.Emit(OpCodes.Ldloca_S, methodBody.Variables[0]);
		iLProcessor.Emit(OpCodes.Call, keyValuePairGetItemMethodInstance);
		iLProcessor.Emit(OpCodes.Ret);
		getCurrentMethod.Body.OptimizeMacros();
	}

	private void EmitEnumeratorGetCurrentMethod(MethodDefinition getCurrentMethod, MethodReference iEnumeratorOfTCurrentMethod)
	{
		_typeEditContext.ChangeAttributes(getCurrentMethod, getCurrentMethod.Attributes & ~MethodAttributes.Abstract);
		ILProcessor iLProcessor = getCurrentMethod.Body.GetILProcessor();
		iLProcessor.Emit(OpCodes.Ldarg_0);
		iLProcessor.Emit(OpCodes.Call, iEnumeratorOfTCurrentMethod);
		iLProcessor.Emit(OpCodes.Box, getCurrentMethod.DeclaringType.GenericParameters[(int)_collectionKind]);
		iLProcessor.Emit(OpCodes.Ret);
		getCurrentMethod.Body.OptimizeMacros();
	}

	private void EmitMethodForwardingToFieldMethodBody(MethodDefinition method, FieldReference fieldInstance, MethodReference methodToForwardTo)
	{
		_typeEditContext.ChangeAttributes(method, method.Attributes & ~MethodAttributes.Abstract);
		ILProcessor ilProcessor = method.Body.GetILProcessor();
		ilProcessor.Emit(OpCodes.Ldarg_0);
		ilProcessor.Emit(OpCodes.Ldfld, fieldInstance);
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			ilProcessor.Emit(OpCodes.Ldarg, i + 1);
		}
		ilProcessor.Emit(OpCodes.Callvirt, methodToForwardTo);
		ilProcessor.Emit(OpCodes.Ret);
		method.Body.OptimizeMacros();
	}

	private FieldDefinition AddDictionaryField(ITypeFactory typeFactory, TypeDefinition typeDefinition)
	{
		GenericInstanceType dictionaryFieldType = typeFactory.CreateGenericInstanceTypeFromDefinition(_iDictionaryTypeDef, typeDefinition.GenericParameters);
		return _typeEditContext.BuildField("dictionary", FieldAttributes.Private | FieldAttributes.InitOnly, dictionaryFieldType).Complete(typeDefinition);
	}

	private FieldDefinition AddEnumeratorField(ITypeFactory typeFactory, TypeDefinition typeDefinition, TypeDefinition iEnumeratorTypeDef)
	{
		GenericInstanceType keyValuePairInstance = typeFactory.CreateGenericInstanceTypeFromDefinition(_keyValuePairTypeDef, typeDefinition.GenericParameters);
		GenericInstanceType enumeratorFieldType = typeFactory.CreateGenericInstanceTypeFromDefinition(iEnumeratorTypeDef, keyValuePairInstance);
		return _typeEditContext.BuildField("enumerator", FieldAttributes.Private, enumeratorFieldType).Complete(typeDefinition);
	}

	private MethodDefinition GetObjectConstructor()
	{
		return _context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 0);
	}

	private string GetInvalidMutationExceptionMessage()
	{
		if (_collectionKind != 0)
		{
			return "Mutating a value collection derived from a dictionary is not allowed.";
		}
		return "Mutating a key collection derived from a dictionary is not allowed.";
	}
}
