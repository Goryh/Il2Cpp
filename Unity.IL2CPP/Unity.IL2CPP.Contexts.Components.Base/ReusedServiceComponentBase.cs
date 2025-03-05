using System;

namespace Unity.IL2CPP.Contexts.Components.Base;

public abstract class ReusedServiceComponentBase<TRead, TFull> : ServiceComponentBase<TRead, TFull> where TFull : ServiceComponentBase<TRead, TFull>, TRead
{
	protected override void ResetPooledInstanceStateIfNecessary()
	{
		throw new NotSupportedException("Reused services should not use Pooled.  Inherit from ServiceComponentBase instead to use Pooled");
	}

	protected override void SyncPooledInstanceWithParent(TFull parent)
	{
		throw new NotSupportedException("Reused services should not use Pooled.  Inherit from ServiceComponentBase instead to use Pooled");
	}

	protected override TFull CreatePooledInstance()
	{
		throw new NotSupportedException("Reused services should not use Pooled.  Inherit from ServiceComponentBase instead to use Pooled");
	}
}
