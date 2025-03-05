using System.Collections.Generic;
using System.Text;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.DataModel;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Contexts.Services;

public interface IObjectFactory
{
	IChunkedMemoryStreamBufferProvider ChunkedMemoryStreamProvider { get; }

	IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage, WritingMethodFor writingMethodFor);

	DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields);

	Returnable<StringBuilder> CheckoutStringBuilder();
}
