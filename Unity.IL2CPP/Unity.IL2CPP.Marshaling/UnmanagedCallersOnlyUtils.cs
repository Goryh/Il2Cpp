using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.MethodWriting;

namespace Unity.IL2CPP.Marshaling;

public static class UnmanagedCallersOnlyUtils
{
	public static void WriteCallToRaiseInvalidCallingConvsIfNeeded(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess, ResolvedMethodInfo method)
	{
		WriteCallToRaiseInvalidCallingConvsIfNeeded(writer, metadataAccess, method.ResolvedMethodReference);
	}

	public static void WriteCallToRaiseInvalidCallingConvsIfNeeded(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess, MethodReference method)
	{
		if (method.IsUnmanagedCallersOnly && !method.UnmanagedCallersOnlyInfo.IsValid)
		{
			WriteCallToRaiseInvalidCallingConvs(writer, metadataAccess, method);
		}
	}

	public static void WriteCallToRaiseInvalidCallingConvs(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess, MethodReference method)
	{
		writer.WriteLine($"il2cpp_codegen_raise_invalid_unmanaged_callers_usage(\"{method.UnmanagedCallersOnlyInfo.Error.Replace("\"", "\\\"")}\", {metadataAccess.MethodInfo(method)});");
	}

	public static string GetUnmanagedCallersCallingConv(MethodReference method)
	{
		return method.UnmanagedCallersOnlyInfo.UnmanagedCallingConvention switch
		{
			UnmanagedCallingConvention.Cdecl => "CDECL", 
			UnmanagedCallingConvention.StdCall => "STDCALL", 
			UnmanagedCallingConvention.FastCall => "FASTCALL", 
			UnmanagedCallingConvention.ThisCall => "THISCALL", 
			_ => "", 
		};
	}
}
