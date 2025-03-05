using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;

public class WriteFullPerAssemblyCodeMetadata : PerAssemblyScheduledStepAction<GlobalWriteContext>
{
	private readonly string _assemblyMetadataRegistrationVarName;

	private readonly string _codeRegistrationVarName;

	protected override string Name => "Write Assembly Code Metadata";

	public WriteFullPerAssemblyCodeMetadata(string assemblyMetadataRegistrationVarName, string codeRegistrationVarName)
	{
		_assemblyMetadataRegistrationVarName = assemblyMetadataRegistrationVarName;
		_codeRegistrationVarName = codeRegistrationVarName;
	}

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
	{
		PerAssemblyCodeMetadataWriter.Write(context.CreateSourceWritingContext(), item, context.Results.PrimaryWrite.GenericContextCollections[item], _assemblyMetadataRegistrationVarName, _codeRegistrationVarName);
	}
}
