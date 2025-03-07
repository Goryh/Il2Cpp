using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.DataModel.BuildLogic;

internal static class DebuggerHelpers
{
	[Conditional("DEBUG")]
	public static void ForceFullNameBuildsForDefinitionsIfDebugging(TypeContext context, TinyProfiler2 tinyProfiler)
	{
		using (tinyProfiler.Section("ForceFullNameBuildsForDefinitionsIfDebugging"))
		{
			if (!Debugger.IsAttached)
			{
				return;
			}
			ParallelHelpers.ForEach(context.AssembliesOrderedByCostToProcess, delegate(AssemblyDefinition data)
			{
				using (tinyProfiler.Section(data.Name.Name))
				{
					foreach (TypeDefinition allType in data.GetAllTypes())
					{
						_ = allType;
					}
					foreach (MethodDefinition item in data.AllMethods())
					{
						_ = item;
					}
					foreach (FieldDefinition item2 in data.AllFields())
					{
						_ = item2;
					}
				}
			}, context.Parameters.EnableSerial);
		}
	}

	[Conditional("DEBUG")]
	public static void ForceFullNameBuildsForNonDefinitionsIfDebugging(TypeContext context, TinyProfiler2 tinyProfiler, ReadOnlyCollection<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>> allNonDefinitionTypes, ReadOnlyCollection<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>> allNonDefinitionMethods, ReadOnlyCollection<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>> allNonDefinitionFields)
	{
		using (tinyProfiler.Section("ForceFullNameBuildsForNonDefinitionsIfDebugging"))
		{
			if (!Debugger.IsAttached)
			{
				return;
			}
			ParallelHelpers.ForEach(((IEnumerable<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>>)allNonDefinitionTypes).Select((Func<UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference>, MemberReference>)((UnderConstructionMember<TypeReference, Mono.Cecil.TypeReference> u) => u.Ours)).Concat(((IEnumerable<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>>)allNonDefinitionMethods).Select((Func<UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference>, MemberReference>)((UnderConstructionMember<MethodReference, Mono.Cecil.MethodReference> u) => u.Ours))).Concat(((IEnumerable<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>>)allNonDefinitionFields).Select((Func<UnderConstruction<FieldReference, Mono.Cecil.FieldReference>, MemberReference>)((UnderConstruction<FieldReference, Mono.Cecil.FieldReference> u) => u.Ours)))
				.ToArray()
				.Chunk(context.Parameters.JobCount), delegate(ReadOnlyCollection<MemberReference> data)
			{
				using (tinyProfiler.Section("Chunk"))
				{
					foreach (MemberReference datum in data)
					{
						_ = datum;
					}
				}
			}, context.Parameters.EnableSerial);
		}
	}

	[Conditional("DEBUG")]
	private static void ForceInitializeFullNameForDebugger(MemberReference member)
	{
		_ = member.FullName;
	}

	[Conditional("DEBUG")]
	internal static void ForceInitializeFullNameForDebuggerIfDebuggerAttached(MemberReference member)
	{
		_ = Debugger.IsAttached;
	}
}
