using System;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.BuildLogic;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.Contexts.Components;

public class DataModelComponent : IDisposable
{
	private DataModelBuilder _builder;

	private TypeContext _typeContext;

	private bool _ownsTypeContext;

	private bool _ownsBuilder;

	public TypeContext TypeContext
	{
		get
		{
			if (_typeContext == null)
			{
				throw new InvalidOperationException("The TypeContext is not available yet.");
			}
			return _typeContext;
		}
	}

	public TypeContext Load(TinyProfiler2 tinyProfiler, LoadSettings loadSettings)
	{
		DataModelBuilder builder;
		return Load(tinyProfiler, loadSettings, ownsTypeContext: true, ownsBuilder: true, out builder);
	}

	public TypeContext Load(TinyProfiler2 tinyProfiler, LoadSettings loadSettings, bool ownsTypeContext, bool ownsBuilder, out DataModelBuilder builder)
	{
		_builder = (builder = Loader.Load(tinyProfiler, loadSettings));
		_typeContext = _builder.Context;
		_ownsTypeContext = ownsTypeContext;
		_ownsBuilder = ownsBuilder;
		return _typeContext;
	}

	public void Load(TypeContext typeContext)
	{
		_typeContext = typeContext;
		_ownsTypeContext = false;
		_ownsBuilder = false;
	}

	public void Dispose()
	{
		if (_ownsBuilder)
		{
			_builder?.Dispose();
		}
		if (_ownsTypeContext)
		{
			_typeContext.Dispose();
		}
	}
}
