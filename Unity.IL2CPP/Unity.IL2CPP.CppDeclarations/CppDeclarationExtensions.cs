using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CppDeclarations;

public static class CppDeclarationExtensions
{
	public static CppDeclarationsData GetCppDeclarations(this TypeReference type, ReadOnlyContext context)
	{
		return CppDeclarationDataModelInitializers.GetCppDeclarations(context, type);
	}

	public static int GetCppDeclarationsDepth(this TypeReference type, ReadOnlyContext context)
	{
		return CppDeclarationDataModelInitializers.GetCppDeclarationsDepth(context, type);
	}

	public static ReadOnlyCollection<CppDeclarationsData> GetCppDeclarationsDependencies(this TypeReference type, ReadOnlyContext context)
	{
		return CppDeclarationDataModelInitializers.GetCppDeclarationsDependencies(context, type);
	}

	public static IEnumerable<CppDeclarationsData> GetCppDeclarations(this IEnumerable<TypeReference> types, ReadOnlyContext context)
	{
		return types.Select((TypeReference t) => t.GetCppDeclarations(context));
	}

	public static ReadOnlyCollection<CppDeclarationsData> GetCppDeclarationsDependencies(this CppDeclarationsData data, ReadOnlyContext context)
	{
		return data.Type.GetCppDeclarationsDependencies(context);
	}

	public static IEnumerable<CppDeclarationsData> GetCppDeclarationsDependencies(this IEnumerable<CppDeclarationsData> datas, ReadOnlyContext context)
	{
		return datas.SelectMany((CppDeclarationsData d) => d.GetCppDeclarationsDependencies(context));
	}

	public static int GetCppDeclarationsDepth(this CppDeclarationsData data, ReadOnlyContext context)
	{
		return data.Type.GetCppDeclarationsDepth(context);
	}
}
