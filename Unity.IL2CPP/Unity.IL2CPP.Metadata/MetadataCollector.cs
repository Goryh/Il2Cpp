using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata.Fields;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata;

public class MetadataCollector : IMetadataCollectionResults
{
	private MetadataStringsCollector _stringsCollector = new MetadataStringsCollector();

	private readonly Dictionary<FieldDefinition, MetadataFieldInfo> _fields = new Dictionary<FieldDefinition, MetadataFieldInfo>();

	private readonly Dictionary<FieldDefaultValue, int> _fieldDefaultValues = new Dictionary<FieldDefaultValue, int>();

	private readonly Dictionary<ParameterDefaultValue, int> _parameterDefaultValues = new Dictionary<ParameterDefaultValue, int>();

	private readonly List<FieldMarshaledSize> _fieldMarshaledSizes = new List<FieldMarshaledSize>();

	private readonly Dictionary<MethodDefinition, MetadataMethodInfo> _methods = new Dictionary<MethodDefinition, MetadataMethodInfo>();

	private readonly Dictionary<ParameterDefinition, MetadataParameterInfo> _parameters = new Dictionary<ParameterDefinition, MetadataParameterInfo>();

	private readonly Dictionary<PropertyDefinition, int> _properties = new Dictionary<PropertyDefinition, int>();

	private readonly Dictionary<EventDefinition, MetadataEventInfo> _events = new Dictionary<EventDefinition, MetadataEventInfo>();

	private readonly Dictionary<TypeDefinition, MetadataTypeDefinitionInfo> _typeInfos = new Dictionary<TypeDefinition, MetadataTypeDefinitionInfo>();

	private readonly Dictionary<IGenericParameterProvider, int> _genericContainers = new Dictionary<IGenericParameterProvider, int>();

	private readonly Dictionary<GenericParameter, int> _genericParameters = new Dictionary<GenericParameter, int>();

	private readonly Dictionary<GenericParameter, int> _genericParameterConstraintsStart = new Dictionary<GenericParameter, int>();

	private readonly List<IIl2CppRuntimeType> _genericParameterConstraints = new List<IIl2CppRuntimeType>();

	private readonly Dictionary<TypeDefinition, int> _nestedTypesStart = new Dictionary<TypeDefinition, int>();

	private readonly List<int> _nestedTypes = new List<int>();

	private readonly Dictionary<TypeDefinition, int> _interfacesStart = new Dictionary<TypeDefinition, int>();

	private readonly List<IIl2CppRuntimeType> _interfaces = new List<IIl2CppRuntimeType>();

	private readonly Dictionary<TypeDefinition, int> _vtableMethodsStart = new Dictionary<TypeDefinition, int>();

	private readonly List<VTableSlot> _vtableMethods = new List<VTableSlot>();

	private readonly Dictionary<TypeDefinition, int> _interfaceOffsetsStart = new Dictionary<TypeDefinition, int>();

	private readonly List<InterfaceOffset> _interfaceOffsets = new List<InterfaceOffset>();

	private readonly List<byte> _defaultValueData = new List<byte>();

	private readonly Dictionary<ModuleDefinition, int> _modules = new Dictionary<ModuleDefinition, int>();

	private readonly List<TypeReference> _exportedTypes = new List<TypeReference>();

	private readonly Dictionary<AssemblyDefinition, int> _assemblies = new Dictionary<AssemblyDefinition, int>();

	private readonly Dictionary<ModuleDefinition, int> _lowestTypeInfoIndexForModule = new Dictionary<ModuleDefinition, int>();

	private readonly Dictionary<ModuleDefinition, int> _lowestExportedTypeIndexForModule = new Dictionary<ModuleDefinition, int>();

	private readonly Dictionary<AssemblyDefinition, Tuple<int, int>> _firstReferencedAssemblyIndexCache = new Dictionary<AssemblyDefinition, Tuple<int, int>>();

	private readonly List<AssemblyDefinition> _referencedAssemblyTable = new List<AssemblyDefinition>();

	public void AddAssemblies(PrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		foreach (AssemblyDefinition asm in assemblies)
		{
			AddAssembly(context, asm);
		}
	}

