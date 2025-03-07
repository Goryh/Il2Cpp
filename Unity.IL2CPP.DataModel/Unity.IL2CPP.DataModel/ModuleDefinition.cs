using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

[DebuggerDisplay("{Assembly}")]
public class ModuleDefinition : ICustomAttributeProvider, IMetadataTokenProvider
{
	private ReadOnlyCollection<TypeReference> _exportedTypes;

	private ReadOnlyCollection<Resource> _resources;

	private ReadOnlyCollection<AssemblyNameReference> _assemblyReferences;

	private bool _moduleInitializerInitialized;

	private MethodDefinition _moduleInitializer;

	public TypeSystem TypeSystem { get; }

	public string Name { get; }

	public string FileName { get; }

	public ModuleKind Kind { get; }

	public MetadataKind MetadataKind { get; }

	public ReadOnlyCollection<CustomAttribute> CustomAttributes { get; }

	public MetadataToken MetadataToken { get; }

	public AssemblyDefinition Assembly { get; }

	public bool HasSymbols { get; }

	public bool HasExportedTypes => ExportedTypes.Count > 0;

	public ReadOnlyCollection<TypeReference> ExportedTypes
	{
		get
		{
			if (_exportedTypes == null)
			{
				throw new UninitializedDataAccessException("ExportedTypes");
			}
			return _exportedTypes;
		}
	}

	public MethodDefinition ModuleInitializer
	{
		get
		{
			if (!_moduleInitializerInitialized)
			{
				throw new UninitializedDataAccessException("ExportedTypes");
			}
			return _moduleInitializer;
		}
	}

	public bool HasResources => Resources.Count > 0;

	public ReadOnlyCollection<Resource> Resources
	{
		get
		{
			if (_resources == null)
			{
				throw new UninitializedDataAccessException($"[{GetType()}] {this}.{"Resources"} has not been initialized yet.");
			}
			return _resources;
		}
	}

	public bool HasAssemblyReferences => AssemblyReferences.Count > 0;

	public ReadOnlyCollection<AssemblyNameReference> AssemblyReferences
	{
		get
		{
			if (_assemblyReferences == null)
			{
				throw new UninitializedDataAccessException($"[{GetType()}] {this}.{"AssemblyReferences"} has not been initialized yet.");
			}
			return _assemblyReferences;
		}
	}

	internal ModuleDefinition(AssemblyDefinition assembly, TypeSystem typeSystem, Mono.Cecil.ModuleDefinition module, ReadOnlyCollection<CustomAttribute> customAttributes)
		: this(assembly, typeSystem, module.Name, module.FileName, MetadataToken.FromCecil(module), (ModuleKind)module.Kind, (MetadataKind)module.MetadataKind, module.HasSymbols, customAttributes)
	{
	}

	internal ModuleDefinition(AssemblyDefinition assembly, TypeSystem typeSystem, string name, string fileName, MetadataToken metadataToken, ModuleKind kind, MetadataKind metadataKind, bool hasSymbols, ReadOnlyCollection<CustomAttribute> customAttributes)
	{
		CustomAttributes = customAttributes;
		Name = name;
		FileName = fileName;
		MetadataToken = metadataToken;
		Assembly = assembly;
		Kind = kind;
		MetadataKind = metadataKind;
		HasSymbols = hasSymbols;
		TypeSystem = typeSystem;
	}

	public TypeDefinition ThisIsSlowFindTypeByFullName(string typeName)
	{
		return Assembly.GetAllTypes().FirstOrDefault((TypeDefinition t) => TypeMatches(t, typeName));
	}

	private static bool TypeMatches(TypeDefinition type, string fullName)
	{
		return type.FullName == fullName;
	}

	internal void InitializeExportedTypes(ReadOnlyCollection<TypeReference> exportedTypes)
	{
		_exportedTypes = exportedTypes;
	}

	internal void InitializeModuleInitializer(MethodDefinition moduleInitializer)
	{
		_moduleInitializer = moduleInitializer;
		_moduleInitializerInitialized = true;
	}

	internal void InitializeResources(ReadOnlyCollection<Resource> resources)
	{
		_resources = resources;
	}

	internal void InitializeAssemblyReferences(ReadOnlyCollection<AssemblyNameReference> references)
	{
		_assemblyReferences = references;
	}
}
