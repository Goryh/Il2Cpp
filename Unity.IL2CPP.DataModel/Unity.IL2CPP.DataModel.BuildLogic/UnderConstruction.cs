using System.Diagnostics;

namespace Unity.IL2CPP.DataModel.BuildLogic;

[DebuggerDisplay("{Ours}, {Source}")]
internal readonly struct UnderConstruction<TOurs, TSource>
{
	public readonly TOurs Ours;

	public readonly TSource Source;

	public UnderConstruction(TOurs ours, TSource source)
	{
		Ours = ours;
		Source = source;
	}
}
