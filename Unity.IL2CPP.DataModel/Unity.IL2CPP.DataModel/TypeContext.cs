using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.IL2CPP.DataModel.BuildLogic.Repositories;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.IL2CPP.DataModel.Creation;
using Unity.IL2CPP.DataModel.Stats;

namespace Unity.IL2CPP.DataModel;

public class TypeContext : IDisposable
{
	private ReadOnlyDictionary<Mono.Cecil.TypeDefinition, TypeDefinition> _typeDefCache;

	private ReadOnlyDictionary<Mono.Cecil.FieldDefinition, FieldDefinition> _fieldDefCache;

	private ReadOnlyDictionary<Mono.Cecil.MethodDefinition, MethodDefinition> _methodDefCache;

	private ReadOnlyDictionary<Mono.Cecil.PropertyDefinition, PropertyDefinition> _propertyDefCache;

	private ReadOnlyDictionary<Mono.Cecil.EventDefinition, EventDefinition> _eventDefCache;

	private ReadOnlyDictionary<Mono.Cecil.GenericParameter, GenericParameter> _genericParamCache;

	private TypeProvider _typeProvider;

	private ITypeFactory _globallySafeFactory;

	private ThreadSafeMemberStore _globalSafeMemberStore;

	private ITypeFactory _readOnlyFactory;

	private Dictionary<string, AssemblyDefinition> _assemblyTable;

	private WindowsRuntimeProjections _windowsRuntimeProjections;

	private TypeDefinition[] _systemTypes;

	private ReadOnlyDictionary<TypeReference, TypeReference> _sharedEnumTypes;

	private TypeReference[] _il2CppTypes;

	private ReadOnlyCollection<TypeDefinition> _graftedArrayInterfaceTypes;

	private ReadOnlyCollection<MethodDefinition> _graftedArrayInterfaceMethods;

	private readonly Statistics _stats;

	private readonly PerThreadObjectFactory _perThreadObjectFactory;

	private bool _frozenDefinitions;

	internal readonly LoadParameters Parameters;

	public AssemblyDefinition SystemAssembly { get; private set; }

	internal GeneratedTypesAssemblyDefinition GeneratedTypesAssembly { get; private set; }

	public AssemblyDefinition WindowsRuntimeMetadataAssembly { get; private set; }

	public ReadOnlyCollection<AssemblyDefinition> AssembliesOrderedByCostToProcess { get; private set; }

	public ReadOnlyCollection<AssemblyDefinition> AssembliesOrderedForPublicExposure { get; private set; }

	public bool WindowsRuntimeAssembliesLoaded { get; private set; }

	public WindowsRuntimeProjections WindowsRuntimeProjections => _windowsRuntimeProjections;

	public TypeProvider TypeProvider => _typeProvider;

	internal Statistics Statistics => _stats;

	internal PerThreadObjectFactory PerThreadObjects => _perThreadObjectFactory;

	public ReadOnlyCollection<TypeDefinition> GraftedArrayInterfaceTypes => _graftedArrayInterfaceTypes;

	public ReadOnlyCollection<MethodDefinition> GraftedArrayInterfaceMethods => _graftedArrayInterfaceMethods;

	internal TypeContext(LoadParameters parameters)
	{
		_stats = new Statistics(this);
		_perThreadObjectFactory = new PerThreadObjectFactory(_stats);
		Parameters = parameters;
	}

	public ITypeFactory CreateThreadSafeFactoryForFullConstruction()
	{
		return _globallySafeFactory;
	}

	public void FreezeInflations()
	{
		_globallySafeFactory = _readOnlyFactory;
	}

	public void FreezeDefinitions()
	{
		_frozenDefinitions = true;
	}

	[Conditional("DEBUG")]
	public void WriteStatistics(TextWriter writer, NPath statsOutputDirectory)
	{
	}

	public EditContext CreateEditContext()
	{
		AssertDefinitionsAreNotFrozen();
		return new EditContext(this);
	}

	internal void AssertDefinitionsAreNotFrozen()
	{
		if (_frozenDefinitions)
		{
			throw new InvalidOperationException("Creation of new definitions and changes to existing definitions are not allowed");
		}
	}

