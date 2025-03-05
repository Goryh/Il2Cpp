using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.DataModel.Comparers;

namespace Unity.IL2CPP;

public class VirtualMethodDeclarationDataComparer : IEqualityComparer<VirtualMethodDeclarationData>
{
	public static readonly VirtualMethodDeclarationDataComparer Default = new VirtualMethodDeclarationDataComparer();

	private VirtualMethodDeclarationDataComparer()
	{
	}

	public bool Equals(VirtualMethodDeclarationData left, VirtualMethodDeclarationData right)
	{
		if (MethodSignatureEqualityComparer.AreEqual(left.Method, right.Method) && ((left.CallType == right.CallType) & (left.ReturnsVoid == right.ReturnsVoid)) && left.DoCallViaInvoker == right.DoCallViaInvoker)
		{
			return left.Parameters.SequenceEqual(right.Parameters);
		}
		return false;
	}

	public int GetHashCode(VirtualMethodDeclarationData data)
	{
		return data.GetHashCode();
	}
}
