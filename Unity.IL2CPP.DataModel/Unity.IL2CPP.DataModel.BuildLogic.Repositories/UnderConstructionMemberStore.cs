namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionMemberStore : MemberStore
{
	public UnderConstructionMemberStore()
		: base(new UnderConstructionMemberStorageStrategy())
	{
	}
}
