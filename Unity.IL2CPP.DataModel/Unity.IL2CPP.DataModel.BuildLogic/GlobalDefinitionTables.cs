using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class GlobalDefinitionTables
{
	public readonly Dictionary<Mono.Cecil.TypeDefinition, TypeDefinition> Types = new Dictionary<Mono.Cecil.TypeDefinition, TypeDefinition>();

	public readonly Dictionary<Mono.Cecil.FieldDefinition, FieldDefinition> Fields = new Dictionary<Mono.Cecil.FieldDefinition, FieldDefinition>();

	public readonly Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition> Methods = new Dictionary<Mono.Cecil.MethodDefinition, MethodDefinition>();

	public readonly Dictionary<Mono.Cecil.PropertyDefinition, PropertyDefinition> Properties = new Dictionary<Mono.Cecil.PropertyDefinition, PropertyDefinition>();

	public readonly Dictionary<Mono.Cecil.EventDefinition, EventDefinition> Events = new Dictionary<Mono.Cecil.EventDefinition, EventDefinition>();

	public readonly Dictionary<Mono.Cecil.GenericParameter, GenericParameter> GenericParams = new Dictionary<Mono.Cecil.GenericParameter, GenericParameter>();

	public static GlobalDefinitionTables Build(ReadOnlyCollection<CecilSourcedAssemblyData> assemblyCecilData)
	{
		GlobalDefinitionTables globalDefinitionTables = new GlobalDefinitionTables();
		foreach (CecilSourcedAssemblyData data in assemblyCecilData)
		{
			if (!data.NotAvailable)
			{
				globalDefinitionTables.Add(data.DefinitionTables);
			}
		}
		return globalDefinitionTables;
	}

	private GlobalDefinitionTables()
	{
	}

	public TypeDefinition GetDef(Mono.Cecil.TypeDefinition typeDefinition)
	{
		return Types[typeDefinition];
	}

	private void Add(AssemblyDefinitionTables assemblyTables)
	{
		foreach (UnderConstructionMember<TypeDefinition, Mono.Cecil.TypeDefinition> item in assemblyTables.Types)
		{
			Types.Add(item.Source, item.Ours);
		}
		foreach (UnderConstructionMember<FieldDefinition, Mono.Cecil.FieldDefinition> item2 in assemblyTables.Fields)
		{
			Fields.Add(item2.Source, item2.Ours);
		}
		foreach (UnderConstructionMember<MethodDefinition, Mono.Cecil.MethodDefinition> item3 in assemblyTables.Methods)
		{
			Methods.Add(item3.Source, item3.Ours);
		}
		foreach (UnderConstructionMember<PropertyDefinition, Mono.Cecil.PropertyDefinition> item4 in assemblyTables.Properties)
		{
			Properties.Add(item4.Source, item4.Ours);
		}
		foreach (UnderConstructionMember<EventDefinition, Mono.Cecil.EventDefinition> item5 in assemblyTables.Events)
		{
			Events.Add(item5.Source, item5.Ours);
		}
		foreach (UnderConstructionMember<GenericParameter, Mono.Cecil.GenericParameter> item6 in assemblyTables.GenericParams)
		{
			GenericParams.Add(item6.Source, item6.Ours);
		}
	}
}
