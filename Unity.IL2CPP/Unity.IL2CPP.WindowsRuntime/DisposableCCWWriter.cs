using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime;

internal class DisposableCCWWriter : IProjectedComCallableWrapperMethodWriter
{
	public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
	{
	}

	public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference closeMethod)
	{
		TypeDefinition closableType = closeMethod.DeclaringType.Resolve();
		MethodDefinition disposeMethod = context.Global.Services.WindowsRuntime.ProjectToCLR(closableType).Methods.Single((MethodDefinition m) => m.Name == "Dispose");
		return new ProjectedMethodBodyWriter(context, disposeMethod, closeMethod);
	}
}
