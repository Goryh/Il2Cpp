using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Naming;

public static class MethodNaming
{
	public static string NameForAdjustorThunk(this MethodReference method)
	{
		return method.CppName + "_AdjustorThunk";
	}
}
