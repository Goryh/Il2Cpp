namespace Unity.IL2CPP.DataModel;

public class ExceptionHandler
{
	public readonly TypeReference CatchType;

	public readonly ExceptionHandlerType HandlerType;

	public readonly Instruction TryStart;

	public readonly Instruction TryEnd;

	public readonly Instruction FilterStart;

	public readonly Instruction HandlerStart;

	public readonly Instruction HandlerEnd;

	internal ExceptionHandler(TypeReference catchType, ExceptionHandlerType handlerType, Instruction tryStart, Instruction tryEnd, Instruction filterStart, Instruction handlerStart, Instruction handlerEnd)
	{
		CatchType = catchType;
		HandlerType = handlerType;
		TryStart = tryStart;
		TryEnd = tryEnd;
		FilterStart = filterStart;
		HandlerStart = handlerStart;
		HandlerEnd = handlerEnd;
	}
}