	public void Add(PrimaryCollectionContext context, AssemblyDefinition assembly)
	{
		AddAssembly(context, assembly);
	}

	public void AddVTableMethodData(IEnumerable<(TypeDefinition, ReadOnlyCollection<VTableSlot>)> vtableMethodsStart)
	{
		foreach (var item in vtableMethodsStart)
		{
			_vtableMethodsStart.Add(item.Item1, _vtableMethods.Count);
			_vtableMethods.AddRange(item.Item2);
		}
	}

	public void AddInterfaceOffsetData(IEnumerable<(TypeDefinition, ReadOnlyCollection<InterfaceOffset>)> interfaceOffsetsStart)
	{
		foreach (var item in interfaceOffsetsStart)
		{
			_interfaceOffsetsStart.Add(item.Item1, _interfaceOffsets.Count);
			_interfaceOffsets.AddRange(item.Item2);
		}
	}

	public IMetadataCollectionResults Complete(ReadOnlyContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		AddReferencedAssemblyMetadata(context, assemblies);
		return this;
	}

	private void AddAssembly(PrimaryCollectionContext context, AssemblyDefinition assemblyDefinition)
	{
		AddUnique(_assemblies, assemblyDefinition);
		AddString(assemblyDefinition.Name.Name);
		AddString(assemblyDefinition.Name.Culture);
		AddBytes(NameOfAssemblyPublicKeyData(assemblyDefinition.Name), EncodeBlob(assemblyDefinition.Name.PublicKey));
		AddUnique(_modules, assemblyDefinition.MainModule, delegate(ModuleDefinition module)
		{
			AddString(module.GetModuleFileName());
			AddTypeInfos(context, from t in assemblyDefinition.GetAllTypes()
				where !t.IsNested
				select t);
			if (module.HasExportedTypes)
			{
				_lowestExportedTypeIndexForModule.Add(module, _exportedTypes.Count);
				_exportedTypes.AddRange(module.ExportedTypes);
			}
		});
	}

	private static byte[] EncodeBlob(byte[] data)
	{
		int stringLength = data.Length;
		byte[] encodedLength = new byte[4];
		uint sizeForLength;
		if (stringLength < 128)
		{
			sizeForLength = 1u;
			encodedLength[0] = (byte)stringLength;
		}
		else if (stringLength < 16384)
		{
			sizeForLength = 2u;
			encodedLength[0] = (byte)((stringLength >> 8) | 0x80);
			encodedLength[1] = (byte)(stringLength & 0xFF);
		}
		else
		{
			sizeForLength = 4u;
			encodedLength[0] = (byte)((stringLength >> 24) | 0xC0);
			encodedLength[1] = (byte)((stringLength >> 16) & 0xFF);
			encodedLength[2] = (byte)((stringLength >> 8) & 0xFF);
			encodedLength[3] = (byte)(stringLength & 0xFF);
		}
		byte[] encodedArray = new byte[stringLength + sizeForLength + 1];
		Array.Copy(encodedLength, 0L, encodedArray, 0L, sizeForLength);
		Array.Copy(data, 0L, encodedArray, sizeForLength, data.Length);
		return encodedArray;
	}

	public int AddString(string str)
	{
		return _stringsCollector.AddString(str);
	}

	public static string NameOfAssemblyPublicKeyData(AssemblyNameReference assemblyName)
	{
		return assemblyName.Name + "_PublicKey";
	}

	private void AddBytes(string nameOfData, byte[] data)
	{
		_stringsCollector.AddBytes(nameOfData, data);
	}

	public void AddFields(PrimaryCollectionContext context, IEnumerable<FieldDefinition> fields, MarshalType marshalType)
	{
		ITypeCollector typeCollector = context.Global.Collectors.Types;
		FieldDefinition[] fieldsArray = fields.ToArray();
		AddUnique(_fields, fieldsArray, delegate(FieldDefinition field)
		{
			AddString(field.Name);
			return new MetadataFieldInfo(_fields.Count, typeCollector.Add(field.FieldType, (int)field.Attributes));
		});
		AddUnique(_fieldDefaultValues, DefaultValueFromFields(context, this, typeCollector, fieldsArray));
		AddUnique(_fieldMarshaledSizes, MarshaledSizeFromFields(context, this, typeCollector, fieldsArray, marshalType));
	}

