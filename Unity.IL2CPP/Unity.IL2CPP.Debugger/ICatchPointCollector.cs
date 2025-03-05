using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.DataModel.Awesome.CFG;

namespace Unity.IL2CPP.Debugger;

public interface ICatchPointCollector
{
	void AddCatchPoint(PrimaryCollectionContext context, MethodDefinition method, Node catchNode);
}
