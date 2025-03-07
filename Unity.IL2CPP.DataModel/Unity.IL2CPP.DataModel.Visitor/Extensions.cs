namespace Unity.IL2CPP.DataModel.Visitor;

public static class Extensions
{
	public static void Accept(this AssemblyDefinition assemblyDefinition, Visitor visitor)
	{
		if (visitor != null && assemblyDefinition != null)
		{
			visitor.Visit(assemblyDefinition, Context.None);
		}
	}

	public static void Accept(this TypeDefinition typeDefinition, Visitor visitor)
	{
		if (visitor != null && typeDefinition != null)
		{
			visitor.Visit(typeDefinition, Context.None);
		}
	}

	public static void Accept(this GenericInstanceType genericInstanceType, Visitor visitor)
	{
		if (visitor != null && genericInstanceType != null)
		{
			visitor.Visit(genericInstanceType, Context.None);
		}
	}

	public static void Accept(this PointerType pointerType, Visitor visitor)
	{
		if (visitor != null && pointerType != null)
		{
			visitor.Visit(pointerType, Context.None);
		}
	}

	public static void Accept(this ArrayType arrayType, Visitor visitor)
	{
		if (visitor != null && arrayType != null)
		{
			visitor.Visit(arrayType, Context.None);
		}
	}

	public static void Accept(this FieldDefinition fieldDefinition, Visitor visitor)
	{
		if (visitor != null && fieldDefinition != null)
		{
			visitor.Visit(fieldDefinition, Context.None);
		}
	}

	public static void Accept(this MethodDefinition methodDefinition, Visitor visitor)
	{
		if (visitor != null && methodDefinition != null)
		{
			visitor.Visit(methodDefinition, Context.None);
		}
	}

	public static void Accept(this PropertyDefinition propertyDefinition, Visitor visitor)
	{
		if (visitor != null && propertyDefinition != null)
		{
			visitor.Visit(propertyDefinition, Context.None);
		}
	}
}
