namespace Unity.IL2CPP.DataModel.Awesome.CFG;

public class InstructionData
{
	public readonly int StackBefore;

	public readonly int StackAfter;

	public InstructionData(int before, int after)
	{
		StackBefore = before;
		StackAfter = after;
	}
}
