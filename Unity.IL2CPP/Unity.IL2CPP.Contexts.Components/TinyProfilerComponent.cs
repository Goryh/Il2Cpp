using Bee.Core;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.TinyProfiler;

namespace Unity.IL2CPP.Contexts.Components;

public class TinyProfilerComponent : ReusedServiceComponentBase<ITinyProfilerService, TinyProfilerComponent>, ITinyProfilerService
{
	private readonly Unity.TinyProfiler.TinyProfiler2 _tinyProfiler;

	public Unity.TinyProfiler.TinyProfiler2 TinyProfiler => _tinyProfiler;

	public TinyProfilerComponent(Unity.TinyProfiler.TinyProfiler2 tinyProfiler)
	{
		_tinyProfiler = tinyProfiler;
	}

	protected override TinyProfilerComponent ThisAsFull()
	{
		return this;
	}

	protected override ITinyProfilerService ThisAsRead()
	{
		return this;
	}

	public SectionDisposable Section(string name)
	{
		return _tinyProfiler.Section(name);
	}

	public SectionDisposable Section(string name, string details)
	{
		return _tinyProfiler.Section(name, details);
	}
}
