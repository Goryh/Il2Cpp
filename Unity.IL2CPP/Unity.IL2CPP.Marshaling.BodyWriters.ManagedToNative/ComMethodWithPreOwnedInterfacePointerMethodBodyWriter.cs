using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;

internal class ComMethodWithPreOwnedInterfacePointerMethodBodyWriter : ComMethodBodyWriter
{
	public ComMethodWithPreOwnedInterfacePointerMethodBodyWriter(ReadOnlyContext context, MethodReference interfaceMethod)
		: base(context, interfaceMethod, interfaceMethod)
	{
	}

	protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
	{
	}
}
