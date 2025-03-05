using Unity.IL2CPP.Api;
using Unity.IL2CPP.AssemblyConversion;
using Unity.TinyProfiler;

namespace il2cpp.Conversion;

internal static class ConversionDriver
{
	public static ConversionResults Run(TinyProfiler2 tinyProfiler, Il2CppCommandLineArguments il2CppCommandLineArguments)
	{
		return AssemblyConverter.ConvertAssemblies(tinyProfiler, ContextDataFactory.CreateConversionDataFromOptions(il2CppCommandLineArguments), ContextDataFactory.CreateConversionParametersFromOptions(il2CppCommandLineArguments), ContextDataFactory.CreateTopLevelDataFromOptions(il2CppCommandLineArguments));
	}
}
