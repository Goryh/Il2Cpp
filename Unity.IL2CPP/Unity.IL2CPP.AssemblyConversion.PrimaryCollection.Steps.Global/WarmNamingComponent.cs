using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;

public class WarmNamingComponent : GlobalScheduledStepAction<GlobalPrimaryCollectionContext, object>
{
	protected override string Name => "Warm up Naming";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		if (context.Parameters.EnableSerialConversion)
		{
			return true;
		}
		if (context.Results.Initialize.GenericLimits.MaximumRecursiveGenericDepth == 0)
		{
			return true;
		}
		return false;
	}

	protected override void ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item, object globalState)
	{
		foreach (TypeDefinition type in item.GetAllTypes())
		{
			_ = type.CppName;
			foreach (MethodDefinition method in type.Methods)
			{
				_ = method.CppName;
				_ = method.ReturnType.CppName;
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					_ = parameter.ParameterType.CppName;
				}
				if (!method.HasBody)
				{
					continue;
				}
				foreach (Instruction ins in method.Body.Instructions)
				{
					if (ins.Operand is TypeReference typeReference)
					{
						_ = typeReference.CppName;
					}
					else if (ins.Operand is MethodReference methodReference)
					{
						_ = methodReference.CppName;
					}
				}
			}
			foreach (EventDefinition @event in type.Events)
			{
				_ = @event.EventType.CppName;
			}
			foreach (FieldDefinition field in type.Fields)
			{
				_ = field.FieldType.CppName;
			}
		}
	}

	protected override object CreateGlobalState(GlobalPrimaryCollectionContext context)
	{
		return null;
	}
}
