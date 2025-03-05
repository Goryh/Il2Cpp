using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Debugger;

public interface ICatchPointCollectorProvider
{
	ICatchPointCollector GetCollector(AssemblyDefinition assembly);
}
