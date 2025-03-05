using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;

public class WarningsScanner : PerAssemblyScheduledStepAction<GlobalSecondaryCollectionContext>
{
	private const int WarningThresholdHighVariableCount = 200;

	private const int WarningThresholdHighInstructionCount = 5000;

	private const int WarningThresholdHighCodeSize = 15000;

	protected override string Name => "Warning Scanner";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item)
	{
		foreach (TypeDefinition allType in item.GetAllTypes())
		{
			foreach (MethodDefinition method in allType.Methods)
			{
				if (method.HasBody)
				{
					MethodBody methodBody = method.Body;
					if (methodBody.Variables.Count > 200)
					{
						context.Services.MessageLogger.LogWarning(method, $"High Variable Count of {methodBody.Variables.Count}");
					}
					if (methodBody.Instructions.Count > 5000)
					{
						context.Services.MessageLogger.LogWarning(method, $"High Instruction Count of {methodBody.Instructions.Count}");
					}
					if (methodBody.CodeSize > 15000)
					{
						context.Services.MessageLogger.LogWarning(method, $"High CodeSize of {methodBody.CodeSize}");
					}
				}
			}
		}
	}
}
