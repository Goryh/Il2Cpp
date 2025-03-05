using Bee.Core;

namespace Unity.IL2CPP.Contexts.Services;

public interface ITinyProfilerService
{
	SectionDisposable Section(string name);

	SectionDisposable Section(string name, string details);
}
