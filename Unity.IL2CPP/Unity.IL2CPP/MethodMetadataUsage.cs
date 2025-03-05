using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP;

public class MethodMetadataUsage
{
	private readonly Dictionary<IIl2CppRuntimeType, bool> _types = new Dictionary<IIl2CppRuntimeType, bool>(Il2CppRuntimeTypeEqualityComparer.Default);

	private readonly Dictionary<IIl2CppRuntimeType, bool> _typeInfos = new Dictionary<IIl2CppRuntimeType, bool>(Il2CppRuntimeTypeEqualityComparer.Default);

	private readonly Dictionary<MethodReference, bool> _inflatedMethods = new Dictionary<MethodReference, bool>();

	private readonly Dictionary<Il2CppRuntimeFieldReference, bool> _fieldInfos = new Dictionary<Il2CppRuntimeFieldReference, bool>(Il2CppRuntimeFieldReferenceEqualityComparer.Default);

	private readonly Dictionary<Il2CppRuntimeFieldReference, bool> _fieldRvaInfos = new Dictionary<Il2CppRuntimeFieldReference, bool>(Il2CppRuntimeFieldReferenceEqualityComparer.Default);

	private readonly Dictionary<StringMetadataToken, bool> _stringLiterals = new Dictionary<StringMetadataToken, bool>(StringMetadataTokenComparer.Default);

	private readonly List<string> _initializationStatements = new List<string>();

	public bool UsesMetadata
	{
		get
		{
			if (_types.Count <= 0 && _typeInfos.Count <= 0 && _inflatedMethods.Count <= 0 && _fieldInfos.Count <= 0 && _fieldRvaInfos.Count <= 0)
			{
				return _stringLiterals.Count > 0;
			}
			return true;
		}
	}

	public int UsageCount => _types.Count + _typeInfos.Count + _inflatedMethods.Count + _fieldInfos.Count + _fieldRvaInfos.Count + _stringLiterals.Count;

	public void AddTypeInfo(IIl2CppRuntimeType type, bool inlinedInitialization = false)
	{
		AddItem(_typeInfos, type, inlinedInitialization);
	}

	public void AddIl2CppType(IIl2CppRuntimeType type, bool inlinedInitialization = false)
	{
		AddItem(_types, type, inlinedInitialization);
	}

	public void AddInflatedMethod(MethodReference method, bool inlinedInitialization = false)
	{
		AddItem(_inflatedMethods, method, inlinedInitialization);
	}

	public void AddFieldInfo(Il2CppRuntimeFieldReference field, bool inlinedInitialization = false)
	{
		AddItem(_fieldInfos, field, inlinedInitialization);
	}

	public void AddFieldRvaInfo(Il2CppRuntimeFieldReference field, bool inlinedInitialization = false)
	{
		AddItem(_fieldRvaInfos, field, inlinedInitialization);
	}

	public void AddStringLiteral(string literal, AssemblyDefinition assemblyDefinition, bool inlinedInitialization = false)
	{
		AddItem(_stringLiterals, new StringMetadataToken(literal, assemblyDefinition), inlinedInitialization);
	}

	public void AddInitializationStatement(string statement)
	{
		_initializationStatements.Add(statement);
	}

	public IEnumerable<IIl2CppRuntimeType> GetTypeInfos()
	{
		return _typeInfos.Keys;
	}

	public IEnumerable<IIl2CppRuntimeType> GetIl2CppTypes()
	{
		return _types.Keys;
	}

	public IEnumerable<MethodReference> GetInflatedMethods()
	{
		return _inflatedMethods.Keys;
	}

	public IEnumerable<Il2CppRuntimeFieldReference> GetFieldInfos()
	{
		return _fieldInfos.Keys;
	}

	public IEnumerable<Il2CppRuntimeFieldReference> GetFieldRvaInfos()
	{
		return _fieldRvaInfos.Keys;
	}

	public IEnumerable<StringMetadataToken> GetStringLiterals()
	{
		return _stringLiterals.Keys;
	}

	public IEnumerable<IIl2CppRuntimeType> GetTypeInfosNeedingInit()
	{
		return GetItemsNeedingInit(_typeInfos);
	}

	public IEnumerable<IIl2CppRuntimeType> GetIl2CppTypesNeedingInit()
	{
		return GetItemsNeedingInit(_types);
	}

	public IEnumerable<MethodReference> GetInflatedMethodsNeedingInit()
	{
		return GetItemsNeedingInit(_inflatedMethods);
	}

	public IEnumerable<Il2CppRuntimeFieldReference> GetFieldInfosNeedingInit()
	{
		return GetItemsNeedingInit(_fieldInfos);
	}

	public IEnumerable<Il2CppRuntimeFieldReference> GetFieldRvaInfosNeedingInit()
	{
		return GetItemsNeedingInit(_fieldRvaInfos);
	}

	public IEnumerable<StringMetadataToken> GetStringLiteralsNeedingInit()
	{
		return GetItemsNeedingInit(_stringLiterals);
	}

	public IEnumerable<string> GetInitializationStatements()
	{
		return _initializationStatements;
	}

	private static void AddItem<T>(Dictionary<T, bool> list, T item, bool inlinedInitialization)
	{
		if (!list.ContainsKey(item))
		{
			list.Add(item, inlinedInitialization);
		}
		else if (!inlinedInitialization)
		{
			list[item] = false;
		}
	}

	private static T[] GetItemsNeedingInit<T>(Dictionary<T, bool> items)
	{
		return (from t in items
			where !t.Value
			select t.Key).ToArray();
	}
}
