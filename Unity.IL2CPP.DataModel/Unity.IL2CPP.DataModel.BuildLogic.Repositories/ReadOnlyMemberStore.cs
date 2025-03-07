namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class ReadOnlyMemberStore : MemberStore
{
	public ReadOnlyMemberStore(MemberStore other)
		: base(new ReadonlyMemberStorageStrategy(), other)
	{
	}
}
