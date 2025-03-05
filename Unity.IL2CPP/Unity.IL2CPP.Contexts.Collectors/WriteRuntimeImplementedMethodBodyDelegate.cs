using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.Contexts.Collectors;

public delegate void WriteRuntimeImplementedMethodBodyDelegate(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess);