	internal void InitializeAssemblies(AssemblyDefinition systemAssembly, GeneratedTypesAssemblyDefinition generatedTypesAssembly, ReadOnlyCollection<AssemblyDefinition> allAssemblies, bool windowsRuntimeAssembliesLoaded)
	{
		SystemAssembly = systemAssembly;
		GeneratedTypesAssembly = generatedTypesAssembly;
		AssembliesOrderedByCostToProcess = allAssemblies;
		_assemblyTable = new Dictionary<string, AssemblyDefinition>();
		foreach (AssemblyDefinition assembly in allAssemblies)
		{
			_assemblyTable.Add(assembly.Name.Name, assembly);
		}
		WindowsRuntimeAssembliesLoaded = windowsRuntimeAssembliesLoaded;
		if (windowsRuntimeAssembliesLoaded)
		{
			WindowsRuntimeMetadataAssembly = _assemblyTable["WindowsRuntimeMetadata"];
		}
	}

	internal void SetSystemTypes(TypeDefinition[] systemTypes)
	{
		_systemTypes = systemTypes;
	}

	internal void SetIl2CppTypes(TypeReference[] il2cppTypes)
	{
		_il2CppTypes = il2cppTypes;
	}

	internal void SetGraftedArrayInterfaceTypes(ReadOnlyCollection<TypeDefinition> types)
	{
		_graftedArrayInterfaceTypes = types;
	}

	internal void SetGraftedArrayInterfaceMethods(ReadOnlyCollection<MethodDefinition> methods)
	{
		_graftedArrayInterfaceMethods = methods;
	}

	internal void SetSharedEnumTypes(ReadOnlyDictionary<TypeReference, TypeReference> sharedEnumTypes)
	{
		_sharedEnumTypes = sharedEnumTypes;
	}

	internal void SetTypeProvider()
	{
		_typeProvider = new TypeProvider(this);
	}

	internal void SetWindowsRuntimeProjects(WindowsRuntimeProjections projections)
	{
		_windowsRuntimeProjections = projections;
	}

	internal void CompleteBuild(ThreadSafeMemberStore memberStore, ReadOnlyCollection<AssemblyDefinition> assembliesOrderedForPublicExposure)
	{
		_globalSafeMemberStore = memberStore;
		_globallySafeFactory = new FullyConstructedTypeFactory(this, _globalSafeMemberStore);
		_readOnlyFactory = new FullyConstructedTypeFactory(this, new ReadOnlyMemberStore(memberStore));
		AssembliesOrderedForPublicExposure = assembliesOrderedForPublicExposure;
		if (Parameters.FreezeDefinitionsOnLoad)
		{
			FreezeDefinitions();
		}
	}

	internal void InitializeGlobalDefinitionTables(ReadOnlyDictionary<Mono.Cecil.TypeDefinition, TypeDefinition> types, ReadOnlyDictionary<Mono.Cecil.FieldDefinition, FieldDefinition> fields, ReadOnlyDictionary<Mono.Cecil.MethodDefinition, MethodDefinition> methods, ReadOnlyDictionary<Mono.Cecil.PropertyDefinition, PropertyDefinition> properties, ReadOnlyDictionary<Mono.Cecil.EventDefinition, EventDefinition> events, ReadOnlyDictionary<Mono.Cecil.GenericParameter, GenericParameter> genericParams)
	{
		_typeDefCache = types;
		_fieldDefCache = fields;
		_methodDefCache = methods;
		_propertyDefCache = properties;
		_eventDefCache = events;
		_genericParamCache = genericParams;
	}

	internal IEnumerable<TypeReference> AllKnownNonDefinitionTypesUnordered()
	{
		return _globalSafeMemberStore.AllTypesUnordered();
	}

	internal IEnumerable<MethodReference> AllKnownNonDefinitionMethodsUnordered()
	{
		return _globalSafeMemberStore.AllMethodsUnordered();
	}

	internal IEnumerable<FieldReference> AllKnownNonDefinitionFieldsUnordered()
	{
		return _globalSafeMemberStore.AllFieldsUnordered();
	}

