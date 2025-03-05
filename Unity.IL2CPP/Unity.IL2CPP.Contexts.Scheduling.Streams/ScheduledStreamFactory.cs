using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams;

public static class ScheduledStreamFactory
{
	private static bool FavorItemLevelOverFile => false;

	public static BaseStreamManager<TItem, TWritingItem, TStream> Create<TItem, TWritingItem, TStream>(SourceWritingContext context, NPath fileName, IStreamWriterCallbacks<TItem, TWritingItem, TStream> callbacks) where TStream : IStream
	{
		NPath outputDirectory = context.Global.InputData.OutputDir;
		if (context.Global.Parameters.EnableSerialConversion)
		{
			return new SerialStreamManager<TItem, TWritingItem, TStream>(outputDirectory, fileName, callbacks);
		}
		if (FavorItemLevelOverFile)
		{
			return new ItemLevelParallelStreamManager<TItem, TWritingItem, TStream>(outputDirectory, fileName, callbacks);
		}
		return new FileLevelParallelStreamManager<TItem, TWritingItem, TStream>(outputDirectory, fileName, callbacks);
	}
}
