using System;
using System.Collections.Generic;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome;

namespace Unity.IL2CPP.GenericSharing;

public class GenericSharingVisitor
{
	private readonly PrimaryCollectionContext _context;

	private readonly Dictionary<TypeDefinition, GenericSharingData> _genericTypeData = new Dictionary<TypeDefinition, GenericSharingData>();

	private readonly Dictionary<MethodDefinition, GenericSharingData> _genericMethodData = new Dictionary<MethodDefinition, GenericSharingData>();

	private List<RuntimeGenericData> _typeList;

	private List<RuntimeGenericData> _methodList;

	public GenericSharingVisitor(PrimaryCollectionContext context)
	{
		_context = context;
	}

	public void Collect(AssemblyDefinition assembly)
	{
		foreach (TypeDefinition type in assembly.GetAllTypes())
		{
			ProcessType(type);
		}
	}

	public void Add(GenericSharingVisitor other)
	{
		foreach (KeyValuePair<TypeDefinition, GenericSharingData> pair in other._genericTypeData)
		{
			_genericTypeData.Add(pair.Key, pair.Value);
		}
		foreach (KeyValuePair<MethodDefinition, GenericSharingData> pair2 in other._genericMethodData)
		{
			_genericMethodData.Add(pair2.Key, pair2.Value);
		}
	}

	public GenericSharingAnalysisResults Complete()
	{
		return new GenericSharingAnalysisResults(_genericTypeData.AsReadOnly(), _genericMethodData.AsReadOnly());
	}

	private void AddType(TypeDefinition typeDefinition, List<RuntimeGenericData> runtimeGenericDataList)
	{
		_genericTypeData.Add(typeDefinition, new GenericSharingData(runtimeGenericDataList.AsReadOnly()));
		CollectGenericMethodsFromRgctxs(runtimeGenericDataList);
	}

	private void AddMethod(MethodDefinition methodDefinition, List<RuntimeGenericData> runtimeGenericDataList)
	{
		_genericMethodData.Add(methodDefinition, new GenericSharingData(runtimeGenericDataList.AsReadOnly()));
		CollectGenericMethodsFromRgctxs(runtimeGenericDataList);
	}

	private void CollectGenericMethodsFromRgctxs(List<RuntimeGenericData> runtimeGenericDataList)
	{
		foreach (RuntimeGenericData data in runtimeGenericDataList)
		{
			if (data.InfoType == RuntimeGenericContextInfo.Method)
			{
				_context.Global.Collectors.GenericMethods.Add(_context, ((RuntimeGenericMethodData)data).GenericMethod);
			}
			else if (data.InfoType == RuntimeGenericContextInfo.Constrained)
			{
				MethodReference method = ((RuntimeGenericConstrainedCallData)data).ConstrainedMethod;
				if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
				{
					_context.Global.Collectors.GenericMethods.Add(_context, method);
				}
			}
		}
	}

