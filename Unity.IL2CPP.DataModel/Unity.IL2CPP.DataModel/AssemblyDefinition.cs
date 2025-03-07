using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.DataModel.BuildLogic.Naming;
using Unity.IL2CPP.DataModel.Modify.Definitions;

namespace Unity.IL2CPP.DataModel;

public class AssemblyDefinition : ICustomAttributeProvider, IMetadataTokenProvider, IAssemblyDefinitionUpdater
{
	protected readonly TypeContext _context;

	private List<TypeDefinition> _allTypes;

	private List<MethodDefinition> _allMethods;

	private List<FieldDefinition> _allFields;

	private List<EventDefinition> _allEvents;

	private List<PropertyDefinition> _allProperties;

	private ReadOnlyCollection<GenericParameter> _allGenericParameters;

	private uint _highestTokenType;

	private uint _highestTokenMethods;

	private uint _highestTokenFields;

	private uint _highestTokenProperties;

	private uint _highestTokenEvents;

	private uint _highestTokenParameters;

	private uint _highestTokenInterfaceImpls;

	private ReadOnlyCollection<CustomAttribute> _customAttributes;

	private ReadOnlyCollection<AssemblyDefinition> _references;

	private ModuleDefinition _mainModule;

	private bool _entryPointInitialized;

	private MethodDefinition _entryPoint;

	public bool LoadedForExportsOnly { get; }

	public AssemblyNameReference Name { get; }

	public string CleanFileName { get; }

	public string FullName { get; }

	public NPath Path { get; }

	public ReadOnlyCollection<AssemblyDefinition> References
	{
		get
		{
			if (_references == null)
			{
				throw new UninitializedDataAccessException($"[{GetType()}] {this}.{"References"} has not been initialized yet.");
			}
			return _references;
		}
	}

	public MethodDefinition EntryPoint
	{
		get
		{
			if (!_entryPointInitialized)
			{
				throw new UninitializedDataAccessException("EntryPoint");
			}
			return _entryPoint;
		}
	}

