using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Visitor;

namespace Unity.IL2CPP.GenericsCollection;

public class GenericContextFreeVisitor : Visitor
{
	private readonly IGenericsCollector _generics;

	private readonly PrimaryCollectionContext _context;

	public GenericContextFreeVisitor(PrimaryCollectionContext context, IGenericsCollector generics)
	{
		_context = context;
		_generics = generics;
	}

	protected override void Visit(TypeDefinition typeDefinition, Context context)
	{
		_context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
		ProcessIReferenceIfNeeded(typeDefinition);
		if (_context.Global.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth == 0)
		{
			if (typeDefinition.HasGenericParameters)
			{
				_generics.AddType(typeDefinition.FullySharedType);
			}
			{
				foreach (MethodDefinition method in typeDefinition.Methods)
				{
					if (method.HasGenericParameters)
					{
						_generics.AddMethod((GenericInstanceMethod)method.FullySharedMethod);
					}
				}
				return;
			}
		}
		if (ArrayRegistration.ShouldForce2DArrayFor(_context, typeDefinition))
		{
			ProcessArray(typeDefinition, 2);
		}
		if (typeDefinition.HasFullySharableGenericParameters)
		{
			ProcessGenericType(typeDefinition.FullySharedType);
		}
		base.Visit(typeDefinition, context);
	}

	protected override void Visit(MethodDefinition methodDefinition, Context context)
	{
		_context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
		if (methodDefinition.MethodAndTypeHaveFullySharableGenericParameters)
		{
			GenericContextAwareVisitor.ProcessGenericMethod(_context, (GenericInstanceMethod)methodDefinition.FullySharedMethod, _generics);
		}
		if (methodDefinition.HasFullySharedMethod && (!methodDefinition.HasGenericParameters || methodDefinition.HasFullySharableGenericParameters) && !methodDefinition.IsStripped && (!methodDefinition.DeclaringType.HasGenericParameters || methodDefinition.DeclaringType.HasFullySharableGenericParameters))
		{
			_context.Global.Collectors.GenericMethods.Add(_context, methodDefinition.FullySharedMethod);
		}
		foreach (GenericParameter genericParameter in methodDefinition.GenericParameters)
		{
			foreach (GenericParameterConstraint constraint in genericParameter.Constraints)
			{
				if (constraint.ConstraintType is GenericInstanceType && !constraint.ConstraintType.ContainsGenericParameter)
				{
					ProcessGenericType((GenericInstanceType)constraint.ConstraintType);
				}
			}
		}
		base.Visit(methodDefinition, context);
	}

	protected override void Visit(CustomAttributeArgument customAttributeArgument, Context context)
	{
		ProcessCustomAttributeArgument(_context, customAttributeArgument);
		base.Visit(customAttributeArgument, context);
	}

	private void ProcessCustomAttributeTypeReferenceRecursive(TypeReference typeReference)
	{
		if (typeReference is ArrayType arrayType)
		{
			ProcessCustomAttributeTypeReferenceRecursive(arrayType.ElementType);
		}
		else if (typeReference.IsGenericInstance)
		{
			ProcessGenericType((GenericInstanceType)typeReference);
		}
	}

	private void ProcessCustomAttributeArgument(ReadOnlyContext context, CustomAttributeArgument customAttributeArgument)
	{
		if (customAttributeArgument.Type.IsSystemType)
		{
			if (customAttributeArgument.Value is TypeReference attributeArgumentValue)
			{
				ProcessCustomAttributeTypeReferenceRecursive(attributeArgumentValue);
			}
		}
		else if (customAttributeArgument.Value is CustomAttributeArgument[] values)
		{
			CustomAttributeArgument[] array = values;
			foreach (CustomAttributeArgument value in array)
			{
				ProcessCustomAttributeArgument(context, value);
			}
		}
		else if (customAttributeArgument.Value is CustomAttributeArgument)
		{
			ProcessCustomAttributeArgument(context, (CustomAttributeArgument)customAttributeArgument.Value);
		}
	}

