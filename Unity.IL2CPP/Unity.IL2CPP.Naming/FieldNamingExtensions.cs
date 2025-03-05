using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Naming;

public static class FieldNamingExtensions
{
	public static string ForFieldPadding(this INamingService naming, FieldReference field)
	{
		return field.CppName + "_OffsetPadding";
	}
}
