using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.DataModel.BuildLogic;

[DebuggerDisplay("{Assembly}")]
internal class AssemblyDefinitionTables
{
	public readonly UnderConstructionCollection<TypeDefinition, Mono.Cecil.TypeDefinition> Types;

	public readonly UnderConstructionCollection<FieldDefinition, Mono.Cecil.FieldDefinition> Fields = new UnderConstructionCollection<FieldDefinition, Mono.Cecil.FieldDefinition>();

	public readonly UnderConstructionCollection<MethodDefinition, Mono.Cecil.MethodDefinition> Methods = new UnderConstructionCollection<MethodDefinition, Mono.Cecil.MethodDefinition>();

	public readonly UnderConstructionCollection<PropertyDefinition, Mono.Cecil.PropertyDefinition> Properties = new UnderConstructionCollection<PropertyDefinition, Mono.Cecil.PropertyDefinition>();

	public readonly UnderConstructionCollection<EventDefinition, Mono.Cecil.EventDefinition> Events = new UnderConstructionCollection<EventDefinition, Mono.Cecil.EventDefinition>();

	public readonly UnderConstructionCollection<GenericParameter, Mono.Cecil.GenericParameter> GenericParams = new UnderConstructionCollection<GenericParameter, Mono.Cecil.GenericParameter>();

	public AssemblyDefinition Assembly { get; }

	public AssemblyDefinitionTables(AssemblyDefinition assembly, Mono.Cecil.AssemblyDefinition source)
	{
		Assembly = assembly;
		Types = new UnderConstructionCollection<TypeDefinition, Mono.Cecil.TypeDefinition>(source.MainModule.Types.Count);
	}
}
