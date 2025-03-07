namespace Unity.IL2CPP.DataModel.Visitor;

public class Visitor
{
	protected internal virtual void Visit(AssemblyDefinition assemblyDefinition, Context context)
	{
		foreach (TypeDefinition typeDefinition in assemblyDefinition.GetAllTypes())
		{
			Visit(typeDefinition, context.Member(assemblyDefinition.MainModule));
		}
	}

	protected internal virtual void Visit(TypeDefinition typeDefinition, Context context)
	{
		if (typeDefinition.BaseType != null)
		{
			VisitTypeReference(typeDefinition.BaseType, context.BaseType(typeDefinition));
		}
		foreach (CustomAttribute customAttribute in typeDefinition.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(typeDefinition));
		}
		foreach (InterfaceImplementation interfaceImpl in typeDefinition.Interfaces)
		{
			Visit(interfaceImpl, context.Interface(typeDefinition));
		}
		foreach (GenericParameter genericParameter in typeDefinition.GenericParameters)
		{
			Visit(genericParameter, context.GenericParameter(context));
		}
		foreach (PropertyDefinition propertyDefinition in typeDefinition.Properties)
		{
			Visit(propertyDefinition, context.Member(typeDefinition));
		}
		foreach (FieldDefinition fieldDefinition in typeDefinition.Fields)
		{
			Visit(fieldDefinition, context.Member(typeDefinition));
		}
		foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
		{
			Visit(methodDefinition, context.Member(typeDefinition));
		}
		foreach (EventDefinition eventDefinition in typeDefinition.Events)
		{
			Visit(eventDefinition, context.Member(typeDefinition));
		}
	}

	protected virtual void Visit(EventDefinition eventDefinition, Context context)
	{
		VisitTypeReference(eventDefinition.EventType, context.ReturnType(eventDefinition));
		foreach (CustomAttribute customAttribute in eventDefinition.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(eventDefinition));
		}
		if (eventDefinition.AddMethod != null)
		{
			Visit(eventDefinition.AddMethod, context.EventAdder(eventDefinition));
		}
		if (eventDefinition.RemoveMethod != null)
		{
			Visit(eventDefinition.RemoveMethod, context.EventRemover(eventDefinition));
		}
	}

	protected internal virtual void Visit(FieldDefinition fieldDefinition, Context context)
	{
		VisitTypeReference(fieldDefinition.FieldType, context.ReturnType(fieldDefinition));
		foreach (CustomAttribute customAttribute in fieldDefinition.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(fieldDefinition));
		}
	}

	protected internal virtual void Visit(PropertyDefinition propertyDefinition, Context context)
	{
		VisitTypeReference(propertyDefinition.PropertyType, context.ReturnType(propertyDefinition));
		foreach (CustomAttribute customAttribute in propertyDefinition.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(propertyDefinition));
		}
		if (propertyDefinition.GetMethod != null)
		{
			Visit(propertyDefinition.GetMethod, context.Getter(propertyDefinition));
		}
		if (propertyDefinition.SetMethod != null)
		{
			Visit(propertyDefinition.SetMethod, context.Setter(propertyDefinition));
		}
	}

	protected internal virtual void Visit(MethodDefinition methodDefinition, Context context)
	{
		Visit(methodDefinition.MethodReturnType, context.ReturnType(methodDefinition));
		foreach (CustomAttribute customAttribute in methodDefinition.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(methodDefinition));
		}
		foreach (GenericParameter genericParameter in methodDefinition.GenericParameters)
		{
			Visit(genericParameter, context.GenericParameter(methodDefinition));
		}
		foreach (ParameterDefinition parameterDefinition in methodDefinition.Parameters)
		{
			Visit(parameterDefinition, context.Parameter(methodDefinition));
		}
		if (methodDefinition.HasBody)
		{
			Visit(methodDefinition.Body, context.MethodBody(methodDefinition));
		}
	}

	protected virtual void Visit(CustomAttribute customAttribute, Context context)
	{
		if (customAttribute.Constructor != null)
		{
			Visit((MethodReference)customAttribute.Constructor, context.AttributeConstructor(customAttribute));
		}
		if (customAttribute.AttributeType != null)
		{
			VisitTypeReference(customAttribute.AttributeType, context.AttributeType(customAttribute));
		}
		foreach (CustomAttributeArgument customAttributeArgument in customAttribute.ConstructorArguments)
		{
			Visit(customAttributeArgument, context.AttributeArgument(customAttribute));
		}
		foreach (CustomAttributeNamedArgument fieldArgument in customAttribute.Fields)
		{
			Visit(fieldArgument, context.AttributeArgument(customAttribute));
		}
		foreach (CustomAttributeNamedArgument propertyArgument in customAttribute.Properties)
		{
			Visit(propertyArgument, context.AttributeArgument(customAttribute));
		}
	}

	protected virtual void Visit(CustomAttributeArgument customAttributeArgument, Context context)
	{
		VisitTypeReference(customAttributeArgument.Type, context.AttributeArgumentType(customAttributeArgument));
		if (customAttributeArgument.Value is TypeReference argumentValueTypeReference)
		{
			VisitTypeReference(argumentValueTypeReference, context.AttributeArgument(customAttributeArgument));
		}
	}

	protected virtual void Visit(CustomAttributeNamedArgument customAttributeNamedArgument, Context context)
	{
		Visit(customAttributeNamedArgument.Argument, context);
	}

	protected virtual void Visit(FieldReference fieldReference, Context context)
	{
		VisitTypeReference(fieldReference.FieldType, context.ReturnType(fieldReference));
		VisitTypeReference(fieldReference.DeclaringType, context.DeclaringType(fieldReference));
	}

	protected virtual void Visit(MethodReference methodReference, Context context)
	{
		VisitTypeReference(methodReference.ReturnType, context.ReturnType(methodReference));
		VisitTypeReference(methodReference.DeclaringType, context.DeclaringType(methodReference));
		foreach (GenericParameter genericParameter in methodReference.GenericParameters)
		{
			VisitTypeReference(genericParameter, context.GenericParameter(methodReference));
		}
		foreach (ParameterDefinition parameterDefinition in methodReference.Parameters)
		{
			Visit(parameterDefinition, context.Parameter(methodReference));
		}
		if (!(methodReference is GenericInstanceMethod genericInstanceMethod))
		{
			return;
		}
		foreach (TypeReference genericArgument in genericInstanceMethod.GenericArguments)
		{
			VisitTypeReference(genericArgument, context.GenericArgument(genericInstanceMethod));
		}
	}

	protected virtual void Visit(TypeReference typeReference, Context context)
	{
		if (typeReference.DeclaringType != null)
		{
			VisitTypeReference(typeReference.DeclaringType, context.DeclaringType(typeReference));
		}
	}

	protected virtual void Visit(MethodReturnType methodReturnType, Context context)
	{
		VisitTypeReference(methodReturnType.ReturnType, context.ReturnType(methodReturnType));
	}

	protected virtual void Visit(ParameterDefinition parameterDefinition, Context context)
	{
		VisitTypeReference(parameterDefinition.ParameterType, context.ReturnType(parameterDefinition));
	}

	protected virtual void Visit(MethodBody methodBody, Context context)
	{
		foreach (ExceptionHandler exceptionHandler in methodBody.ExceptionHandlers)
		{
			Visit(exceptionHandler, context.Member(exceptionHandler));
		}
		foreach (VariableDefinition variableDefinition in methodBody.Variables)
		{
			Visit(variableDefinition, context.LocalVariable(methodBody));
		}
		foreach (Instruction instruction in methodBody.Instructions)
		{
			Visit(instruction, context);
		}
	}

	protected virtual void Visit(VariableDefinition variableDefinition, Context context)
	{
		VisitTypeReference(variableDefinition.VariableType, context.ReturnType(variableDefinition));
	}

	protected virtual void Visit(Instruction instruction, Context context)
	{
		if (instruction.Operand != null && !(instruction.Operand is Instruction))
		{
			if (instruction.Operand is FieldReference fieldReference)
			{
				Visit(fieldReference, context.Operand(instruction));
			}
			else if (instruction.Operand is MethodReference methodReference)
			{
				Visit(methodReference, context.Operand(instruction));
			}
			else if (instruction.Operand is TypeReference typeReference)
			{
				VisitTypeReference(typeReference, context.Operand(instruction));
			}
			else if (instruction.Operand is ParameterDefinition parameterDefinition)
			{
				Visit(parameterDefinition, context.Operand(instruction));
			}
			else if (instruction.Operand is VariableDefinition variableDefinition)
			{
				Visit(variableDefinition, context.Operand(instruction));
			}
		}
	}

	protected virtual void Visit(ExceptionHandler exceptionHandler, Context context)
	{
		if (exceptionHandler.CatchType != null)
		{
			VisitTypeReference(exceptionHandler.CatchType, context.ReturnType(exceptionHandler));
		}
	}

	protected virtual void Visit(GenericParameter genericParameter, Context context)
	{
	}

	protected internal virtual void Visit(ArrayType arrayType, Context context)
	{
		VisitTypeReference(arrayType.ElementType, context.ElementType(arrayType));
	}

	protected internal virtual void Visit(PointerType pointerType, Context context)
	{
		VisitTypeReference(pointerType.ElementType, context.ElementType(pointerType));
	}

	protected virtual void Visit(ByReferenceType byReferenceType, Context context)
	{
		VisitTypeReference(byReferenceType.ElementType, context.ElementType(byReferenceType));
	}

	protected virtual void Visit(PinnedType pinnedType, Context context)
	{
		VisitTypeReference(pinnedType.ElementType, context.ElementType(pinnedType));
	}

	protected virtual void Visit(SentinelType sentinelType, Context context)
	{
		VisitTypeReference(sentinelType.ElementType, context.ElementType(sentinelType));
	}

	protected virtual void Visit(FunctionPointerType functionPointerType, Context context)
	{
	}

	protected virtual void Visit(RequiredModifierType requiredModifierType, Context context)
	{
		VisitTypeReference(requiredModifierType.ElementType, context.ElementType(requiredModifierType));
	}

	protected virtual void Visit(OptionalModifierType optionalModifierType, Context context)
	{
		VisitTypeReference(optionalModifierType.ElementType, context.ElementType(optionalModifierType));
	}

	protected internal virtual void Visit(GenericInstanceType genericInstanceType, Context context)
	{
		VisitTypeReference(genericInstanceType.ElementType, context.ElementType(genericInstanceType));
		foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
		{
			VisitTypeReference(genericArgument, context.GenericArgument(genericInstanceType));
		}
	}

	protected void VisitTypeReference(TypeReference typeReference, Context context)
	{
		if (typeReference is GenericParameter genericParameter)
		{
			Visit(genericParameter, context);
			return;
		}
		if (typeReference is TypeSpecification)
		{
			if (typeReference is ArrayType arrayType)
			{
				Visit(arrayType, context);
				return;
			}
			if (typeReference is PointerType pointerType)
			{
				Visit(pointerType, context);
				return;
			}
			if (typeReference is ByReferenceType byReferenceType)
			{
				Visit(byReferenceType, context);
				return;
			}
			if (typeReference is FunctionPointerType functionPointerType)
			{
				Visit(functionPointerType, context);
				return;
			}
			if (typeReference is PinnedType pinnedType)
			{
				Visit(pinnedType, context);
				return;
			}
			if (typeReference is SentinelType sentinelType)
			{
				Visit(sentinelType, context);
				return;
			}
			if (typeReference is GenericInstanceType genericInstanceType)
			{
				Visit(genericInstanceType, context);
				return;
			}
			if (typeReference is RequiredModifierType requiredModifierType)
			{
				Visit(requiredModifierType, context);
				return;
			}
			if (typeReference is OptionalModifierType optionalModiferType)
			{
				Visit(optionalModiferType, context);
				return;
			}
		}
		Visit(typeReference, context);
	}

	protected virtual void Visit(InterfaceImplementation interfaceImpl, Context context)
	{
		VisitTypeReference(interfaceImpl.InterfaceType, context.InterfaceType(interfaceImpl));
		foreach (CustomAttribute customAttribute in interfaceImpl.CustomAttributes)
		{
			Visit(customAttribute, context.Attribute(interfaceImpl));
		}
	}
}
