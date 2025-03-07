using System;

namespace Unity.IL2CPP.DataModel.BuildLogic.Repositories;

internal class UnderConstructionMemberRepositories : IDisposable
{
	public readonly UnderConstructionFieldReferenceRepository Fields;

	public readonly UnderConstructionMethodReferenceRepository Methods;

	public readonly UnderConstructionTypeReferenceRepository Types;

	public UnderConstructionMemberRepositories(TypeContext context, IMemberStore memberStore)
	{
		Types = new UnderConstructionTypeReferenceRepository(context, memberStore);
		Methods = new UnderConstructionMethodReferenceRepository(context, memberStore);
		Fields = new UnderConstructionFieldReferenceRepository(context, memberStore);
	}

	public void Dispose()
	{
		Fields?.Dispose();
		Methods?.Dispose();
		Types?.Dispose();
	}
}
