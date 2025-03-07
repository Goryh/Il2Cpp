using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class DefinitionModelBuilder
{
	private readonly TypeContext _context;

	public DefinitionModelBuilder(TypeContext context)
	{
		_context = context;
	}

	public void PopulateDefinitions(GlobalDefinitionTables globalDefinitionTables)
	{
		_context.InitializeGlobalDefinitionTables(globalDefinitionTables.Types.AsReadOnly(), globalDefinitionTables.Fields.AsReadOnly(), globalDefinitionTables.Methods.AsReadOnly(), globalDefinitionTables.Properties.AsReadOnly(), globalDefinitionTables.Events.AsReadOnly(), globalDefinitionTables.GenericParams.AsReadOnly());
	}

	public void BuildAssemblyDefinitionTable(CecilSourcedAssemblyData assemblyData)
	{
		ProcessAssemblyForDefinitions(assemblyData, assemblyData.DefinitionTables);
	}

	private void ProcessAssemblyForDefinitions(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables)
	{
		Mono.Cecil.AssemblyDefinition source = assemblyData.Assembly.Source;
		AssemblyDefinition ours = assemblyData.Assembly.Ours;
		ours.InitializeCustomAttributes(BuildCustomAttrs(source));
		ModuleDefinition mainModule = new ModuleDefinition(ours, new TypeSystem(_context), source.MainModule, BuildCustomAttrs(source.MainModule));
		ours.InitializeMainModule(mainModule);
		foreach (Mono.Cecil.TypeDefinition typeDefinition in source.MainModule.Types)
		{
			ProcessTypeDefinitionForDefs(assemblyData, definitionTables, typeDefinition, null);
		}
		ours.InitializeMembers(definitionTables.Types.Complete(), definitionTables.Methods.Complete(), definitionTables.GenericParams.Complete().AsReadOnly(), definitionTables.Fields.Complete(), definitionTables.Events.Complete(), definitionTables.Properties.Complete());
	}

	private TypeDefinition ProcessTypeDefinitionForDefs(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Mono.Cecil.TypeDefinition typeDefinition, TypeDefinition declaringType)
	{
		TypeDefinition def = new TypeDefinition(_context, definitionTables.Assembly.MainModule, typeDefinition, declaringType, BuildCustomAttrs(typeDefinition));
		definitionTables.Types.Add(def, typeDefinition, assemblyData);
		ProcessNestedTypes(assemblyData, definitionTables, typeDefinition, def);
		ProcessInterfaceImplementations(def, typeDefinition);
		ProcessFields(assemblyData, definitionTables, typeDefinition, def);
		Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> typeMethodMap = ProcessMethods(assemblyData, definitionTables, typeDefinition, def);
		ProcessProperties(assemblyData, definitionTables, typeMethodMap, typeDefinition, def);
		ProcessEvents(assemblyData, definitionTables, typeMethodMap, typeDefinition, def);
		BuildGenericParameters(_context, def, typeDefinition, delegate((Mono.Cecil.GenericParameter, GenericParameter) item)
		{
			definitionTables.GenericParams.Add(item.Item2, item.Item1, assemblyData);
		});
		return def;
	}

	private void ProcessNestedTypes(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Mono.Cecil.TypeDefinition typeDefinition, TypeDefinition def)
	{
		ReadOnlyCollection<TypeDefinition> nestedTypes = ReadOnlyCollectionCache<TypeDefinition>.Empty;
		if (typeDefinition.HasNestedTypes)
		{
			List<TypeDefinition> nestedTypeList = new List<TypeDefinition>(typeDefinition.NestedTypes.Count);
			foreach (Mono.Cecil.TypeDefinition nestedType in typeDefinition.NestedTypes)
			{
				nestedTypeList.Add(ProcessTypeDefinitionForDefs(assemblyData, definitionTables, nestedType, def));
			}
			nestedTypes = nestedTypeList.AsReadOnly();
		}
		def.InitializeNestedTypes(nestedTypes);
	}

	private static void ProcessFields(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Mono.Cecil.TypeDefinition type, TypeDefinition declaringTypeDef)
	{
		if (!type.HasFields)
		{
			declaringTypeDef.InitializeFields(ReadOnlyCollectionCache<FieldDefinition>.Empty);
			return;
		}
		List<FieldDefinition> all = new List<FieldDefinition>(type.Fields.Count);
		foreach (Mono.Cecil.FieldDefinition fieldDefinition in type.Fields)
		{
			FieldDefinition def = new FieldDefinition(declaringTypeDef, BuildCustomAttrs(fieldDefinition), BuildMarshalInfo(fieldDefinition), fieldDefinition);
			all.Add(def);
			definitionTables.Fields.Add(def, fieldDefinition, assemblyData);
		}
		declaringTypeDef.InitializeFields(all.AsReadOnly());
	}

	private static void ProcessInterfaceImplementations(TypeDefinition ourType, Mono.Cecil.TypeDefinition cecilType)
	{
		ReadOnlyCollection<InterfaceImplementation> interfaceImpls = ReadOnlyCollectionCache<InterfaceImplementation>.Empty;
		if (cecilType.HasInterfaces)
		{
			List<InterfaceImplementation> interfaceBuilder = new List<InterfaceImplementation>(cecilType.Interfaces.Count);
			foreach (Mono.Cecil.InterfaceImplementation interfaceImpl in cecilType.Interfaces)
			{
				interfaceBuilder.Add(new InterfaceImplementation(interfaceImpl, BuildCustomAttrs(interfaceImpl)));
			}
			interfaceImpls = interfaceBuilder.AsReadOnly();
		}
		ourType.InitializeInterfaces(interfaceImpls);
	}

	private Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> ProcessMethods(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Mono.Cecil.TypeDefinition type, TypeDefinition declaringTypeDef)
	{
		if (!type.HasMethods)
		{
			declaringTypeDef.InitializeMethods(ReadOnlyCollectionCache<MethodDefinition>.Empty);
			return null;
		}
		Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> methodMapping = new Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition>(type.Methods.Count);
		List<MethodDefinition> all = new List<MethodDefinition>(type.Methods.Count);
		HashSet<string> privateScopeCollisionNameTable = BuildPrivateScopeCollisionNameTable(type);
		foreach (Mono.Cecil.MethodDefinition method in type.Methods)
		{
			bool requiresRidForNameUniqueness = method.IsCompilerControlled && privateScopeCollisionNameTable.Contains(method.Name);
			MethodDefinition def = new MethodDefinition(declaringTypeDef, ParameterDefBuilder.BuildParametersForDefinition(method), BuildCustomAttrs(method), new MethodReturnType(BuildCustomAttrs(method.MethodReturnType), BuildMarshalInfo(method.MethodReturnType), MetadataToken.FromCecil(method.MethodReturnType)), requiresRidForNameUniqueness, method);
			all.Add(def);
			definitionTables.Methods.Add(def, method, assemblyData);
			methodMapping.Add(method, def);
			BuildGenericParameters(_context, def, method, delegate((Mono.Cecil.GenericParameter, GenericParameter) item)
			{
				definitionTables.GenericParams.Add(item.Item2, item.Item1, assemblyData);
			});
		}
		declaringTypeDef.InitializeMethods(all.AsReadOnly());
		return methodMapping;
	}

	private static HashSet<string> BuildPrivateScopeCollisionNameTable(Mono.Cecil.TypeDefinition type)
	{
		HashSet<string> visitedMethods = new HashSet<string>();
		HashSet<string> collisions = new HashSet<string>();
		foreach (Mono.Cecil.MethodDefinition method in type.Methods)
		{
			if (method.IsCompilerControlled)
			{
				if (visitedMethods.Contains(method.Name))
				{
					collisions.Add(method.Name);
				}
				visitedMethods.Add(method.Name);
			}
		}
		return collisions;
	}

	private static void ProcessProperties(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> methodMapping, Mono.Cecil.TypeDefinition type, TypeDefinition declaringTypeDef)
	{
		if (!type.HasProperties)
		{
			declaringTypeDef.InitializeProperties(ReadOnlyCollectionCache<PropertyDefinition>.Empty);
			return;
		}
		List<PropertyDefinition> all = new List<PropertyDefinition>(type.Properties.Count);
		foreach (Mono.Cecil.PropertyDefinition property in type.Properties)
		{
			ReadOnlyCollection<ParameterDefinition> parameters = ReadOnlyCollectionCache<ParameterDefinition>.Empty;
			if (property.HasParameters)
			{
				List<ParameterDefinition> paramBuilder = new List<ParameterDefinition>(property.Parameters.Count);
				foreach (Mono.Cecil.ParameterDefinition parameter in property.Parameters)
				{
					paramBuilder.Add(new ParameterDefinition(parameter, BuildCustomAttrs(parameter), null));
				}
				parameters = paramBuilder.AsReadOnly();
			}
			PropertyDefinition def = new PropertyDefinition(declaringTypeDef, BuildCustomAttrs(property), parameters, (property.GetMethod != null) ? methodMapping[property.GetMethod] : null, (property.SetMethod != null) ? methodMapping[property.SetMethod] : null, property);
			all.Add(def);
			definitionTables.Properties.Add(def, property, assemblyData);
		}
		declaringTypeDef.InitializeProperties(all.AsReadOnly());
	}

	private static void ProcessEvents(CecilSourcedAssemblyData assemblyData, AssemblyDefinitionTables definitionTables, Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> methodMapping, Mono.Cecil.TypeDefinition type, TypeDefinition declaringTypeDef)
	{
		if (!type.HasEvents)
		{
			declaringTypeDef.InitializeEvents(ReadOnlyCollectionCache<EventDefinition>.Empty);
			return;
		}
		List<EventDefinition> all = new List<EventDefinition>(type.Events.Count);
		foreach (Mono.Cecil.EventDefinition eventDefinition in type.Events)
		{
			EventDefinition def = new EventDefinition(declaringTypeDef, BuildCustomAttrs(eventDefinition), (eventDefinition.AddMethod != null) ? methodMapping[eventDefinition.AddMethod] : null, (eventDefinition.RemoveMethod != null) ? methodMapping[eventDefinition.RemoveMethod] : null, (eventDefinition.InvokeMethod != null) ? methodMapping[eventDefinition.InvokeMethod] : null, eventDefinition.HasOtherMethods ? eventDefinition.OtherMethods.Select((Mono.Cecil.MethodDefinition m) => methodMapping[m]).ToArray().AsReadOnly() : ReadOnlyCollectionCache<MethodDefinition>.Empty, eventDefinition);
			all.Add(def);
			definitionTables.Events.Add(def, eventDefinition, assemblyData);
		}
		declaringTypeDef.InitializeEvents(all.AsReadOnly());
	}

	internal static ReadOnlyCollection<CustomAttribute> BuildCustomAttrs(Mono.Cecil.ICustomAttributeProvider customAttributeProvider)
	{
		if (!customAttributeProvider.HasCustomAttributes)
		{
			return ReadOnlyCollectionCache<CustomAttribute>.Empty;
		}
		List<CustomAttribute> customAttrs = new List<CustomAttribute>(customAttributeProvider.CustomAttributes.Count);
		foreach (Mono.Cecil.CustomAttribute customAttribute in customAttributeProvider.CustomAttributes)
		{
			if (CustomAttributeSupport.ShouldProcess(customAttribute))
			{
				customAttrs.Add(new CustomAttribute(customAttribute, BuildCustomAttrArguments(customAttribute), BuildCustomAttrFieldArguments(customAttribute), BuildCustomAttrPropertyArguments(customAttribute)));
			}
		}
		return customAttrs.AsReadOnly();
	}

	private static ReadOnlyCollection<CustomAttributeArgument> BuildCustomAttrArguments(Mono.Cecil.CustomAttribute customAttribute)
	{
		if (!customAttribute.HasConstructorArguments)
		{
			return ReadOnlyCollectionCache<CustomAttributeArgument>.Empty;
		}
		List<CustomAttributeArgument> args = new List<CustomAttributeArgument>(customAttribute.ConstructorArguments.Count);
		foreach (Mono.Cecil.CustomAttributeArgument constructorArg in customAttribute.ConstructorArguments)
		{
			args.Add(new CustomAttributeArgument(constructorArg, constructorArg.Value));
		}
		return args.AsReadOnly();
	}

	private static ReadOnlyCollection<CustomAttributeNamedArgument> BuildCustomAttrFieldArguments(Mono.Cecil.CustomAttribute customAttribute)
	{
		if (!customAttribute.HasFields)
		{
			return ReadOnlyCollectionCache<CustomAttributeNamedArgument>.Empty;
		}
		return BuildCustomAttrsNamedArguments(customAttribute.Fields);
	}

	private static ReadOnlyCollection<CustomAttributeNamedArgument> BuildCustomAttrPropertyArguments(Mono.Cecil.CustomAttribute customAttribute)
	{
		if (!customAttribute.HasProperties)
		{
			return ReadOnlyCollectionCache<CustomAttributeNamedArgument>.Empty;
		}
		return BuildCustomAttrsNamedArguments(customAttribute.Properties);
	}

	private static ReadOnlyCollection<CustomAttributeNamedArgument> BuildCustomAttrsNamedArguments(ICollection<Mono.Cecil.CustomAttributeNamedArgument> namedArguments)
	{
		List<CustomAttributeNamedArgument> args = new List<CustomAttributeNamedArgument>(namedArguments.Count);
		foreach (Mono.Cecil.CustomAttributeNamedArgument namedArg in namedArguments)
		{
			args.Add(new CustomAttributeNamedArgument(namedArg.Name, new CustomAttributeArgument(namedArg.Argument, namedArg.Argument.Value)));
		}
		return args.AsReadOnly();
	}

	internal static MarshalInfo BuildMarshalInfo(Mono.Cecil.IMarshalInfoProvider marshalInfoProvider)
	{
		if (!marshalInfoProvider.HasMarshalInfo)
		{
			return null;
		}
		Mono.Cecil.MarshalInfo marshalInfo = marshalInfoProvider.MarshalInfo;
		if (!(marshalInfo is Mono.Cecil.ArrayMarshalInfo arrayMarshalInfo))
		{
			if (!(marshalInfo is Mono.Cecil.CustomMarshalInfo customMarshalInfo))
			{
				if (!(marshalInfo is Mono.Cecil.FixedArrayMarshalInfo fixedArrayMarshalInfo))
				{
					if (!(marshalInfo is Mono.Cecil.FixedSysStringMarshalInfo fixedSysStringMarshalInfo))
					{
						if (marshalInfo is Mono.Cecil.SafeArrayMarshalInfo safeArrayMarshalInfo)
						{
							return new SafeArrayMarshalInfo((VariantType)safeArrayMarshalInfo.ElementType);
						}
						return new MarshalInfo((NativeType)marshalInfoProvider.MarshalInfo.NativeType);
					}
					return new FixedSysStringMarshalInfo(fixedSysStringMarshalInfo.Size);
				}
				return new FixedArrayMarshalInfo((NativeType)fixedArrayMarshalInfo.ElementType, fixedArrayMarshalInfo.Size);
			}
			return new CustomMarshalInfo(customMarshalInfo.Guid, customMarshalInfo.UnmanagedType, customMarshalInfo.Cookie);
		}
		return new ArrayMarshalInfo((NativeType)arrayMarshalInfo.ElementType, arrayMarshalInfo.SizeParameterIndex, arrayMarshalInfo.Size, arrayMarshalInfo.SizeParameterMultiplier);
	}

	private static void BuildGenericParameters(TypeContext context, IGenericParameterProvider provider, Mono.Cecil.IGenericParameterProvider definition, Action<(Mono.Cecil.GenericParameter, GenericParameter)> genericParamCreated)
	{
		if (definition.HasGenericParameters)
		{
			List<GenericParameter> genericParams = new List<GenericParameter>(definition.GenericParameters.Count);
			foreach (Mono.Cecil.GenericParameter genericParameter in definition.GenericParameters)
			{
				GenericParameter genericParam = new GenericParameter(genericParameter, provider, BuildGenericConstraints(genericParameter), BuildCustomAttrs(genericParameter), context);
				((IGenericParamProviderInitializer)genericParam).InitializeGenericParameters(ReadOnlyCollectionCache<GenericParameter>.Empty);
				genericParamCreated((genericParameter, genericParam));
				genericParams.Add(genericParam);
			}
			((IGenericParamProviderInitializer)provider).InitializeGenericParameters(genericParams.AsReadOnly());
		}
		else
		{
			((IGenericParamProviderInitializer)provider).InitializeGenericParameters(ReadOnlyCollectionCache<GenericParameter>.Empty);
		}
	}

	private static ReadOnlyCollection<GenericParameterConstraint> BuildGenericConstraints(Mono.Cecil.GenericParameter genericParameter)
	{
		if (!genericParameter.HasConstraints)
		{
			return ReadOnlyCollectionCache<GenericParameterConstraint>.Empty;
		}
		List<GenericParameterConstraint> genericParamConstraints = new List<GenericParameterConstraint>(genericParameter.Constraints.Count);
		foreach (Mono.Cecil.GenericParameterConstraint constraint in genericParameter.Constraints)
		{
			genericParamConstraints.Add(new GenericParameterConstraint(BuildCustomAttrs(constraint), constraint));
		}
		return genericParamConstraints.AsReadOnly();
	}
}
