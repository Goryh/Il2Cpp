using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.Fields;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;

public class CollectMetadata : ScheduledTwoInItemsStepFuncWithContinueFunc<GlobalPrimaryCollectionContext, AssemblyDefinition, ReadOnlyCollection<AssemblyDefinition>, (CollectMetadata.PerAssemblyMetadata, MetadataCollector), IMetadataCollectionResults>
{
	public class PerAssemblyMetadata
	{
		public readonly AssemblyDefinition Assembly;

		private readonly List<(TypeDefinition, ReadOnlyCollection<VTableSlot>)> _vtableMethods = new List<(TypeDefinition, ReadOnlyCollection<VTableSlot>)>();

		private readonly List<(TypeDefinition, ReadOnlyCollection<InterfaceOffset>)> _interfaceOffsets = new List<(TypeDefinition, ReadOnlyCollection<InterfaceOffset>)>();

		public PerAssemblyMetadata(AssemblyDefinition assembly)
		{
			Assembly = assembly;
		}

		public void AddVTables(PrimaryCollectionContext context, ReadOnlyCollection<TypeDefinition> types)
		{
			foreach (TypeDefinition type in types)
			{
				if (type.IsInterface && !type.IsComOrWindowsRuntimeType() && context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(type) == null)
				{
					continue;
				}
				VTable vtable = context.Global.Services.VTable.VTableFor(context, type);
				foreach (VTableSlot slot in vtable.Slots)
				{
					MethodReference slotMethod = slot.Method;
					if (slotMethod != null && (slotMethod.IsGenericInstance || slotMethod.DeclaringType.IsGenericInstance))
					{
						context.Global.Collectors.GenericMethods.Add(context, slotMethod);
					}
				}
				_vtableMethods.Add((type, vtable.Slots));
				_interfaceOffsets.Add((type, vtable.InterfaceOffsets.Select((KeyValuePair<TypeReference, int> pair) => new InterfaceOffset(context.Global.Collectors.Types.Add(pair.Key), pair.Value)).ToList().AsReadOnly()));
			}
		}

		public ReadOnlyCollection<(TypeDefinition, ReadOnlyCollection<VTableSlot>)> GetVTableData()
		{
			return _vtableMethods.AsReadOnly();
		}

		public ReadOnlyCollection<(TypeDefinition, ReadOnlyCollection<InterfaceOffset>)> GetInterfaceOffsetData()
		{
			return _interfaceOffsets.AsReadOnly();
		}
	}

	private class NotAvailable : IMetadataCollectionResults
	{
		public ReadOnlyCollection<KeyValuePair<EventDefinition, MetadataEventInfo>> GetEvents()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<KeyValuePair<FieldDefinition, MetadataFieldInfo>> GetFields()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<FieldDefaultValue> GetFieldDefaultValues()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<byte> GetDefaultValueData()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<KeyValuePair<MethodDefinition, MetadataMethodInfo>> GetMethods()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<KeyValuePair<ParameterDefinition, MetadataParameterInfo>> GetParameters()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<PropertyDefinition> GetProperties()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<byte> GetStringData()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> GetTypeInfos()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<IGenericParameterProvider> GetGenericContainers()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<GenericParameter> GetGenericParameters()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<IIl2CppRuntimeType> GetGenericParameterConstraints()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<ParameterDefaultValue> GetParameterDefaultValues()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<FieldMarshaledSize> GetFieldMarshaledSizes()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<int> GetNestedTypes()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<IIl2CppRuntimeType> GetInterfaces()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<VTableSlot> GetVTableMethods()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<InterfaceOffset> GetInterfaceOffsets()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<TypeReference> GetExportedTypes()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<ModuleDefinition> GetModules()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<AssemblyDefinition> GetAssemblies()
		{
			throw new NotSupportedException();
		}

		public ReadOnlyCollection<AssemblyDefinition> GetReferencedAssemblyTable()
		{
			throw new NotSupportedException();
		}

		public int GetTypeInfoIndex(TypeDefinition type)
		{
			throw new NotSupportedException();
		}

		public int GetEventIndex(EventDefinition @event)
		{
			throw new NotSupportedException();
		}

		public int GetFieldIndex(FieldDefinition field)
		{
			throw new NotSupportedException();
		}

		public int GetMethodIndex(MethodDefinition method)
		{
			throw new NotSupportedException();
		}

