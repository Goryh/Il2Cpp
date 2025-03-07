using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.DataModel;

public sealed class UnmanagedCallersOnlyInfo
{
	public UnmanagedCallingConvention UnmanagedCallingConvention { get; }

	public string EntryPointName { get; }

	public string Error { get; }

	public bool IsValid => Error == null;

	private UnmanagedCallersOnlyInfo(UnmanagedCallingConvention callingConvention, string entryPointName, string error)
	{
		UnmanagedCallingConvention = callingConvention;
		EntryPointName = entryPointName;
		Error = error;
	}

	public static UnmanagedCallersOnlyInfo FromCecil(Mono.Cecil.MethodDefinition methodDefinition)
	{
		Mono.Cecil.CustomAttribute unmanagedCallersOnly = methodDefinition.CustomAttributes.FirstOrDefault((Mono.Cecil.CustomAttribute ca) => ca.AttributeType.FullNameEquals("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute"));
		if (unmanagedCallersOnly == null)
		{
			return null;
		}
		UnmanagedCallingConvention callingConvention = UnmanagedCallingConvention.PlatformDefault;
		List<string> errors = new List<string>();
		if (methodDefinition.HasThis)
		{
			errors.Add("An instance method may not have the UnmanagedCallersOnly attribute");
		}
		if (methodDefinition.ContainsGenericParameters())
		{
			errors.Add("A generic method method may not have the UnmanagedCallersOnly attribute");
		}
		if (unmanagedCallersOnly.Fields.FirstOrDefault((Mono.Cecil.CustomAttributeNamedArgument f) => f.Name == "CallConvs").Argument.Value is Mono.Cecil.CustomAttributeArgument[] argumentArray)
		{
			foreach (Mono.Cecil.TypeReference callConvType in argumentArray.Select((Mono.Cecil.CustomAttributeArgument a) => a.Value).OfType<Mono.Cecil.TypeReference>())
			{
				if (callConvType.Namespace != "System.Runtime.CompilerServices")
				{
					errors?.Add("Invalid calling convention type - " + callConvType.FullName);
					break;
				}
				switch (callConvType.Name)
				{
				case "CallConvCdecl":
					if (callingConvention != 0)
					{
						errors.Add("Multiple calling conventions are not supported");
					}
					callingConvention = UnmanagedCallingConvention.Cdecl;
					break;
				case "CallConvStdcall":
					if (callingConvention != 0)
					{
						errors.Add("Multiple calling conventions are not supported");
					}
					callingConvention = UnmanagedCallingConvention.StdCall;
					break;
				case "CallConvFastcall":
					if (callingConvention != 0)
					{
						errors.Add("Multiple calling conventions are not supported");
					}
					callingConvention = UnmanagedCallingConvention.FastCall;
					break;
				case "CallConvThiscall":
					if (callingConvention != 0)
					{
						errors.Add("Multiple calling conventions are not supported");
					}
					callingConvention = UnmanagedCallingConvention.ThisCall;
					break;
				case "CallConvSuppressGCTransition":
					errors.Add("SuppressGCTransition is not valid on a managed method");
					break;
				default:
					errors.Add("Unsupported calling convention type - " + callConvType.FullName);
					break;
				}
			}
		}
		string entryPointName = (string)unmanagedCallersOnly.Fields.SingleOrDefault((Mono.Cecil.CustomAttributeNamedArgument f) => f.Name == "EntryPoint").Argument.Value;
		string errorMessage = ((errors.Count > 0) ? string.Join(" : ", errors.Distinct()) : null);
		return new UnmanagedCallersOnlyInfo((errorMessage == null) ? callingConvention : UnmanagedCallingConvention.PlatformDefault, entryPointName, errorMessage);
	}
}
