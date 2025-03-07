using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.IL2CPP.DataModel.BuildLogic.Utils;

public class SingleThreadedWorkQueue<TWorkItem> : IDisposable
{
	private readonly Action<TWorkItem> _worker;

	private readonly BlockingCollection<TWorkItem> _queue = new BlockingCollection<TWorkItem>();

	private readonly List<Exception> _exceptions = new List<Exception>();

	private readonly Task _workTask;

	public SingleThreadedWorkQueue(Action<TWorkItem> worker)
	{
		_worker = worker;
		_workTask = Task.Run((Action)WorkThreadRoutine);
	}

	public void QueueWork(TWorkItem item)
	{
		_queue.Add(item);
	}

	public void Complete()
	{
		_queue.CompleteAdding();
		_workTask.Wait();
		if (_exceptions.Count > 0)
		{
			throw new AggregateException(_exceptions);
		}
	}

	private void WorkThreadRoutine()
	{
		try
		{
			while (!_queue.IsCompleted)
			{
				TWorkItem workItem = _queue.Take();
				try
				{
					_worker(workItem);
				}
				catch (Exception item)
				{
					_exceptions.Add(item);
				}
			}
		}
		catch (InvalidOperationException) when (_queue.IsCompleted)
		{
		}
	}

	public void Dispose()
	{
		_queue.Dispose();
	}
}
