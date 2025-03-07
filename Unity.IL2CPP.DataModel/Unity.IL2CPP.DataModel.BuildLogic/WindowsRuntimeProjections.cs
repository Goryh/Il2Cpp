using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.DataModel.BuildLogic;

public class WindowsRuntimeProjections
{
	internal readonly struct ClrProjectionKey
	{
		public readonly string ClrAssembly;

		public readonly string ClrNamespace;

		public readonly string ClrName;

		public ClrProjectionKey(string clrAssembly, string clrNamespace, string clrName)
		{
			ClrAssembly = clrAssembly;
			ClrNamespace = clrNamespace;
			ClrName = clrName;
		}
	}

	internal readonly struct Mapping
	{
		public readonly TypeDefinition ClrType;

		public readonly TypeDefinition WindowsRuntimeType;

		public Mapping(TypeDefinition clrType, TypeDefinition windowsRuntimeType)
		{
			ClrType = clrType;
			WindowsRuntimeType = windowsRuntimeType;
		}
	}

	private readonly TypeContext _typeContext;

	private readonly ReadOnlyDictionary<TypeDefinition, TypeDefinition> _clrTypeToWindowsRuntimeTypeMap;

	private readonly ReadOnlyDictionary<TypeDefinition, TypeDefinition> _windowsRuntimeTypeToCLRTypeMap;

	private readonly ReadOnlyDictionary<ClrProjectionKey, Mapping> _projectedTypes;

	public ReadOnlyDictionary<TypeDefinition, TypeDefinition> ClrTypeToWindowsRuntimeTypeMap => _clrTypeToWindowsRuntimeTypeMap;

	internal WindowsRuntimeProjections(TypeContext typeContext, ReadOnlyDictionary<TypeDefinition, TypeDefinition> clrTypeToWindowsRuntimeTypeMap, ReadOnlyDictionary<TypeDefinition, TypeDefinition> windowsRuntimeTypeToCLRTypeMap, ReadOnlyDictionary<ClrProjectionKey, Mapping> projectedTypes)
	{
		_typeContext = typeContext;
		_clrTypeToWindowsRuntimeTypeMap = clrTypeToWindowsRuntimeTypeMap;
		_windowsRuntimeTypeToCLRTypeMap = windowsRuntimeTypeToCLRTypeMap;
		_projectedTypes = projectedTypes;
	}

	public TypeReference ProjectToWindowsRuntime(TypeReference clrType, ITypeFactory typeFactory)
	{
		if (clrType is TypeSpecification && !clrType.IsGenericInstance)
		{
			return clrType;
		}
		if (clrType.IsGenericParameter)
		{
			return clrType;
		}
		if (_clrTypeToWindowsRuntimeTypeMap.TryGetValue(clrType.Resolve(), out var windowsRuntimeType))
		{
			return new TypeResolver(clrType as GenericInstanceType, null, _typeContext, typeFactory).Resolve(windowsRuntimeType);
		}
		return clrType;
	}

	public TypeDefinition ProjectToWindowsRuntime(TypeDefinition clrType)
	{
		if (_clrTypeToWindowsRuntimeTypeMap.TryGetValue(clrType, out var windowsRuntimeType))
		{
			return windowsRuntimeType;
		}
		return clrType;
	}

	public TypeReference ProjectToCLR(TypeReference windowsRuntimeType)
	{
		if (windowsRuntimeType is TypeSpecification && !windowsRuntimeType.IsGenericInstance)
		{
			return windowsRuntimeType;
		}
		if (windowsRuntimeType.IsGenericParameter)
		{
			return windowsRuntimeType;
		}
		if (_windowsRuntimeTypeToCLRTypeMap.TryGetValue(windowsRuntimeType.Resolve(), out var clrType))
		{
			return new TypeResolver(windowsRuntimeType as GenericInstanceType, null, _typeContext, _typeContext.CreateThreadSafeFactoryForFullConstruction()).Resolve(clrType);
		}
		return windowsRuntimeType;
	}

	public TypeDefinition ProjectToCLR(TypeDefinition windowsRuntimeType)
	{
		if (_windowsRuntimeTypeToCLRTypeMap.TryGetValue(windowsRuntimeType, out var clrType))
		{
			return clrType;
		}
		return windowsRuntimeType;
	}

	public bool WasProjected(string clrAssembly, string clrNamespace, string clrName, out TypeDefinition clrType, out TypeDefinition windowsRuntimeType)
	{
		if (!_projectedTypes.TryGetValue(new ClrProjectionKey(clrAssembly, clrNamespace, clrName), out var mapping))
		{
			clrType = null;
			windowsRuntimeType = null;
			return false;
		}
		clrType = mapping.ClrType;
		windowsRuntimeType = mapping.WindowsRuntimeType;
		return true;
	}
}
