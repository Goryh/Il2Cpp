using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.DataModel.BuildLogic.Utils;

namespace Unity.IL2CPP.DataModel.BuildLogic.CecilLoading;

internal class WindowsRuntimeLoading
{
	public static void MergeAssembliesIntoMetadataAssemblies(WindowsRuntimeLoadContext loadContext, LoadParameters parameters, NewWindowsRuntimeAwareMetadataResolver metadataResolver)
	{
		metadataResolver.PopulateWinmds(loadContext.WindowsRuntimeAssemblies);
		ParallelHelpers.ForEach(loadContext.WindowsRuntimeAssemblies, ImmediateReadEnoughToMove, parameters.EnableSerial);
		foreach (Mono.Cecil.AssemblyDefinition assembly in loadContext.WindowsRuntimeAssemblies)
		{
			AddToMetadataAssembly(loadContext.MetadataAssembly, assembly);
		}
		UpdateEmptyTokens(loadContext.MetadataAssembly);
		ValidateTokens(loadContext.MetadataAssembly);
		metadataResolver.MetadataAssemblyMergeComplete(loadContext.MetadataAssembly);
	}

	private static void ImmediateReadEnoughToMove(Mono.Cecil.AssemblyDefinition assembly)
	{
		assembly.MainModule.ImmediateRead();
		ClearMetadataTokens(CecilExtensions.AllDefinedTypes(assembly).ToArray());
	}

	private static void AddToMetadataAssembly(Mono.Cecil.AssemblyDefinition metadataAssembly, Mono.Cecil.AssemblyDefinition otherAssembly)
	{
		List<Mono.Cecil.TypeDefinition> list = new List<Mono.Cecil.TypeDefinition>(otherAssembly.MainModule.Types.Count);
		list.AddRange(otherAssembly.MainModule.Types);
		otherAssembly.MainModule.Types.Clear();
		foreach (Mono.Cecil.TypeDefinition type in list)
		{
			if (!(type.Name == "<Module>"))
			{
				metadataAssembly.MainModule.Types.Add(type);
			}
		}
		foreach (Mono.Cecil.AssemblyNameReference assemblyReference in otherAssembly.MainModule.AssemblyReferences)
		{
			metadataAssembly.MainModule.AssemblyReferences.Add(assemblyReference);
		}
	}

	private static void ClearMetadataTokens(ICollection<Mono.Cecil.TypeDefinition> types)
	{
		ClearTokens(types);
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Interfaces));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.GenericParameters));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.GenericParameters));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.Parameters));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Properties));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Fields));
		ClearTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Events));
	}

	private static void ClearTokens(IEnumerable<Mono.Cecil.IMetadataTokenProvider> providers)
	{
		foreach (Mono.Cecil.IMetadataTokenProvider provider in providers)
		{
			provider.MetadataToken = Mono.Cecil.MetadataToken.Zero;
		}
	}

	private static void UpdateEmptyTokens(Mono.Cecil.AssemblyDefinition asm)
	{
		Mono.Cecil.TypeDefinition[] types = asm.MainModule.GetAllTypes().ToArray();
		UpdateTokens(types, Mono.Cecil.TokenType.TypeDef);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Interfaces), Mono.Cecil.TokenType.InterfaceImpl);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.GenericParameters).Concat(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.GenericParameters)), Mono.Cecil.TokenType.GenericParam);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods), Mono.Cecil.TokenType.Method);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.Parameters), Mono.Cecil.TokenType.Param);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Properties), Mono.Cecil.TokenType.Property);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Fields), Mono.Cecil.TokenType.Field);
		UpdateTokens(types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Events), Mono.Cecil.TokenType.Event);
	}

	private static void UpdateTokens(IEnumerable<Mono.Cecil.IMetadataTokenProvider> providers, Mono.Cecil.TokenType type)
	{
		uint max = 0u;
		List<Mono.Cecil.IMetadataTokenProvider> invalidTokens = new List<Mono.Cecil.IMetadataTokenProvider>();
		foreach (Mono.Cecil.IMetadataTokenProvider item in providers)
		{
			max = Math.Max(max, item.MetadataToken.RID);
			if (item.MetadataToken.RID == 0)
			{
				invalidTokens.Add(item);
			}
		}
		foreach (Mono.Cecil.IMetadataTokenProvider item2 in invalidTokens)
		{
			item2.MetadataToken = new Mono.Cecil.MetadataToken(type, ++max);
		}
	}

	private static void ValidateTokens(Mono.Cecil.AssemblyDefinition asm)
	{
		Dictionary<uint, Mono.Cecil.IMetadataTokenProvider> tokens = new Dictionary<uint, Mono.Cecil.IMetadataTokenProvider>();
		Mono.Cecil.TypeDefinition[] types = asm.MainModule.GetAllTypes().ToArray();
		ValidateTokens(tokens, types);
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Interfaces));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.GenericParameters));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.GenericParameters));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Methods).SelectMany((Mono.Cecil.MethodDefinition m) => m.Parameters));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Properties));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Fields));
		ValidateTokens(tokens, types.SelectMany((Mono.Cecil.TypeDefinition t) => t.Events));
	}

	private static void ValidateTokens(Dictionary<uint, Mono.Cecil.IMetadataTokenProvider> tokens, IEnumerable<Mono.Cecil.IMetadataTokenProvider> providers)
	{
		foreach (Mono.Cecil.IMetadataTokenProvider provider in providers)
		{
			if (provider.MetadataToken.RID == 0)
			{
				throw new Exception();
			}
			uint token = provider.MetadataToken.ToUInt32();
			if (tokens.TryGetValue(token, out var existing))
			{
				throw new InvalidOperationException($"Duplicate metadata token 0x{token:X} for '{existing}' and '{provider}'");
			}
			tokens.Add(token, provider);
		}
	}
}
