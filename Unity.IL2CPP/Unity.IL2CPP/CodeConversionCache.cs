using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

public class CodeConversionCache
{
	public const string ChecksumFileName = "code_conversion_checksum.txt";

	private readonly AssemblyDefinition[] _assemblies;

	private readonly string _serializedDataAndOptions;

	private readonly NPath _generatedCodeDirectory;

	private readonly Func<IEnumerable<AssemblyDefinition>, string, string> _hashingFunction;

	public CodeConversionCache(AssemblyConversionContext context)
		: this(context.Results.Initialize.AllAssembliesOrderedByDependency, context.InputData.OutputDir, SerializeDataAndOptions(context), DefaultHashFunction)
	{
	}

	public CodeConversionCache(IEnumerable<AssemblyDefinition> assemblies, NPath generatedCodeDirectory, string serializedDataAndOptions)
		: this(assemblies, generatedCodeDirectory, serializedDataAndOptions, DefaultHashFunction)
	{
	}

	public CodeConversionCache(IEnumerable<AssemblyDefinition> assemblies, NPath generatedCodeDirectory, string serializedDataAndOptions, Func<IEnumerable<AssemblyDefinition>, string, string> hashingFunction)
	{
		_assemblies = assemblies.ToArray();
		_generatedCodeDirectory = generatedCodeDirectory;
		_serializedDataAndOptions = serializedDataAndOptions;
		_hashingFunction = hashingFunction;
	}

	public bool IsUpToDate()
	{
		if (_generatedCodeDirectory.Exists() && _generatedCodeDirectory.FileExists("code_conversion_checksum.txt"))
		{
			string text = File.ReadAllText(_generatedCodeDirectory.Combine("code_conversion_checksum.txt").ToString());
			string currentChecksum = _hashingFunction(_assemblies, _serializedDataAndOptions);
			return text == currentChecksum;
		}
		return false;
	}

	public void Refresh()
	{
		NPath nPath = _generatedCodeDirectory.Combine("code_conversion_checksum.txt");
		nPath.DeleteIfExists();
		File.WriteAllText(nPath.ToString(), _hashingFunction(_assemblies, _serializedDataAndOptions));
	}

	public static string DefaultHashFunction(IEnumerable<AssemblyDefinition> assemblies, string codegenOptions)
	{
		string allHashes = string.Empty;
		foreach (AssemblyDefinition assembly in assemblies)
		{
			allHashes += HashTools.HashOfFile(assembly.MainModule.FileName.ToNPath());
		}
		return HashTools.HashOf(allHashes + codegenOptions);
	}

	private static string SerializeDataAndOptions(AssemblyConversionContext context)
	{
		StringBuilder allFields = new StringBuilder();
		FieldInfo[] fields = typeof(AssemblyConversionParameters).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo field in fields)
		{
			StringBuilder stringBuilder = allFields;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder);
			handler.AppendFormatted(field.Name);
			handler.AppendLiteral("=");
			handler.AppendFormatted<object>(field.GetValue(context.Parameters));
			handler.AppendLiteral(",");
			stringBuilder.Append(ref handler);
		}
		return allFields.ToString();
	}
}
