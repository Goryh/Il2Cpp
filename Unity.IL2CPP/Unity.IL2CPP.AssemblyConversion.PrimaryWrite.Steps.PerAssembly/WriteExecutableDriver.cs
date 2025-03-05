using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;

public class WriteExecutableDriver : SimpleScheduledStep<GlobalWriteContext>
{
	private readonly AssemblyDefinition _entryAssembly;

	protected override string Name => "Executable Processing";

	public WriteExecutableDriver(AssemblyDefinition entryAssembly)
	{
		_entryAssembly = entryAssembly;
	}

	protected override bool Skip(GlobalSchedulingContext context)
	{
		return _entryAssembly == null;
	}

	protected override void Worker(GlobalWriteContext context)
	{
		NPath assemblyFolder = ((context.InputData.ExecutableAssembliesFolder != null) ? context.InputData.ExecutableAssembliesFolder : new NPath(_entryAssembly.MainModule.FileName).Parent);
		new DriverWriter(context.CreateSourceWritingContext(), _entryAssembly).Write(assemblyFolder);
	}
}
