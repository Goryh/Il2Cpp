using Mono.Cecil;

namespace Unity.IL2CPP.DataModel;

public sealed class PInvokeInfo
{
	public struct ModuleReference
	{
		public string Name { get; }

		public ModuleReference(string name)
		{
			Name = name;
		}
	}

	public PInvokeAttributes Attributes { get; }

	public string EntryPoint { get; }

	public ModuleReference Module { get; }

	public bool IsNoMangle => Attributes.HasFlag(PInvokeAttributes.NoMangle);

	public bool IsCharSetNotSpec => (Attributes & PInvokeAttributes.CharSetMask) == 0;

	public bool IsCharSetAnsi => (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetAnsi;

	public bool IsCharSetUnicode => (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetUnicode;

	public bool IsCharSetAuto => (Attributes & PInvokeAttributes.CharSetMask) == PInvokeAttributes.CharSetMask;

	public bool SupportsLastError => Attributes.HasFlag(PInvokeAttributes.SupportsLastError);

	public bool IsCallConvWinapi => (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvWinapi;

	public bool IsCallConvCdecl => (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvCdecl;

	public bool IsCallConvStdCall => (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvStdCall;

	public bool IsCallConvThiscall => (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvThiscall;

	public bool IsCallConvFastcall => (Attributes & PInvokeAttributes.CallConvMask) == PInvokeAttributes.CallConvFastcall;

	public bool IsBestFitEnabled => (Attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;

	public bool IsBestFitDisabled => (Attributes & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitDisabled;

	public bool IsThrowOnUnmappableCharEnabled => (Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;

	public bool IsThrowOnUnmappableCharDisabled => (Attributes & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharDisabled;

	internal PInvokeInfo(PInvokeAttributes attributes, string entryPoint, ModuleReference module)
	{
		Attributes = attributes;
		EntryPoint = entryPoint;
		Module = module;
	}

	internal static PInvokeInfo FromCecil(Mono.Cecil.MethodDefinition method)
	{
		if (!method.HasPInvokeInfo)
		{
			return None();
		}
		return new PInvokeInfo((PInvokeAttributes)method.PInvokeInfo.Attributes, method.PInvokeInfo.EntryPoint, new ModuleReference(method.PInvokeInfo.Module.Name));
	}

	internal static PInvokeInfo None()
	{
		return null;
	}
}
