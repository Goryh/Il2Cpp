using System;

namespace Unity.IL2CPP.DataModel.InjectedInitialize;

public interface ITypeReferenceInjectedInitialize
{
	object GetCppDeclarationsData<TContext>(TContext context, Func<TContext, TypeReference, object> initialize);

	object GetCppDeclarationsDependencies<TContext>(TContext context, Func<TContext, TypeReference, object> initialize);

	int GetCppDeclarationsDepth<TContext>(TContext context, Func<TContext, TypeReference, int> initialize);
}
