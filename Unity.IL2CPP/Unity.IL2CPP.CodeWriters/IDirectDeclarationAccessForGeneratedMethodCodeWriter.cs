using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public interface IDirectDeclarationAccessForGeneratedMethodCodeWriter
{
	void AddVirtualMethodDeclarationData(VirtualMethodDeclarationData data);

	bool AddMethodDeclaration(MethodReference method);

	bool AddSharedMethodDeclaration(MethodReference method);

	void TryAddInternalPInvokeMethodDeclarationsForForcedInternalPInvoke(string methodName, string pinvokeDeclaration);

	void TryAddInternalPInvokeMethodDeclarations(string methodName, string pinvokeDeclaration);
}
