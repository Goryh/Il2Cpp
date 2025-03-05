using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.SourceWriters.Utils;

public static class Chunker
{
	public const int DefaultMethodBodyChunkSize = 26000;

	public const int DelegateInvokeCppSizeEstimate = 80;

	public const int MinimumSizeEstimateWhenNoBody = 5;

	public static ReadOnlyCollection<ReadOnlyCollection<TypeWritingInformation>> ChunkByApproximateGeneratedCodeSize(this ICollection<TypeWritingInformation> collection, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, (TypeWritingInformation i) => i.GetApproximateGeneratedCodeSize(), (TypeWritingInformation i) => i.ChunkToOwnFile);
	}

	public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceType>> ChunkByApproximateGeneratedCodeSize(this ICollection<GenericInstanceType> collection, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, (GenericInstanceType m) => m.GetApproximateGeneratedCodeSize(), (GenericInstanceType t) => false);
	}

	public static ReadOnlyCollection<ReadOnlyCollection<TypeDefinition>> ChunkByApproximateGeneratedCodeSize(this ICollection<TypeDefinition> collection, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, (TypeDefinition m) => m.GetApproximateGeneratedCodeSize(), (TypeDefinition t) => false);
	}

	public static ReadOnlyCollection<ReadOnlyCollection<TypeReference>> ChunkByApproximateGeneratedCodeSize(this ICollection<TypeReference> collection, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, (TypeReference m) => m.GetApproximateGeneratedCodeSize(), (TypeReference t) => false);
	}

	public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceMethod>> ChunkByApproximateGeneratedCodeSize(this ICollection<GenericInstanceMethod> collection, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, (GenericInstanceMethod m) => m.GetApproximateGeneratedCodeSize(), WritingUtils.ShouldGenerateIntoOwnFile);
	}

	public static ReadOnlyCollection<ReadOnlyCollection<T>> ChunkByApproximateGeneratedCodeSize<T>(this ICollection<T> collection, Func<T, int> getSize, int sizeOfChunks = 26000)
	{
		return collection.ChunkBySize(sizeOfChunks, getSize, (T i) => false);
	}

	public static int GetApproximateGeneratedCodeSize(this in TypeWritingInformation type)
	{
		return type.MethodsToWrite.Sum((MethodReference m) => m.GetApproximateGeneratedCodeSize());
	}

	public static int GetApproximateGeneratedCodeSize(this TypeReference type)
	{
		return type.Resolve().GetApproximateGeneratedCodeSize();
	}

	private static int GetApproximateGeneratedCodeSize(this TypeDefinition type)
	{
		return type.Methods.Sum((MethodDefinition m) => m.GetApproximateGeneratedCodeSize());
	}

	private static int GetApproximateGeneratedCodeSize(this GenericInstanceMethod method)
	{
		if (method.HasBody)
		{
			return method.CodeSize;
		}
		MethodDefinition resolved = method.Resolve();
		if (resolved == null)
		{
			return 0;
		}
		return GetApproximateGeneratedCodeSizeOfBodyLessMethod(resolved);
	}

	private static int GetApproximateGeneratedCodeSize(this MethodReference method)
	{
		if (method.HasBody)
		{
			return method.CodeSize;
		}
		MethodDefinition resolved = method.Resolve();
		if (resolved == null)
		{
			return 0;
		}
		return GetApproximateGeneratedCodeSizeOfBodyLessMethod(resolved);
	}

	public static int GetApproximateGeneratedCodeSize(this MethodDefinition method)
	{
		if (method.HasBody)
		{
			return method.CodeSize;
		}
		return GetApproximateGeneratedCodeSizeOfBodyLessMethod(method);
	}

	private static int GetApproximateGeneratedCodeSizeOfBodyLessMethod(MethodDefinition method)
	{
		if (method.DeclaringType.IsDelegate && method.Name == "Invoke")
		{
			return 80;
		}
		if (method.IsAbstract)
		{
			return 0;
		}
		return 5;
	}

	public static ReadOnlyCollection<ReadOnlyCollection<T>> ChunkBySize<T>(this IEnumerable<T> collection, int sizeOfChunks, Func<T, int> getSize, Func<T, bool> ownFile)
	{
		List<ReadOnlyCollection<T>> results = new List<ReadOnlyCollection<T>>();
		List<T> currentList = new List<T>();
		int counter = 0;
		foreach (T item in collection)
		{
			if (ownFile(item))
			{
				results.Add(new T[1] { item }.AsReadOnly());
				continue;
			}
			counter += getSize(item);
			currentList.Add(item);
			if (counter > sizeOfChunks)
			{
				results.Add(currentList.ToList().AsReadOnly());
				currentList.Clear();
				counter = 0;
			}
		}
		results.Add(currentList.ToList().AsReadOnly());
		return results.AsReadOnly();
	}
}
