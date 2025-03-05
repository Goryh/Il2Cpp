using System.Collections.ObjectModel;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.SourceWriters;

public readonly struct TypeWritingInformation
{
	public readonly TypeReference DeclaringType;

	public readonly ReadOnlyCollection<MethodReference> MethodsToWrite;

	public readonly bool ChunkToOwnFile;

	public readonly bool WriteTypeLevelInformation;

	public string ProfilerSectionName
	{
		get
		{
			if (MethodsToWrite.Count != 1)
			{
				return DeclaringType.Name;
			}
			return MethodsToWrite[0].Name;
		}
	}

	public TypeWritingInformation(TypeReference declaringType, ReadOnlyCollection<MethodReference> methodsToWrite, bool chunkToOwnFile, bool writeTypeLevelInformation)
	{
		DeclaringType = declaringType;
		MethodsToWrite = methodsToWrite;
		ChunkToOwnFile = chunkToOwnFile;
		WriteTypeLevelInformation = writeTypeLevelInformation;
	}
}
