using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CppDeclarations;

[DebuggerDisplay("{Type}")]
public class CppDeclarationsData
{
	public readonly TypeReference Type;

	public readonly ReadOnlyCollection<TypeReference> TypesRequiringInteropGuids;

	public readonly CppDeclarationsInstance Instance;

	public readonly CppDeclarationsInstance Static;

	public readonly CppDeclarationsInstance ThreadStatic;

	public CppDeclarationsData(TypeReference type, CppDeclarationsInstance instance, CppDeclarationsInstance @static, CppDeclarationsInstance threadStatic, ReadOnlyCollection<TypeReference> typesRequiringInteropGuids)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		Type = type;
		Instance = instance;
		Static = @static;
		ThreadStatic = threadStatic;
		TypesRequiringInteropGuids = typesRequiringInteropGuids;
	}
}
