using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP;

public struct ExtraTypesSupport
{
	private readonly IEnumerable<AssemblyDefinition> _usedAssemblies;

	private readonly ITypeFactory _typeFactory;

	public ExtraTypesSupport(IEnumerable<AssemblyDefinition> usedAssemblies, ITypeFactory typeFactory)
	{
		_usedAssemblies = usedAssemblies;
		_typeFactory = typeFactory;
	}

	public bool TryAddType(TypeNameParseInfo typeNameInfo, out TypeReference type)
	{
		type = null;
		try
		{
			type = TypeReferenceFor(typeNameInfo);
			return true;
		}
		catch (TypeResolutionException)
		{
			return false;
		}
	}

	private TypeReference TypeReferenceFor(TypeNameParseInfo typeNameInfo)
	{
		TypeReference typeReference = GetTypeByName(CecilElementTypeNameFor(typeNameInfo), typeNameInfo.Assembly);
		if (typeReference == null)
		{
			throw new TypeResolutionException(typeNameInfo);
		}
		if (typeNameInfo.HasGenericArguments)
		{
			typeReference = _typeFactory.CreateGenericInstanceType((TypeDefinition)typeReference, null, typeNameInfo.TypeArguments.Select(TypeReferenceFor).ToArray());
		}
		if (typeNameInfo.IsPointer)
		{
			int indirectionCount = typeNameInfo.Modifiers.Count((int m) => m == -1);
			PointerType pointerType = _typeFactory.CreatePointerType(typeReference);
			for (int i = 1; i < indirectionCount; i++)
			{
				pointerType = _typeFactory.CreatePointerType(pointerType);
			}
			typeReference = pointerType;
		}
		if (typeNameInfo.IsArray)
		{
			ArrayType arrayType = _typeFactory.CreateArrayType(typeReference, typeNameInfo.Ranks[0], typeNameInfo.Ranks[0] == 1);
			for (int j = 1; j < typeNameInfo.Ranks.Length; j++)
			{
				arrayType = _typeFactory.CreateArrayType(arrayType, typeNameInfo.Ranks[j], typeNameInfo.Ranks[j] == 1);
			}
			typeReference = arrayType;
		}
		return typeReference;
	}

	private static string CecilElementTypeNameFor(TypeNameParseInfo typeNameInfo)
	{
		if (!typeNameInfo.IsNested)
		{
			return typeNameInfo.ElementTypeName;
		}
		string baseName = typeNameInfo.Name;
		if (!string.IsNullOrEmpty(typeNameInfo.Namespace))
		{
			baseName = typeNameInfo.Namespace + "." + baseName;
		}
		return typeNameInfo.Nested.Aggregate(baseName, (string c, string n) => c + "/" + n);
	}

	private TypeReference GetTypeByName(string name, AssemblyNameParseInfo assembly)
	{
		if (string.IsNullOrEmpty(assembly?.Name))
		{
			return _usedAssemblies.Select((AssemblyDefinition a) => a.MainModule.ThisIsSlowFindTypeByFullName(name)).FirstOrDefault((TypeDefinition t) => t != null);
		}
		return _usedAssemblies.FirstOrDefault((AssemblyDefinition a) => a.Name.Name == assembly.Name)?.MainModule.ThisIsSlowFindTypeByFullName(name);
	}

	public static IEnumerable<string> BuildExtraTypesList(NPath[] extraTypesFiles)
	{
		HashSet<string> extraTypeList = new HashSet<string>();
		foreach (NPath path in extraTypesFiles)
		{
			try
			{
				foreach (string line in from l in File.ReadAllLines(path)
					select l.Trim() into l
					where l.Length > 0
					select l)
				{
					if (!line.StartsWith(";") && !line.StartsWith("#") && !line.StartsWith("//"))
					{
						extraTypeList.Add(line);
					}
				}
			}
			catch (Exception)
			{
				ConsoleOutput.Info.WriteLine("WARNING: Cannot open extra file list {0}. Skipping.", path);
			}
		}
		return extraTypeList;
	}
}