	public void ProcessType(TypeDefinition type)
	{
		_typeList = new List<RuntimeGenericData>();
		foreach (MethodDefinition method in type.Methods)
		{
			if (!type.HasGenericParameters && !method.HasGenericParameters)
			{
				continue;
			}
			if (!method.HasBody)
			{
				if (!_context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetGenericSharingDataFor(_context, method, out var sharingData))
				{
					continue;
				}
				foreach (RuntimeGenericData data in sharingData)
				{
					if (data is RuntimeGenericTypeData genericTypeData)
					{
						AddData(genericTypeData.InfoType, genericTypeData.GenericType);
						continue;
					}
					if (data is RuntimeGenericMethodData genericMethodData)
					{
						AddMethodUsage(genericMethodData.GenericMethod);
						continue;
					}
					throw new NotImplementedException();
				}
				continue;
			}
			_methodList = new List<RuntimeGenericData>();
			foreach (ExceptionHandler exceptionHandler in method.Body.ExceptionHandlers)
			{
				if (exceptionHandler.CatchType != null)
				{
					AddClassUsage(exceptionHandler.CatchType);
				}
			}
			foreach (Instruction instruction in method.Body.Instructions)
			{
				Process(instruction, method);
			}
			if (method.Body.HasVariables)
			{
				foreach (VariableDefinition var in method.Body.Variables)
				{
					AddClassUsage(var.VariableType);
				}
			}
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				AddClassUsage(method.ReturnType);
			}
			if (_context.Global.Parameters.EnableDebugger)
			{
				foreach (ParameterDefinition param in method.Parameters)
				{
					AddClassUsage(param.ParameterType);
				}
				foreach (VariableDefinition var2 in method.Body.Variables)
				{
					AddTypeUsage(var2.VariableType.GetNonPinnedAndNonByReferenceType());
				}
			}
			if (_methodList.Count > 0)
			{
				AddMethod(method, _methodList);
			}
		}
		if (_typeList.Count > 0)
		{
			AddType(type, _typeList);
		}
	}

	private void Process(Instruction instruction, MethodDefinition method)
	{
		_context.Global.Services.ErrorInformation.CurrentMethod = method;
		switch (instruction.OpCode.Code)
		{
		case Code.Cpobj:
		case Code.Ldobj:
		case Code.Stobj:
		case Code.Ldelema:
		case Code.Ldelem_Any:
		case Code.Stelem_Any:
		case Code.Refanyval:
		case Code.Initobj:
		{
			TypeReference type7 = (TypeReference)instruction.Operand;
			AddClassUsage(type7);
			break;
		}
		case Code.Mkrefany:
		{
			TypeReference type6 = (TypeReference)instruction.Operand;
			AddClassUsage(type6);
			AddTypeUsage(type6);
			break;
		}
		case Code.Ldfld:
		case Code.Ldflda:
		case Code.Stfld:
		{
			FieldReference fieldReference2 = (FieldReference)instruction.Operand;
			AddClassUsage(fieldReference2.DeclaringType);
			AddClassUsage(GenericParameterResolver.ResolveFieldTypeIfNeeded(_context.Global.Services.TypeFactory, fieldReference2));
			break;
		}
		case Code.Ldarg_0:
			AddArgUsage(method, 0);
			break;
		case Code.Ldarg_1:
			AddArgUsage(method, 1);
			break;
		case Code.Ldarg_2:
			AddArgUsage(method, 2);
			break;
		case Code.Ldarg_3:
			AddArgUsage(method, 3);
			break;
		case Code.Ldarg_S:
		case Code.Ldarga_S:
		case Code.Starg_S:
		case Code.Ldarg:
		case Code.Ldarga:
		case Code.Starg:
		{
			ParameterDefinition parameterReference = (ParameterDefinition)instruction.Operand;
			AddClassUsage(parameterReference.ParameterType);
			break;
		}
		case Code.Newobj:
		{
			MethodReference ctor = (MethodReference)instruction.Operand;
			if (ctor.DeclaringType.IsArray)
			{
				AddClassUsage(ctor.DeclaringType);
			}
			else
			{
				AddClassUsage(ctor.DeclaringType);
				AddMethodUsage(ctor);
			}
			AddMethodParameterUsage(ctor);
			break;
		}
		case Code.Newarr:
		{
			TypeReference type4 = (TypeReference)instruction.Operand;
			AddArrayUsage(type4);
			break;
		}
		case Code.Ldsfld:
		case Code.Ldsflda:
		case Code.Stsfld:
		{
			FieldReference field = (FieldReference)instruction.Operand;
			TypeReference type3 = field.DeclaringType;
			AddClassUsage(type3);
			AddClassUsage(GenericParameterResolver.ResolveFieldTypeIfNeeded(_context.Global.Services.TypeFactory, field));
			AddStaticUsage(_context, type3);
			break;
		}
		case Code.Castclass:
		case Code.Isinst:
		case Code.Box:
		case Code.Constrained:
		{
			TypeReference type5 = (TypeReference)instruction.Operand;
			AddClassUsage(type5);
			if (type5.IsNullableGenericInstance)
			{
				AddClassUsage(((GenericInstanceType)type5).GenericArguments[0]);
			}
			break;
		}
		case Code.Unbox:
		case Code.Unbox_Any:
		{
			TypeReference type2 = (TypeReference)instruction.Operand;
			AddClassUsage(type2);
			break;
		}
		case Code.Sizeof:
		{
			TypeReference type = (TypeReference)instruction.Operand;
			AddClassUsage(type);
			break;
		}
		case Code.Ldtoken:
			if (instruction.Operand is TypeReference type8)
			{
				AddTypeUsage(type8);
			}
			if (instruction.Operand is MethodReference methodReference3)
			{
				AddMethodUsage(methodReference3);
			}
			if (instruction.Operand is FieldReference fieldReference)
			{
				AddClassUsage(fieldReference.DeclaringType);
			}
			break;
		case Code.Ldftn:
		{
			MethodReference methodReference2 = (MethodReference)instruction.Operand;
			AddMethodUsage(methodReference2);
			break;
		}
		case Code.Ldvirtftn:
		{
			MethodReference methodReference = (MethodReference)instruction.Operand;
			AddMethodUsage(methodReference);
			if (methodReference.DeclaringType.IsInterface)
			{
				AddClassUsage(methodReference.DeclaringType);
			}
			break;
		}
		case Code.Call:
		case Code.Callvirt:
		{
			MethodReference calledMethod = (MethodReference)instruction.Operand;
			if (MethodSignatureWriter.NeedsMethodMetadataCollected(_context, calledMethod, forFullGenericSharing: true))
			{
				if (instruction.OpCode.Code == Code.Callvirt)
				{
					AddClassUsage(calledMethod.DeclaringType);
					if (instruction.Previous != null && instruction.Previous.OpCode.Code == Code.Constrained)
					{
						AddConstrainedCallUsage((TypeReference)instruction.Previous.Operand, calledMethod);
						AddClassUsage((TypeReference)instruction.Previous.Operand);
					}
					else
					{
						AddMethodUsage(calledMethod);
					}
				}
				else
				{
					AddMethodUsage(calledMethod);
				}
			}
			if (GenericSharingAnalysis.ShouldTryToCallStaticConstructorBeforeMethodCall(_context, calledMethod, method))
			{
				AddStaticUsage(_context, calledMethod.DeclaringType);
			}
			AddMethodParameterUsage(calledMethod);
			AddClassUsage(GenericParameterResolver.ResolveReturnTypeIfNeeded(_context.Global.Services.TypeFactory, calledMethod));
			break;
		}
		default:
			if (instruction.Operand is MemberReference)
			{
				throw new NotImplementedException($"Unable to handle instruction '{instruction}'.");
			}
			break;
		}
	}

	private void AddArgUsage(MethodDefinition method, int argIndex)
	{
		if (method.HasThis)
		{
			argIndex--;
		}
		if (argIndex >= 0)
		{
			AddClassUsage(method.Parameters[argIndex].ParameterType);
		}
	}

	public void AddStaticUsage(ReadOnlyContext context, TypeReference genericType)
	{
		AddStaticUsageRecursiveIfNeeded(context, genericType);
	}

	private void AddStaticUsageRecursiveIfNeeded(ReadOnlyContext context, TypeReference genericType)
	{
		if (genericType.IsGenericInstance)
		{
			AddData(RuntimeGenericContextInfo.Static, genericType);
		}
		TypeReference baseType = genericType.GetBaseType(context);
		if (baseType != null)
		{
			AddStaticUsageRecursiveIfNeeded(context, baseType);
		}
	}

	public void AddClassUsage(TypeReference genericType)
	{
		AddData(RuntimeGenericContextInfo.Class, genericType);
	}

	public void AddArrayUsage(TypeReference genericType)
	{
		AddData(RuntimeGenericContextInfo.Array, genericType);
	}

	public void AddTypeUsage(TypeReference genericType)
	{
		AddData(RuntimeGenericContextInfo.Type, genericType);
	}

	public void AddData(RuntimeGenericContextInfo infoType, TypeReference genericType)
	{
		RuntimeGenericTypeData data = new RuntimeGenericTypeData(infoType, genericType);
		List<RuntimeGenericData> rgctx;
		switch (GenericUsageFor(data.GenericType))
		{
		case GenericContextUsage.Type:
			rgctx = _typeList;
			break;
		case GenericContextUsage.Method:
			rgctx = _methodList;
			break;
		case GenericContextUsage.Both:
			rgctx = _methodList;
			break;
		case GenericContextUsage.None:
			return;
		default:
			throw new NotSupportedException("Invalid generic parameter usage");
		}
		if (rgctx.FindIndex((RuntimeGenericData d) => d.InfoType == data.InfoType && ((RuntimeGenericTypeData)d).GenericType == data.GenericType) == -1)
		{
			rgctx.Add(data);
		}
	}

	public void AddMethodUsage(MethodReference genericMethod)
	{
		List<RuntimeGenericData> rgctx;
		switch (GenericUsageFor(genericMethod))
		{
		case GenericContextUsage.Type:
			rgctx = _typeList;
			break;
		case GenericContextUsage.Method:
			rgctx = _methodList;
			break;
		case GenericContextUsage.Both:
			rgctx = _methodList;
			break;
		case GenericContextUsage.None:
			return;
		default:
			throw new NotSupportedException("Invalid generic parameter usage");
		}
		RuntimeGenericMethodData data = new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, genericMethod);
		if (rgctx.FindIndex((RuntimeGenericData d) => d.InfoType == data.InfoType && ((RuntimeGenericMethodData)d).GenericMethod == data.GenericMethod) == -1)
		{
			rgctx.Add(data);
		}
	}

	private void AddMethodParameterUsage(MethodReference genericMethod)
	{
		foreach (ParameterDefinition methodParameter in genericMethod.Parameters)
		{
			AddClassUsage(GenericParameterResolver.ResolveParameterTypeIfNeeded(_context.Global.Services.TypeFactory, genericMethod, methodParameter));
		}
	}

	public void AddConstrainedCallUsage(TypeReference constrainedType, MethodReference constrainedMethod)
	{
		List<RuntimeGenericData> rgctx;
		switch (GenericUsageFor(constrainedType) | GenericUsageFor(constrainedMethod))
		{
		case GenericContextUsage.Type:
			rgctx = _typeList;
			break;
		case GenericContextUsage.Method:
			rgctx = _methodList;
			break;
		case GenericContextUsage.Both:
			rgctx = _methodList;
			break;
		case GenericContextUsage.None:
			return;
		default:
			throw new NotSupportedException("Invalid generic parameter usage");
		}
		RuntimeGenericConstrainedCallData data = new RuntimeGenericConstrainedCallData(RuntimeGenericContextInfo.Constrained, constrainedType, constrainedMethod);
		if (rgctx.FindIndex((RuntimeGenericData d) => d.InfoType == data.InfoType && ((RuntimeGenericConstrainedCallData)d).ConstrainedType == data.ConstrainedType && ((RuntimeGenericConstrainedCallData)d).ConstrainedMethod == data.ConstrainedMethod) == -1)
		{
			rgctx.Add(data);
		}
	}

	internal static GenericContextUsage GenericUsageFor(MethodReference method)
	{
		GenericContextUsage usage = GenericUsageFor(method.DeclaringType);
		if (method is GenericInstanceMethod genericInstanceMethod)
		{
			{
				foreach (TypeReference genericArgument in genericInstanceMethod.GenericArguments)
				{
					usage |= GenericUsageFor(genericArgument);
				}
				return usage;
			}
		}
		return usage;
	}

	internal static GenericContextUsage GenericUsageFor(TypeReference type)
	{
		if (type is GenericParameter genericParameter)
		{
			if (genericParameter.Type != 0)
			{
				return GenericContextUsage.Method;
			}
			return GenericContextUsage.Type;
		}
		if (type is ArrayType arrayType)
		{
			return GenericUsageFor(arrayType.ElementType);
		}
		if (type is GenericInstanceType genericInstanceType)
		{
			GenericContextUsage usage = GenericContextUsage.None;
			{
				foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
				{
					usage |= GenericUsageFor(genericArgument);
				}
				return usage;
			}
		}
		if (type is ByReferenceType byReferenceType)
		{
			return GenericUsageFor(byReferenceType.ElementType);
		}
		if (type is PointerType pointerType)
		{
			return GenericUsageFor(pointerType.ElementType);
		}
		if (type is PinnedType pinnedType)
		{
			return GenericUsageFor(pinnedType.ElementType);
		}
		if (type is RequiredModifierType requiredType)
		{
			return GenericUsageFor(requiredType.ElementType);
		}
		if (type is OptionalModifierType optionalType)
		{
			return GenericUsageFor(optionalType.ElementType);
		}
		if (type is FunctionPointerType)
		{
			return GenericContextUsage.None;
		}
		if (type is TypeSpecification typeSpecification)
		{
			throw new NotSupportedException($"TypeSpecification found which is not supported {typeSpecification}");
		}
		return GenericContextUsage.None;
	}
}