	public ReadOnlyCollection<CustomAttribute> CustomAttributes
	{
		get
		{
			if (_customAttributes == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _customAttributes;
		}
	}

	public ModuleDefinition MainModule
	{
		get
		{
			if (_mainModule == null)
			{
				throw new ArgumentException("Data has not been initialized yet");
			}
			return _mainModule;
		}
	}

	public MetadataToken MetadataToken { get; }

	internal AssemblyDefinition(Mono.Cecil.AssemblyDefinition assemblyDefinition, TypeContext context, bool loadedForExportsOnly)
	{
		LoadedForExportsOnly = loadedForExportsOnly;
		MetadataToken = MetadataToken.FromCecil(assemblyDefinition);
		_context = context;
		Name = new AssemblyNameReference(assemblyDefinition.Name);
		FullName = assemblyDefinition.FullName;
		Path = assemblyDefinition.MainModule.FileName?.ToNPath();
		CleanFileName = BuildCleanAssemblyFileName(context, assemblyDefinition.MainModule.FileName, assemblyDefinition.MainModule.Name);
	}

	protected AssemblyDefinition(string name, Version version, TypeContext context, MetadataToken metadataToken)
	{
		MetadataToken = metadataToken;
		_context = context;
		Name = new AssemblyNameReference(name, version);
		FullName = name;
		Path = null;
		CleanFileName = BuildCleanAssemblyFileName(context, null, name);
	}

	public virtual ReadOnlyCollection<TypeDefinition> GetAllTypes()
	{
		return _allTypes.AsReadOnly();
	}

	public IEnumerable<GenericParameter> AllGenericParameters()
	{
		return _allGenericParameters;
	}

	public ReadOnlyCollection<MethodDefinition> AllMethods()
	{
		return _allMethods.AsReadOnly();
	}

	public IEnumerable<CallSite> AllCallSites()
	{
		foreach (MethodDefinition method in AllMethods())
		{
			if (!method.HasBody)
			{
				continue;
			}
			foreach (Instruction instruction in method.Body.Instructions)
			{
				if (instruction.Operand is CallSite callSite)
				{
					yield return callSite;
				}
			}
		}
	}

	public IEnumerable<FieldDefinition> AllFields()
	{
		return _allFields;
	}

	public IEnumerable<PropertyDefinition> AllProperties()
	{
		return _allProperties;
	}

	public IEnumerable<EventDefinition> AllEvents()
	{
		return _allEvents;
	}

	internal void InitializeMembers(List<TypeDefinition> allTypes, List<MethodDefinition> allMethods, ReadOnlyCollection<GenericParameter> allGenericParameters, List<FieldDefinition> allFields, List<EventDefinition> allEvents, List<PropertyDefinition> allProperties)
	{
		_allTypes = allTypes;
		_allMethods = allMethods;
		_allEvents = allEvents;
		_allFields = allFields;
		_allProperties = allProperties;
		_allGenericParameters = allGenericParameters;
		_highestTokenType = MaxToken(allMethods, MetadataToken.TypeDefZero);
		_highestTokenMethods = MaxToken(allMethods, MetadataToken.MethodDefZero);
		_highestTokenFields = MaxToken(allFields, MetadataToken.FieldDefZero);
		_highestTokenProperties = MaxToken(allProperties, MetadataToken.PropertyDefZero);
		_highestTokenEvents = MaxToken(allEvents, MetadataToken.EventDefZero);
		_highestTokenInterfaceImpls = MaxToken(allTypes.SelectMany((TypeDefinition t) => t.Interfaces), MetadataToken.InterfaceImplementationZero);
		_highestTokenParameters = MaxToken(allMethods.SelectMany((MethodDefinition m) => m.Parameters), MetadataToken.ParamZero);
	}

	internal void InitializeEntryPoint(MethodDefinition entryPoint)
	{
		_entryPointInitialized = true;
		_entryPoint = entryPoint;
	}

	public TypeDefinition ThisIsSlowFindType(string @namespace, string name)
	{
		foreach (TypeDefinition type in GetAllTypes())
		{
			if (type.Name == name && type.Namespace == @namespace)
			{
				return type;
			}
		}
		if (!MainModule.HasExportedTypes)
		{
			return null;
		}
		foreach (TypeReference type2 in MainModule.ExportedTypes)
		{
			if (type2.Name == name && type2.Namespace == @namespace)
			{
				return (TypeDefinition)type2;
			}
		}
		return null;
	}

	public override string ToString()
	{
		return Name.Name;
	}

	internal void InitializeCustomAttributes(ReadOnlyCollection<CustomAttribute> customAttributes)
	{
		_customAttributes = customAttributes;
	}

	internal void InitializeMainModule(ModuleDefinition mainModule)
	{
		_mainModule = mainModule;
	}

	internal void InitializeReferences(ReadOnlyCollection<AssemblyDefinition> references)
	{
		_references = references;
	}

	internal MetadataToken IssueNewMethodToken()
	{
		return new MetadataToken(TokenType.Method, ++_highestTokenMethods);
	}

	internal MetadataToken IssueNewFieldToken()
	{
		return new MetadataToken(TokenType.Field, ++_highestTokenFields);
	}

	internal MetadataToken IssueNewPropertyToken()
	{
		return new MetadataToken(TokenType.Property, ++_highestTokenProperties);
	}

	internal MetadataToken IssueNewEventToken()
	{
		return new MetadataToken(TokenType.Event, ++_highestTokenEvents);
	}

	internal MetadataToken IssueNewInterfaceImplementationToken()
	{
		return new MetadataToken(TokenType.InterfaceImpl, ++_highestTokenInterfaceImpls);
	}

	internal MetadataToken IssueNewParameterDefinitionToken()
	{
		return new MetadataToken(TokenType.Param, ++_highestTokenParameters);
	}

	internal MetadataToken IssueNewTypeDefinitionMetadataToken()
	{
		return new MetadataToken(TokenType.TypeDef, ++_highestTokenType);
	}

	void IAssemblyDefinitionUpdater.AddGeneratedMethod(MethodDefinition method)
	{
		_allMethods.Add(method);
	}

	void IAssemblyDefinitionUpdater.AddGeneratedEvent(EventDefinition @event)
	{
		_allEvents.Add(@event);
	}

	void IAssemblyDefinitionUpdater.AddGeneratedProperty(PropertyDefinition property)
	{
		_allProperties.Add(property);
	}

	void IAssemblyDefinitionUpdater.AddGeneratedField(FieldDefinition field)
	{
		_allFields.Add(field);
	}

	void IAssemblyDefinitionUpdater.AddGeneratedType(TypeDefinition type)
	{
		_allTypes.Add(type);
	}

	protected static uint MaxToken(IEnumerable<IMetadataTokenProvider> providers, MetadataToken zero)
	{
		uint highest = zero.RID;
		foreach (IMetadataTokenProvider provider in providers)
		{
			highest = Math.Max(highest, provider.MetadataToken.RID);
		}
		return highest;
	}

	private static string BuildCleanAssemblyFileName(TypeContext context, string fileName, string name)
	{
		using Returnable<StringBuilder> builderContext = context.PerThreadObjects.CheckoutStringBuilder();
		StringBuilder value = builderContext.Value;
		value.AppendClean(System.IO.Path.GetFileNameWithoutExtension(fileName ?? name));
		return value.ToString();
	}
}
