namespace Unity.IL2CPP.DataModel.Visitor;

public struct Context
{
	private readonly Role _role;

	public static Context None => new Context(Role.None);

	public Role Role => _role;

	private Context(Role role)
	{
		_role = role;
	}

	public Context Member(object data)
	{
		return new Context(Role.Member);
	}

	public Context BaseType(TypeDefinition data)
	{
		return new Context(Role.BaseType);
	}

	public Context Interface(TypeDefinition data)
	{
		return new Context(Role.Interface);
	}

	public Context InterfaceType(InterfaceImplementation data)
	{
		return new Context(Role.InterfaceType);
	}

	public Context ReturnType(object data)
	{
		return new Context(Role.ReturnType);
	}

	public Context GenericParameter(object data)
	{
		return new Context(Role.GenericParameter);
	}

	public Context Getter(object data)
	{
		return new Context(Role.Getter);
	}

	public Context Setter(object data)
	{
		return new Context(Role.Setter);
	}

	public Context EventAdder(object data)
	{
		return new Context(Role.EventAdder);
	}

	public Context EventRemover(object data)
	{
		return new Context(Role.EventRemover);
	}

	public Context ElementType(object data)
	{
		return new Context(Role.ElementType);
	}

	public Context GenericArgument(object data)
	{
		return new Context(Role.GenericArgument);
	}

	public Context Parameter(object data)
	{
		return new Context(Role.Parameter);
	}

	public Context MethodBody(object data)
	{
		return new Context(Role.MethodBody);
	}

	public Context DeclaringType(object data)
	{
		return new Context(Role.DeclaringType);
	}

	public Context Attribute(object data)
	{
		return new Context(Role.Attribute);
	}

	public Context AttributeConstructor(object data)
	{
		return new Context(Role.AttributeConstructor);
	}

	public Context AttributeType(object data)
	{
		return new Context(Role.AttributeType);
	}

	public Context AttributeArgument(object data)
	{
		return new Context(Role.AttributeArgument);
	}

	public Context AttributeArgumentType(object data)
	{
		return new Context(Role.AttributeArgumentType);
	}

	public Context LocalVariable(object data)
	{
		return new Context(Role.LocalVariable);
	}

	public Context Operand(object data)
	{
		return new Context(Role.Operand);
	}
}