		public int GetParameterIndex(ParameterDefinition parameter)
		{
			throw new NotSupportedException();
		}

		public int GetPropertyIndex(PropertyDefinition property)
		{
			throw new NotSupportedException();
		}

		public int GetStringIndex(string str)
		{
			throw new NotSupportedException();
		}

		public int GetGenericContainerIndex(IGenericParameterProvider container)
		{
			throw new NotSupportedException();
		}

		public int GetGenericParameterIndex(GenericParameter genericParameter)
		{
			throw new NotSupportedException();
		}

		public int GetGenericParameterConstraintsStartIndex(GenericParameter genericParameter)
		{
			throw new NotSupportedException();
		}

		public int GetNestedTypesStartIndex(TypeDefinition type)
		{
			throw new NotSupportedException();
		}

		public int GetInterfacesStartIndex(TypeDefinition type)
		{
			throw new NotSupportedException();
		}

		public int GetVTableMethodsStartIndex(TypeDefinition type)
		{
			throw new NotSupportedException();
		}

		public int GetInterfaceOffsetsStartIndex(TypeDefinition type)
		{
			throw new NotSupportedException();
		}

		public int GetExportedTypeIndex(TypeReference exportedType)
		{
			throw new NotSupportedException();
		}

		public int GetModuleIndex(ModuleDefinition module)
		{
			throw new NotSupportedException();
		}

		public int GetAssemblyIndex(AssemblyDefinition assembly)
		{
			throw new NotSupportedException();
		}

		public int GetFirstIndexInReferencedAssemblyTableForAssembly(AssemblyDefinition assembly, out int length)
		{
			throw new NotSupportedException();
		}

		public int GetLowestTypeInfoIndexForModule(ModuleDefinition image)
		{
			throw new NotSupportedException();
		}

		public int GetLowestExportedTypeIndexForModule(ModuleDefinition image)
		{
			throw new NotSupportedException();
		}
	}

	private readonly ReadOnlyCollection<AssemblyDefinition> _assemblies;

	protected override string Name => "Collect Metadata";

	protected override string PostProcessingSectionName => "Final Merge Metadata";

	public CollectMetadata(ReadOnlyCollection<AssemblyDefinition> assemblies)
	{
		_assemblies = assemblies;
	}

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
	{
		return workerItem.Name.Name;
	}

	protected override (PerAssemblyMetadata, MetadataCollector) ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
	{
		PerAssemblyMetadata metadata = new PerAssemblyMetadata(item);
		using (context.Services.TinyProfiler.Section("Collect VTables"))
		{
			metadata.AddVTables(context.CreateCollectionContext(), item.GetAllTypes());
		}
		return (metadata, null);
	}

	protected override (PerAssemblyMetadata, MetadataCollector) ProcessItem(GlobalPrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> item)
	{
		MetadataCollector metadataCollector = new MetadataCollector();
		foreach (AssemblyDefinition assemblyData in item)
		{
			metadataCollector.Add(context.CreateCollectionContext(), assemblyData);
		}
		return (null, metadataCollector);
	}

	protected override string ProfilerDetailsForItem2(ReadOnlyCollection<AssemblyDefinition> workerItem)
	{
		return "Global Collect Metadata";
	}

	protected override IMetadataCollectionResults CreateEmptyResult()
	{
		return new NotAvailable();
	}

	protected override ReadOnlyCollection<object> OrderItemsForScheduling(GlobalSchedulingContext context, ReadOnlyCollection<AssemblyDefinition> items, ReadOnlyCollection<ReadOnlyCollection<AssemblyDefinition>> items2)
	{
		return new object[1] { items2.First() }.Concat(items).ToList().AsReadOnly();
	}

	protected override IMetadataCollectionResults PostProcess(GlobalPrimaryCollectionContext context, ReadOnlyCollection<(PerAssemblyMetadata, MetadataCollector)> data)
	{
		MetadataCollector metadataCollector = data.Single(((PerAssemblyMetadata, MetadataCollector) d) => d.Item2 != null).Item2;
		foreach (PerAssemblyMetadata result in from d in data
			select d.Item1 into d
			where d != null
			select d)
		{
			metadataCollector.AddVTableMethodData(result.GetVTableData());
			metadataCollector.AddInterfaceOffsetData(result.GetInterfaceOffsetData());
		}
		return metadataCollector.Complete(context.CreateCollectionContext(), _assemblies);
	}
}
