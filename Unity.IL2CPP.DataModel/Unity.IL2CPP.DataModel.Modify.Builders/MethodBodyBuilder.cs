namespace Unity.IL2CPP.DataModel.Modify.Builders;

public class MethodBodyBuilder
{
	public ILProcessorBuilder IlProcessorBuilder { get; }

	public MethodDefinitionBuilder Method { get; }

	internal MethodBodyBuilder(MethodDefinitionBuilder methodBuilder)
	{
		Method = methodBuilder;
		IlProcessorBuilder = new ILProcessorBuilder(methodBuilder);
	}
}
