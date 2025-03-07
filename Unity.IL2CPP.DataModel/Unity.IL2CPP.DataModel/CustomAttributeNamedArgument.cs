namespace Unity.IL2CPP.DataModel;

public class CustomAttributeNamedArgument
{
	public readonly string Name;

	public readonly CustomAttributeArgument Argument;

	public CustomAttributeNamedArgument(string name, CustomAttributeArgument argument)
	{
		Name = name;
		Argument = argument;
	}
}