	internal TypeDefinition GetDef(Mono.Cecil.TypeReference reference)
	{
		Mono.Cecil.TypeDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved type reference : {reference} in module `{reference.Module}` with scope `{reference.Scope}`");
		}
		return GetDef(resolved);
	}

	internal TypeDefinition GetDef(Mono.Cecil.TypeDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		return _typeDefCache[definition];
	}

	internal FieldDefinition GetDef(Mono.Cecil.FieldReference reference)
	{
		Mono.Cecil.FieldDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved field reference : {reference}");
		}
		return GetDef(resolved);
	}

	internal FieldDefinition GetDef(Mono.Cecil.FieldDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		return _fieldDefCache[definition];
	}

	internal MethodDefinition GetDef(Mono.Cecil.MethodReference reference)
	{
		Mono.Cecil.MethodDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved method reference : {reference}");
		}
		return GetDef(resolved);
	}

	internal MethodDefinition GetDef(Mono.Cecil.MethodDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		return _methodDefCache[definition];
	}

	internal PropertyDefinition GetDef(PropertyReference reference)
	{
		Mono.Cecil.PropertyDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved property reference : {reference}");
		}
		return GetDef(resolved);
	}

	internal PropertyDefinition GetDef(Mono.Cecil.PropertyDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		return _propertyDefCache[definition];
	}

	internal EventDefinition GetDef(EventReference reference)
	{
		Mono.Cecil.EventDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved event reference : {reference}");
		}
		return GetDef(resolved);
	}

	internal EventDefinition GetDef(Mono.Cecil.EventDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		return _eventDefCache[definition];
	}

	internal GenericParameter GetDef(Mono.Cecil.GenericParameter definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException("definition");
		}
		if (!definition.Owner.IsDefinition)
		{
			throw new InvalidOperationException("Found a generic parameter that should have been resolved");
		}
		return _genericParamCache[definition];
	}

	internal GenericParameter GetGenericParameterDef(Mono.Cecil.TypeReference reference, int genericParameterPosition)
	{
		Mono.Cecil.TypeDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved type reference : {reference}");
		}
		return GetDef(resolved.GenericParameters[genericParameterPosition]);
	}

	internal GenericParameter GetGenericParameterDef(Mono.Cecil.MethodReference reference, int genericParameterPosition)
	{
		Mono.Cecil.MethodDefinition resolved = reference.Resolve();
		if (resolved == null)
		{
			throw new ArgumentException($"Unresolved method reference : {reference}");
		}
		return GetDef(resolved.GenericParameters[genericParameterPosition]);
	}

	internal bool IsAssemblyLoaded(string assemblyName)
	{
		return _assemblyTable.ContainsKey(assemblyName);
	}

	public bool TryGetAssembly(AssemblyNameReference assemblyName, out AssemblyDefinition assembly)
	{
		return TryGetAssembly(assemblyName.Name, out assembly);
	}

	public bool TryGetAssembly(string assemblyName, out AssemblyDefinition assembly)
	{
		return _assemblyTable.TryGetValue(assemblyName, out assembly);
	}

	public bool TryGetAssemblyByPath(NPath assemblyFilePath, out AssemblyDefinition assembly)
	{
		assembly = AssembliesOrderedByCostToProcess.FirstOrDefault((AssemblyDefinition asm) => asm.Path == assemblyFilePath);
		return assembly != null;
	}

	public TypeDefinition GetSystemType(SystemType type)
	{
		return _systemTypes[(int)type];
	}

	public TypeReference GetSharedEnumType(TypeReference type)
	{
		if (_sharedEnumTypes.TryGetValue(type.GetUnderlyingEnumType(), out var sharedEnumType))
		{
			return sharedEnumType;
		}
		return type;
	}

	public TypeReference GetIl2CppCustomType(Il2CppCustomType type)
	{
		return _il2CppTypes[(int)type];
	}

	public TypeDefinition ThisIsSlowFindType(string assemblyName, string @namespace, string name)
	{
		if (!TryGetAssembly(assemblyName, out var assembly))
		{
			return null;
		}
		return assembly.ThisIsSlowFindType(@namespace, name);
	}

	public void Dispose()
	{
		_stats.Dispose();
		_perThreadObjectFactory.Dispose();
		_globalSafeMemberStore.Dispose();
	}
}
