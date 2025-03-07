using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

[DebuggerDisplay("{Ours}, {Source}")]
internal struct UnderConstructionMember<TOurs, TSource> where TOurs : MemberReference where TSource : Mono.Cecil.MemberReference
{
	public readonly TOurs Ours;

	public readonly TSource Source;

	public readonly CecilSourcedAssemblyData CecilData;

	public UnderConstructionMember(TOurs ours, TSource source, CecilSourcedAssemblyData cecilData)
	{
		Ours = ours;
		Source = source;
		CecilData = cecilData;
	}
}
