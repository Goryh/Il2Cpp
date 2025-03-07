namespace Unity.IL2CPP.DataModel.Awesome.CFG;

internal class TryCatchInfo
{
	public int TryStart;

	public int TryEnd;

	public int CatchStart;

	public int CatchEnd;

	public int FinallyStart;

	public int FinallyEnd;

	public int FaultStart;

	public int FaultEnd;

	public int FilterStart;

	public int FilterEnd;

	public override string ToString()
	{
		return $"try {TryStart}:{TryEnd}, filter {FilterStart}:{FilterEnd}, catch {CatchStart}:{CatchEnd}, finally {FinallyStart}:{FinallyEnd}, fault {FaultStart}:{FaultEnd}";
	}
}
