using System;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;

public class WriteFullPerAssemblyCodeRegistration : SimpleScheduledStep<GlobalWriteContext>
{
	protected override string Name => "Write Per Assembly Code Registration";

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return false;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		throw new NotImplementedException();
	}
}
