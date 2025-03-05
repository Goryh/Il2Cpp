using System;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.DataModel;

namespace Unity.IL2CPP.CodeWriters;

public class PooledGeneratedMethodCodeStringBuilder : GeneratedMethodCodeStringBuilder, IDisposable
{
	private readonly Returnable<StringBuilder> _builderContext;

	public PooledGeneratedMethodCodeStringBuilder(SourceWritingContext context)
		: this(context, context.Global.Services.Factory.CheckoutStringBuilder())
	{
	}

	private PooledGeneratedMethodCodeStringBuilder(SourceWritingContext context, Returnable<StringBuilder> builderContext)
		: base(context, builderContext.Value)
	{
		_builderContext = builderContext;
	}

	public void Dispose()
	{
		_builderContext.Dispose();
	}
}
