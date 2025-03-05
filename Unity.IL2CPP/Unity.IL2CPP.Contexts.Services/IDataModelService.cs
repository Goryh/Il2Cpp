using System;
using Unity.IL2CPP.DataModel.Creation;

namespace Unity.IL2CPP.Contexts.Services;

public interface IDataModelService : ITypeFactory
{
	IDisposable BeginStats(string name);
}
