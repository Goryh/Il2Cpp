using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal class ReferenceUsages
{
	public readonly ReadOnlyHashSet<Mono.Cecil.TypeReference> Types;

	public readonly ReadOnlyHashSet<Mono.Cecil.FieldReference> Fields;

	public readonly ReadOnlyHashSet<Mono.Cecil.MethodReference> Methods;

	public readonly ReadOnlyHashSet<GenericTypeReference> GenericInstances;

	internal static ReferenceUsages Empty => new ReferenceUsages(new HashSet<Mono.Cecil.TypeReference>().AsReadOnly(), new HashSet<Mono.Cecil.FieldReference>().AsReadOnly(), new HashSet<Mono.Cecil.MethodReference>().AsReadOnly(), new HashSet<GenericTypeReference>().AsReadOnly());

	public ReferenceUsages(ReadOnlyHashSet<Mono.Cecil.TypeReference> types, ReadOnlyHashSet<Mono.Cecil.FieldReference> fields, ReadOnlyHashSet<Mono.Cecil.MethodReference> methods, ReadOnlyHashSet<GenericTypeReference> genericInstances)
	{
		Types = types;
		Fields = fields;
		Methods = methods;
		GenericInstances = genericInstances;
	}
}
