using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP;

internal class OptimizationWriter : IDisposable
{
	private string[] _platformsWithOptimizationsDisabled;

	private ICodeWriter _writer;

	public OptimizationWriter(ICodeWriter writer, MethodReference method)
	{
		_platformsWithOptimizationsDisabled = OptimizationDatabase.GetPlatformsWithDisabledOptimizations(method);
		if (_platformsWithOptimizationsDisabled != null)
		{
			_writer = writer;
			ICodeWriter writer2 = _writer;
			writer2.WriteLine($"#if {_platformsWithOptimizationsDisabled.Aggregate((string x, string y) => x + " || " + y)}");
			_writer.WriteLine("IL2CPP_DISABLE_OPTIMIZATIONS");
			_writer.WriteLine("#endif");
		}
	}

	public void Dispose()
	{
		if (_platformsWithOptimizationsDisabled != null)
		{
			ICodeWriter writer = _writer;
			writer.WriteLine($"#if {_platformsWithOptimizationsDisabled.Aggregate((string x, string y) => x + " || " + y)}");
			_writer.WriteLine("IL2CPP_ENABLE_OPTIMIZATIONS");
			_writer.WriteLine("#endif");
		}
	}
}
