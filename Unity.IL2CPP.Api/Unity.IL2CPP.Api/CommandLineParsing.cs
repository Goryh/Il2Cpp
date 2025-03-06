using System;
using System.Linq;
using System.Reflection;
using Unity.Api.Attributes;

namespace Unity.IL2CPP.Api;

public static class CommandLineParsing
{
	public static Il2CppCommandLineArguments ParseIntoObjectGraph(string[] args, Func<string[], object[], Func<OptionAliasAttribute, string>, Func<Type, string, object>, Func<FieldInfo, string, string[]>, string, string[]> prepareInstances, out string[] remaining)
	{
		Il2CppCommandLineArguments il2CppCommandLineArguments = new Il2CppCommandLineArguments();
		remaining = ParseIntoObjectGraph(args, il2CppCommandLineArguments, prepareInstances);
		return il2CppCommandLineArguments;
	}

	public static string[] ParseIntoObjectGraph(string[] args, object root, Func<string[], object[], Func<OptionAliasAttribute, string>, Func<Type, string, object>, Func<FieldInfo, string, string[]>, string, string[]> prepareInstances)
	{
		object[] arg = OptionObjectsGraph.ExtractObjectsFromGraph(root).ToArray();
		return prepareInstances(args, arg, (OptionAliasAttribute attr) => attr.Name, null, PreventCompilerAndLinkerFlagsCommaSplitting, null);
	}

	private static string[] PreventCompilerAndLinkerFlagsCommaSplitting(FieldInfo info, string s)
	{
		if ((info.Name == "CompilerFlags" || info.Name == "LinkerFlags") && Enumerable.Contains(s, ','))
		{
			return new string[1] { s };
		}
		return null;
	}
}
