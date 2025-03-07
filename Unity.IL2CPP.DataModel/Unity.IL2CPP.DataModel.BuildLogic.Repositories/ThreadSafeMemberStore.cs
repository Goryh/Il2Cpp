namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class ThreadSafeMemberStore : MemberStore
{
	public ThreadSafeMemberStore(MemberStore other)
		: base(new ThreadSafeMemberStorageStrategy(), other)
	{
	}
}
