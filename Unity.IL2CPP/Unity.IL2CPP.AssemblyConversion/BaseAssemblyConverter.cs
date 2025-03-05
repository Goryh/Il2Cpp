using System;
using Unity.IL2CPP.Api;
using Unity.IL2CPP.AssemblyConversion.Classic;
using Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion;

public abstract class BaseAssemblyConverter
{
	public abstract void Run(AssemblyConversionContext context);

	public static BaseAssemblyConverter CreateFor(ConversionMode mode)
	{
		switch (mode)
		{
		case ConversionMode.Default:
		case ConversionMode.Classic:
			return new ClassicConverter();
		case ConversionMode.PartialPerAssemblyInProcess:
			return new PartialConverter();
		case ConversionMode.FullPerAssemblyInProcess:
			return new FullConverter();
		default:
			throw new ArgumentException($"Unhandled value of {mode}");
		}
	}
}