	private static IEnumerable<FieldDefaultValue> DefaultValueFromFields(PrimaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<FieldDefinition> fields)
	{
		foreach (FieldDefinition field in fields)
		{
			if (field.HasConstant)
			{
				yield return new FieldDefaultValue(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), (field.Constant == null) ? (-1) : metadataCollector.AddDefaultValueData(field, MetadataUtils.ConstantDataFor(field.Constant, field.FieldType, field.FullName)));
			}
			if (field.InitialValue.Length != 0)
			{
				yield return new FieldDefaultValue(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), metadataCollector.AddDefaultValueData(field, field.InitialValue));
			}
		}
	}

	private static IEnumerable<FieldMarshaledSize> MarshaledSizeFromFields(PrimaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<FieldDefinition> fields, MarshalType marshalType)
	{
		foreach (FieldDefinition field in fields)
		{
			if (field.HasMarshalInfo)
			{
				DefaultMarshalInfoWriter marshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, field.FieldType, marshalType, field.MarshalInfo);
				yield return new FieldMarshaledSize(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), marshalInfoWriter.GetNativeSizeWithoutPointers(context));
			}
		}
	}

	public void AddTypeInfos(PrimaryCollectionContext context, IEnumerable<TypeDefinition> types)
	{
		ITypeCollector typeCollector = context.Global.Collectors.Types;
		AddUnique(_typeInfos, types, delegate(TypeDefinition type)
		{
			AddString(type.Name);
			AddString(type.Namespace);
			AddMethods(context, type.Methods);
			AddFields(context, type.Fields, MarshalType.PInvoke);
			AddProperties(type.Properties);
			AddEvents(context, type.Events);
			if (type.HasNestedTypes)
			{
				AddTypeInfos(context, type.NestedTypes);
				_nestedTypesStart.Add(type, _nestedTypes.Count);
				_nestedTypes.AddRange(type.NestedTypes.Select(GetTypeInfoIndex));
			}
			if (type.HasInterfaces)
			{
				_interfacesStart.Add(type, _interfaces.Count);
				_interfaces.AddRange(type.Interfaces.Select((InterfaceImplementation a) => typeCollector.Add(a.InterfaceType)));
			}
			if (type.HasGenericParameters)
			{
				AddGenericContainer(type);
				AddGenericParameters(context, type.GenericParameters);
			}
			TypeReference typeReference = BaseTypeFor(context, type);
			TypeReference typeReference2 = DeclaringTypeFor(type);
			TypeReference typeReference3 = ElementTypeFor(type);
			int count = _typeInfos.Count;
			if (!_lowestTypeInfoIndexForModule.ContainsKey(type.Module))
			{
				_lowestTypeInfoIndexForModule.Add(type.Module, count);
			}
			else if (_lowestTypeInfoIndexForModule[type.Module] > count)
			{
				_lowestTypeInfoIndexForModule[type.Module] = count;
			}
			return new MetadataTypeDefinitionInfo(count, typeCollector.Add(type), (typeReference != null) ? typeCollector.Add(typeReference) : null, (typeReference2 != null) ? typeCollector.Add(typeReference2) : null, (typeReference3 != null) ? typeCollector.Add(typeReference3) : null);
		});
	}

	private static TypeReference DeclaringTypeFor(TypeDefinition type)
	{
		if (!type.IsNested)
		{
			return null;
		}
		return type.DeclaringType;
	}

	private static TypeReference BaseTypeFor(ReadOnlyContext context, TypeDefinition type)
	{
		return type.GetBaseType(context.Global.Services.TypeFactory);
	}

	private static TypeReference ElementTypeFor(TypeDefinition type)
	{
		if (type.IsEnum)
		{
			return type.GetUnderlyingEnumType();
		}
		return type;
	}

	private void AddProperties(IEnumerable<PropertyDefinition> properties)
	{
		AddUnique(_properties, properties, delegate(PropertyDefinition property)
		{
			AddString(property.Name);
		});
	}

	private void AddEvents(PrimaryCollectionContext context, IEnumerable<EventDefinition> events)
	{
		AddUnique(_events, events, delegate(EventDefinition evt)
		{
			AddString(evt.Name);
			return new MetadataEventInfo(_events.Count, context.Global.Collectors.Types.Add(evt.EventType));
		});
	}

	private void AddMethods(PrimaryCollectionContext context, IEnumerable<MethodDefinition> methods)
	{
		AddUnique(_methods, methods, delegate(MethodDefinition method)
		{
			context.Global.Services.ErrorInformation.CurrentMethod = method;
			AddParameters(context, method.Parameters);
			AddString(method.Name);
			if (method.HasGenericParameters)
			{
				AddGenericContainer(method);
				AddGenericParameters(context, method.GenericParameters);
			}
			return new MetadataMethodInfo(_methods.Count, context.Global.Collectors.Types.Add(method.ReturnType));
		});
	}

	private void AddParameters(PrimaryCollectionContext context, IEnumerable<ParameterDefinition> parameters)
	{
		ParameterDefinition[] parametersArray = parameters.ToArray();
		AddUnique(_parameters, parametersArray, delegate(ParameterDefinition parameter)
		{
			AddString(parameter.Name);
			return new MetadataParameterInfo(_parameters.Count, context.Global.Collectors.Types.Add(parameter.ParameterType, (int)parameter.Attributes));
		});
		AddUnique(_parameterDefaultValues, FromParameters(context, this, context.Global.Collectors.Types, parametersArray));
	}

	private static IEnumerable<ParameterDefaultValue> FromParameters(PrimaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<ParameterDefinition> parameters)
	{
		foreach (ParameterDefinition parameter in parameters)
		{
			if (parameter.HasConstant)
			{
				yield return new ParameterDefaultValue(metadataCollector.GetParameterIndex(parameter), typeCollector.Add(MetadataUtils.GetUnderlyingType(parameter.ParameterType)), (parameter.Constant == null) ? (-1) : metadataCollector.AddDefaultValueData(MetadataUtils.ConstantDataFor(parameter.Constant, parameter.ParameterType, parameter.Name)));
			}
		}
	}

	private void AddGenericContainer(IGenericParameterProvider container)
	{
		AddUnique(_genericContainers, container);
	}

	private void AddGenericParameters(PrimaryCollectionContext context, IEnumerable<GenericParameter> genericParameters)
	{
		AddUnique(_genericParameters, genericParameters, delegate(GenericParameter genericParameter)
		{
			AddString(genericParameter.Name);
			if (genericParameter.Constraints.Count > 0)
			{
				_genericParameterConstraintsStart.Add(genericParameter, _genericParameterConstraints.Count);
				_genericParameterConstraints.AddRange(genericParameter.Constraints.Select((GenericParameterConstraint a) => context.Global.Collectors.Types.Add(a.ConstraintType)));
			}
		});
	}

	private static void AddUnique<T>(Dictionary<T, int> items, IEnumerable<T> itemsToAdd, Action<T> onAdd = null)
	{
		foreach (T item in itemsToAdd)
		{
			AddUnique(items, item, onAdd);
		}
	}

	private static void AddNonUnique<T>(Dictionary<T, int> items, IEnumerable<T> itemsToAdd, Action<T> onAdd = null)
	{
		foreach (T item in itemsToAdd)
		{
			AddNonUnique(items, item, onAdd);
		}
	}

	private static void AddUnique<T, TIndex>(Dictionary<T, TIndex> items, IEnumerable<T> itemsToAdd, Func<T, TIndex> onAdd) where TIndex : MetadataIndex
	{
		foreach (T item in itemsToAdd)
		{
			if (items.TryGetValue(item, out var index))
			{
				throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
			}
			index = onAdd(item);
			items.Add(item, index);
		}
	}

	private static void AddUnique<T>(Dictionary<T, int> items, T item, Action<T> onAdd = null)
	{
		if (items.TryGetValue(item, out var _))
		{
			throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
		}
		AddItem(items, item, onAdd);
	}

	private static void AddNonUnique<T>(Dictionary<T, int> items, T item, Action<T> onAdd = null)
	{
		if (!items.TryGetValue(item, out var _))
		{
			AddItem(items, item, onAdd);
		}
	}

	private static void AddItem<T>(Dictionary<T, int> items, T item, Action<T> onAdd)
	{
		int index = items.Count;
		items.Add(item, index);
		onAdd?.Invoke(item);
	}

	private static void AddUnique<T>(List<T> items, IEnumerable<T> itemsToAdd)
	{
		foreach (T item in itemsToAdd)
		{
			AddUnique(items, item);
		}
	}

	private static void AddUnique<T>(List<T> items, T item)
	{
		if (items.Contains(item))
		{
			throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
		}
		items.Add(item);
	}

	public ReadOnlyCollection<byte> GetStringData()
	{
		return _stringsCollector.GetStringData();
	}

	public int GetStringIndex(string str)
	{
		return _stringsCollector.GetStringIndex(str);
	}

	public ReadOnlyCollection<KeyValuePair<FieldDefinition, MetadataFieldInfo>> GetFields()
	{
		return _fields.ItemsSortedByValue();
	}

	public int GetFieldIndex(FieldDefinition field)
	{
		return _fields[field].Index;
	}

	private int AddDefaultValueData(FieldDefinition field, byte[] data)
	{
		if (field.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
		{
			return AddDefaultValueData(data, 8);
		}
		return AddDefaultValueData(data);
	}

	private int AddDefaultValueData(byte[] data, int alignment)
	{
		for (int paddingForAlignment = alignment - _defaultValueData.Count % alignment; paddingForAlignment > 0; paddingForAlignment--)
		{
			_defaultValueData.Add(0);
		}
		return AddDefaultValueData(data);
	}

	private int AddDefaultValueData(byte[] data)
	{
		int count = _defaultValueData.Count;
		_defaultValueData.AddRange(data);
		return count;
	}

	public ReadOnlyCollection<FieldDefaultValue> GetFieldDefaultValues()
	{
		return _fieldDefaultValues.KeysSortedByValue();
	}

	public ReadOnlyCollection<ParameterDefaultValue> GetParameterDefaultValues()
	{
		return _parameterDefaultValues.KeysSortedByValue();
	}

	public ReadOnlyCollection<byte> GetDefaultValueData()
	{
		return _defaultValueData.AsReadOnly();
	}

	public ReadOnlyCollection<FieldMarshaledSize> GetFieldMarshaledSizes()
	{
		return _fieldMarshaledSizes.ToSortedCollectionBy((FieldMarshaledSize x) => x.FieldIndex);
	}

	public ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> GetTypeInfos()
	{
		return _typeInfos.ItemsSortedByValue();
	}

	public int GetTypeInfoIndex(TypeDefinition type)
	{
		return _typeInfos[type].Index;
	}

	public ReadOnlyCollection<KeyValuePair<MethodDefinition, MetadataMethodInfo>> GetMethods()
	{
		return _methods.ItemsSortedByValue();
	}

	public int GetMethodIndex(MethodDefinition method)
	{
		return _methods[method].Index;
	}

	public ReadOnlyCollection<KeyValuePair<ParameterDefinition, MetadataParameterInfo>> GetParameters()
	{
		return _parameters.ItemsSortedByValue();
	}

	public int GetParameterIndex(ParameterDefinition parameter)
	{
		return _parameters[parameter].Index;
	}

	public ReadOnlyCollection<PropertyDefinition> GetProperties()
	{
		return _properties.KeysSortedByValue();
	}

	public int GetPropertyIndex(PropertyDefinition property)
	{
		return _properties[property];
	}

	public ReadOnlyCollection<KeyValuePair<EventDefinition, MetadataEventInfo>> GetEvents()
	{
		return _events.ItemsSortedByValue();
	}

	public int GetEventIndex(EventDefinition @event)
	{
		return _events[@event].Index;
	}

	public ReadOnlyCollection<IGenericParameterProvider> GetGenericContainers()
	{
		return _genericContainers.KeysSortedByValue();
	}

	public int GetGenericContainerIndex(IGenericParameterProvider container)
	{
		if (_genericContainers.TryGetValue(container, out var index))
		{
			return index;
		}
		return -1;
	}

	public ReadOnlyCollection<GenericParameter> GetGenericParameters()
	{
		return _genericParameters.KeysSortedByValue();
	}

	public int GetGenericParameterIndex(GenericParameter genericParameter)
	{
		return _genericParameters[genericParameter];
	}

	public ReadOnlyCollection<IIl2CppRuntimeType> GetGenericParameterConstraints()
	{
		return _genericParameterConstraints.ToArray().AsReadOnly();
	}

	public int GetGenericParameterConstraintsStartIndex(GenericParameter genericParameter)
	{
		return _genericParameterConstraintsStart[genericParameter];
	}

	public ReadOnlyCollection<int> GetNestedTypes()
	{
		return _nestedTypes.ToArray().AsReadOnly();
	}

	public int GetNestedTypesStartIndex(TypeDefinition type)
	{
		return _nestedTypesStart[type];
	}

	public ReadOnlyCollection<IIl2CppRuntimeType> GetInterfaces()
	{
		return _interfaces.ToArray().AsReadOnly();
	}

	public int GetInterfacesStartIndex(TypeDefinition type)
	{
		return _interfacesStart[type];
	}

	public ReadOnlyCollection<VTableSlot> GetVTableMethods()
	{
		return _vtableMethods.ToArray().AsReadOnly();
	}

	public int GetVTableMethodsStartIndex(TypeDefinition type)
	{
		if (_vtableMethodsStart.TryGetValue(type, out var index))
		{
			return index;
		}
		return -1;
	}

	public ReadOnlyCollection<InterfaceOffset> GetInterfaceOffsets()
	{
		return _interfaceOffsets.ToArray().AsReadOnly();
	}

	public int GetInterfaceOffsetsStartIndex(TypeDefinition type)
	{
		return _interfaceOffsetsStart[type];
	}

	public ReadOnlyCollection<TypeReference> GetExportedTypes()
	{
		return _exportedTypes.AsReadOnly();
	}

	public ReadOnlyCollection<ModuleDefinition> GetModules()
	{
		return _modules.KeysSortedByValue();
	}

	public int GetModuleIndex(ModuleDefinition module)
	{
		return _modules[module];
	}

	public ReadOnlyCollection<AssemblyDefinition> GetAssemblies()
	{
		return _assemblies.KeysSortedByValue();
	}

	public int GetAssemblyIndex(AssemblyDefinition assembly)
	{
		return _assemblies[assembly];
	}

	public ReadOnlyCollection<AssemblyDefinition> GetReferencedAssemblyTable()
	{
		return _referencedAssemblyTable.AsReadOnly();
	}

	private void AddReferencedAssemblyMetadata(ReadOnlyContext context, ICollection<AssemblyDefinition> assemblies)
	{
		ITinyProfilerService tinyProfiler = context.Global.Services.TinyProfiler;
		foreach (AssemblyDefinition asm in assemblies)
		{
			using (tinyProfiler.Section("AddReferencedAssemblyMetadata", asm.Name.Name))
			{
				ReadOnlyCollection<AssemblyDefinition> referencedAssemblies = asm.References;
				if (referencedAssemblies.Count == 0)
				{
					_firstReferencedAssemblyIndexCache.Add(asm, new Tuple<int, int>(-1, 0));
					continue;
				}
				_firstReferencedAssemblyIndexCache.Add(asm, new Tuple<int, int>(_referencedAssemblyTable.Count, referencedAssemblies.Count));
				_referencedAssemblyTable.AddRange(referencedAssemblies.Distinct());
			}
		}
	}

	public int GetFirstIndexInReferencedAssemblyTableForAssembly(AssemblyDefinition assembly, out int length)
	{
		Tuple<int, int> data = _firstReferencedAssemblyIndexCache[assembly];
		length = data.Item2;
		return data.Item1;
	}

	public int GetLowestTypeInfoIndexForModule(ModuleDefinition image)
	{
		return _lowestTypeInfoIndexForModule[image];
	}

	public int GetLowestExportedTypeIndexForModule(ModuleDefinition image)
	{
		return _lowestExportedTypeIndexForModule[image];
	}
}
