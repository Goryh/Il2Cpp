using System;

namespace Unity.IL2CPP.DataModel.Stats;

public static class StatisticsSection
{
	private class ActiveSection : IDisposable
	{
		private readonly TypeContext _context;

		public ActiveSection(TypeContext context)
		{
			_context = context;
		}

		public void Dispose()
		{
		}
	}

	private class DisabledSection : IDisposable
	{
		public void Dispose()
		{
		}
	}

	public static IDisposable Begin(TypeContext context, string name)
	{
		return new DisabledSection();
	}
}