	protected override void Visit(MethodBody methodBody, Context context)
	{
		if (!methodBody.Method.HasGenericParameters)
		{
			base.Visit(methodBody, context);
		}
	}

	protected override void Visit(MethodReference methodReference, Context context)
	{
		GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
		GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
		if (IsFullyInflated(genericInstanceMethod))
		{
			if (IsFullyInflated(genericInstanceType))
			{
				ProcessGenericType(genericInstanceType);
			}
			GenericContextAwareVisitor.ProcessGenericMethod(_context, genericInstanceMethod, _generics);
		}
		else if (IsFullyInflated(genericInstanceType))
		{
			ProcessGenericType(genericInstanceType);
		}
		if (!methodReference.HasGenericParameters && !methodReference.IsGenericInstance)
		{
			base.Visit(methodReference, context);
		}
	}

	protected override void Visit(ArrayType arrayType, Context context)
	{
		if (!arrayType.ContainsGenericParameter)
		{
			ProcessArray(arrayType.ElementType, arrayType.Rank);
		}
		base.Visit(arrayType, context);
	}

	protected override void Visit(Instruction instruction, Context context)
	{
		if (instruction.OpCode.Code == Code.Newarr)
		{
			TypeReference typeReference = (TypeReference)instruction.Operand;
			if (!typeReference.ContainsGenericParameter)
			{
				ProcessArray(typeReference, 1);
			}
		}
		base.Visit(instruction, context);
	}

	protected override void Visit(GenericInstanceType genericInstanceType, Context context)
	{
		if (!genericInstanceType.ContainsGenericParameter)
		{
			GenericInstanceType inflatedType = Inflater.InflateType(_context, default(GenericContext), genericInstanceType);
			ProcessGenericType(inflatedType);
		}
		base.Visit(genericInstanceType, context);
	}

	private void ProcessGenericType(GenericInstanceType inflatedType)
	{
		GenericContextAwareVisitor.ProcessGenericType(_context, inflatedType, _generics);
	}

	private void ProcessArray(TypeReference elementType, int rank)
	{
		for (TypeReference currentElementType = elementType; currentElementType != null; currentElementType = currentElementType.GetBaseType(_context))
		{
			InflateAndProcessArray(currentElementType, rank);
			foreach (TypeReference interfaceType in currentElementType.GetInterfaces(_context))
			{
				InflateAndProcessArray(interfaceType, rank);
			}
		}
	}

	private void InflateAndProcessArray(TypeReference elementType, int rank)
	{
		ArrayType inflatedArray = _context.Global.Services.TypeFactory.CreateArrayType(Inflater.InflateType(_context, default(GenericContext), elementType), rank);
		GenericContextAwareVisitor.ProcessArray(_context, inflatedArray, _generics);
	}

	private static bool IsFullyInflated(GenericInstanceMethod genericInstanceMethod)
	{
		if (genericInstanceMethod != null && !genericInstanceMethod.GenericArguments.Any((TypeReference t) => t.ContainsGenericParameter))
		{
			return !genericInstanceMethod.DeclaringType.ContainsGenericParameter;
		}
		return false;
	}

	private static bool IsFullyInflated(GenericInstanceType genericInstanceType)
	{
		if (genericInstanceType != null)
		{
			return !genericInstanceType.ContainsGenericParameter;
		}
		return false;
	}

	private void ProcessIReferenceIfNeeded(TypeDefinition typeDefinition)
	{
		if (typeDefinition.CanBoxToWindowsRuntime(_context))
		{
			GenericInstanceType genericInstance = _context.Global.Services.TypeFactory.CreateGenericInstanceType((TypeDefinition)_context.Global.Services.TypeProvider.IReferenceType, null, typeDefinition);
			ProcessGenericType(genericInstance);
			genericInstance = _context.Global.Services.TypeFactory.CreateGenericInstanceType((TypeDefinition)_context.Global.Services.TypeProvider.IReferenceArrayType, null, typeDefinition);
			ProcessGenericType(genericInstance);
		}
	}
}
