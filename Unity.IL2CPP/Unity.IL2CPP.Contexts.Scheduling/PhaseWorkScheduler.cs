using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling;

public class PhaseWorkScheduler<TContext> : IPhaseWorkScheduler<TContext>, IWorkScheduler, IDisposable
{
	private class WorkerThreadsIdleOutOfSyncException : Exception
	{
		public WorkerThreadsIdleOutOfSyncException(string message)
			: base(message)
		{
		}

		public WorkerThreadsIdleOutOfSyncException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	private abstract class WorkItem
	{
		public abstract void Invoke(object context, int uniqueId);
	}

	private abstract class WorkItemWithTag<TTag> : WorkItem
	{
		protected readonly TTag _tag;

		protected WorkItemWithTag(TTag tag)
		{
			_tag = tag;
		}
	}

	private sealed class ContextOnlyWorkItem<TWorkerContext> : WorkItemWithTag<object>
	{
		private readonly Action<TWorkerContext> _workerAction;

		public ContextOnlyWorkItem(Action<TWorkerContext> action)
			: base((object)null)
		{
			_workerAction = action;
		}

		public override void Invoke(object context, int uniqueId)
		{
			_workerAction((TWorkerContext)context);
		}
	}

	private sealed class ActionWorkItem<TWorkerContext, TTag> : WorkItemWithTag<TTag>
	{
		private readonly Action<WorkItemData<TWorkerContext, TTag>> _workerAction;

		public ActionWorkItem(Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
			: base(tag)
		{
			_workerAction = action;
		}

		public override void Invoke(object context, int uniqueId)
		{
			WorkItemData<TWorkerContext, TTag> data = new WorkItemData<TWorkerContext, TTag>((TWorkerContext)context, uniqueId, _tag);
			_workerAction(data);
		}
	}

	private sealed class ActionWorkItem<TWorkerContext> : WorkItem
	{
		private readonly Action<WorkItemData<TWorkerContext>> _workerAction;

		public ActionWorkItem(Action<WorkItemData<TWorkerContext>> action)
		{
			_workerAction = action;
		}

		public override void Invoke(object context, int uniqueId)
		{
			WorkItemData<TWorkerContext> data = new WorkItemData<TWorkerContext>((TWorkerContext)context, uniqueId);
			_workerAction(data);
		}
	}

	private sealed class ActionWorkItemWithItem<TWorkerContext, TItem, TTag> : WorkItemWithTag<TTag>
	{
		private readonly WorkerAction<TWorkerContext, TItem, TTag> _workerAction;

		private readonly TItem _item;

		public ActionWorkItemWithItem(WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag, TItem item)
			: base(tag)
		{
			_item = item;
			_workerAction = action;
		}

		public override void Invoke(object context, int uniqueId)
		{
			WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
			_workerAction(data);
		}
	}

	private abstract class BaseContinueWorkItem<TItem, TTag> : WorkItemWithTag<TTag>
	{
		public abstract class CollectionSharedData
		{
			private readonly int _expectedTotal;

			private int _completedCount;

			protected CollectionSharedData(int expectedTotal)
			{
				_completedCount = 0;
				_expectedTotal = expectedTotal;
			}

			public void AttemptPostProcess(object context, int uniqueId, TTag tag)
			{
				if (Interlocked.Increment(ref _completedCount) == _expectedTotal)
				{
					PostProcess(context, uniqueId, tag);
				}
			}

			protected abstract void PostProcess(object context, int uniqueId, TTag tag);
		}

		protected readonly TItem _item;

		private readonly CollectionSharedData _shared;

		protected BaseContinueWorkItem(TTag tag, TItem item, CollectionSharedData shared)
			: base(tag)
		{
			_item = item;
			_shared = shared;
		}

		public override void Invoke(object context, int uniqueId)
		{
			InvokeWorker(context, uniqueId);
			_shared.AttemptPostProcess(context, uniqueId, _tag);
		}

		protected abstract void InvokeWorker(object context, int uniqueId);
	}

	private class ContinueWithWorkItem<TWorkerContext, TItem, TTag> : BaseContinueWorkItem<TItem, TTag>
	{
		public class SharedData : CollectionSharedData
		{
			private readonly ContinueAction<TWorkerContext, TTag> _continueAction;

			public SharedData(int expectedTotal, ContinueAction<TWorkerContext, TTag> continueAction)
				: base(expectedTotal)
			{
				_continueAction = continueAction;
			}

			protected override void PostProcess(object context, int uniqueId, TTag tag)
			{
				WorkItemData<TWorkerContext, TTag> data = new WorkItemData<TWorkerContext, TTag>((TWorkerContext)context, uniqueId, tag);
				_continueAction(data);
			}
		}

		private readonly WorkerAction<TWorkerContext, TItem, TTag> _workerAction;

		public ContinueWithWorkItem(WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag, TItem item, SharedData shared)
			: base(tag, item, (CollectionSharedData)shared)
		{
			_workerAction = action;
		}

		protected override void InvokeWorker(object context, int uniqueId)
		{
			WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
			_workerAction(data);
		}
	}

	private class ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag> : BaseContinueWorkItem<TItem, TTag>
	{
		public class SharedData : CollectionSharedData
		{
			public readonly ResultData<TItem, TResult>[] Results;

			private readonly ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> _continueWithResults;

			public SharedData(int expectedTotal, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults)
				: base(expectedTotal)
			{
				_continueWithResults = continueWithResults;
				Results = new ResultData<TItem, TResult>[expectedTotal];
			}

			protected override void PostProcess(object context, int uniqueId, TTag tag)
			{
				WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag> data = new WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag>((TWorkerContext)context, Results.AsReadOnly(), uniqueId, tag);
				_continueWithResults(data);
			}
		}

		private readonly SharedData _shared;

		private readonly WorkerFunc<TWorkerContext, TItem, TResult, TTag> _workerFunc;

		private readonly int _index;

		public ContinueWithResultsWorkItem(WorkerFunc<TWorkerContext, TItem, TResult, TTag> workerFunc, TTag tag, TItem item, int index, SharedData shared)
			: base(tag, item, (CollectionSharedData)shared)
		{
			_workerFunc = workerFunc;
			_shared = shared;
			_index = index;
		}

		protected override void InvokeWorker(object context, int uniqueId)
		{
			WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
			TResult result = _workerFunc(data);
			_shared.Results[_index] = new ResultData<TItem, TResult>(_item, result);
		}
	}

	private readonly ReadOnlyCollection<ForkedContextScope<int, TContext>.Data> _contextData;

	private readonly ForkedContextScope<int, TContext> _forkedContextScope;

	private readonly List<Thread> _workerThreads = new List<Thread>();

	private readonly Exception[] _workerThreadDeadExceptions;

	private readonly ConcurrentBag<Exception> _workItemActionExceptions = new ConcurrentBag<Exception>();

	private readonly ConcurrentQueue<WorkItem> _workQueue = new ConcurrentQueue<WorkItem>();

	private readonly int _workerCount;

	private readonly Func<TContext, Exception, Exception> _workItemExceptionHandler;

	private readonly bool _workersImmediatelyForceCreationOfForkedContext;

	private readonly bool _allowContextForMainThread;

	private readonly GlobalSchedulingContext _schedulingContext;

	private readonly SemaphoreSlim _workAvailable = new SemaphoreSlim(0);

	private readonly ManualResetEvent _stop = new ManualResetEvent(initialState: false);

	private int _idleCount;

	private readonly ManualResetEvent _allWorkersIdle = new ManualResetEvent(initialState: false);

	private bool _disposed;

	private bool _waitComplete;

	public TContext ContextForMainThread
	{
		get
		{
			if (!_allowContextForMainThread)
			{
				throw new NotSupportedException("It is costly to create a context for the main thread.  Let's try and avoid needing this if possible.  Use SchedulingContext or QueuingContext instead");
			}
			return _contextData[_contextData.Count - 1].Context;
		}
	}

	public GlobalSchedulingContext SchedulingContext => _schedulingContext;

	public TContext QueuingContext => default(TContext);

	public bool WorkIsDoneOnDifferentThread => true;

	public PhaseWorkScheduler(GlobalSchedulingContext context, Func<int, ForkedContextScope<int, TContext>> forker, int workerCount, Func<TContext, Exception, Exception> workerItemExceptionHandler, bool allowContextForMainThread, bool workersImmediatelyForceCreationOfForkedContext = true)
	{
		if (workerItemExceptionHandler == null)
		{
			throw new ArgumentNullException("workerItemExceptionHandler");
		}
		_schedulingContext = context;
		_forkedContextScope = forker(workerCount + 1);
		_workerCount = workerCount;
		_contextData = _forkedContextScope.Items;
		_workerThreadDeadExceptions = new Exception[workerCount];
		_workItemExceptionHandler = workerItemExceptionHandler;
		_workersImmediatelyForceCreationOfForkedContext = workersImmediatelyForceCreationOfForkedContext;
		_allowContextForMainThread = allowContextForMainThread;
		Start();
	}

	public void Enqueue<TWorkerContext>(TWorkerContext context, Action<WorkItemData<TWorkerContext>> action)
	{
		EnqueueInternal(new ActionWorkItem<TWorkerContext>(action));
	}

	public void Enqueue<TWorkerContext, TTag>(TWorkerContext context, Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
	{
		EnqueueInternal(new ActionWorkItem<TWorkerContext, TTag>(action, tag));
	}

	public void Enqueue<TWorkerContext>(TWorkerContext context, Action<TWorkerContext> action)
	{
		EnqueueInternal(new ContextOnlyWorkItem<TWorkerContext>(action));
	}

	public void Enqueue<TWorkerContext, TItem, TTag>(TWorkerContext context, TItem item, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
	{
		EnqueueInternal(new ActionWorkItemWithItem<TWorkerContext, TItem, TTag>(action, tag, item));
	}

	public void EnqueueItems<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
	{
		if (items.Count == 0)
		{
			return;
		}
		foreach (TItem item in items)
		{
			_workQueue.Enqueue(new ActionWorkItemWithItem<TWorkerContext, TItem, TTag>(action, tag, item));
		}
		_workAvailable.Release(Math.Min(items.Count, _workerCount));
	}

	public void EnqueueItemsAndContinueWithResults<TWorkerContext, TItem, TResult, TTag>(TWorkerContext context, ReadOnlyCollection<TItem> items, WorkerFunc<TWorkerContext, TItem, TResult, TTag> func, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults, TTag tag)
	{
		if (items.Count != 0)
		{
			ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>.SharedData sharedData = new ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>.SharedData(items.Count, continueWithResults);
			for (int index = 0; index < items.Count; index++)
			{
				TItem item = items[index];
				_workQueue.Enqueue(new ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>(func, tag, item, index, sharedData));
			}
			_workAvailable.Release(Math.Min(items.Count, _workerCount));
		}
	}

	public void EnqueueItemsAndContinueWith<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, ContinueAction<TWorkerContext, TTag> continueAction, TTag tag)
	{
		if (items.Count == 0)
		{
			return;
		}
		ContinueWithWorkItem<TWorkerContext, TItem, TTag>.SharedData sharedData = new ContinueWithWorkItem<TWorkerContext, TItem, TTag>.SharedData(items.Count, continueAction);
		foreach (TItem item in items)
		{
			_workQueue.Enqueue(new ContinueWithWorkItem<TWorkerContext, TItem, TTag>(action, tag, item, sharedData));
		}
		_workAvailable.Release(Math.Min(items.Count, _workerCount));
	}

	private void EnqueueInternal(WorkItem workItem)
	{
		_workQueue.Enqueue(workItem);
		_workAvailable.Release();
	}

	public void Wait()
	{
		if (_waitComplete)
		{
			return;
		}
		try
		{
			WaitForEmptyQueue();
			JoinThreads();
		}
		finally
		{
			_waitComplete = true;
		}
	}

	public void WaitForEmptyQueue()
	{
		try
		{
			using (_schedulingContext.Services.TinyProfiler.Section("PhaseWorkScheduler.WaitForEmptyQueue"))
			{
				WaitHandle[] waitHandles = new WaitHandle[2] { _stop, _allWorkersIdle };
				while (WaitHandle.WaitAny(waitHandles) != 0 && !_workQueue.IsEmpty)
				{
				}
			}
		}
		catch (Exception)
		{
			try
			{
				_stop.Set();
			}
			catch (Exception)
			{
			}
			throw;
		}
	}

	private void JoinThreads()
	{
		using (_schedulingContext.Services.TinyProfiler.Section("PhaseWorkScheduler.JoinThreads"))
		{
			_stop.Set();
			foreach (Thread workerThread in _workerThreads)
			{
				workerThread.Join();
			}
			ThrowExceptions();
		}
	}

	public void ThrowExceptions()
	{
		Exception[] possibleExceptions = _workerThreadDeadExceptions.Where((Exception e) => e != null).ToArray();
		if (possibleExceptions.Length != 0)
		{
			throw new AggregateException("One or more worker threads hit a fatal exception", possibleExceptions);
		}
		if (_workItemActionExceptions.Count > 0)
		{
			throw new AggregateErrorInformationAlreadyProcessedException("One or more worker items throw an exception", _workItemActionExceptions);
		}
	}

	private void Start()
	{
		for (int i = 0; i < _workerCount; i++)
		{
			Thread thread = new Thread(WorkerLoop);
			thread.Name = "PhaseWorker";
			_workerThreads.Add(thread);
			thread.Start(_contextData[i]);
		}
	}

	private void WorkerLoop(object data)
	{
		ForkedContextScope<int, TContext>.Data workerData = (ForkedContextScope<int, TContext>.Data)data;
		try
		{
			WaitHandle[] waitHandles = new WaitHandle[2] { _stop, _workAvailable.AvailableWaitHandle };
			if (_workersImmediatelyForceCreationOfForkedContext)
			{
				workerData.InitializeContext();
			}
			while (true)
			{
				int index = 1;
				if (_workQueue.IsEmpty)
				{
					int value = Interlocked.Increment(ref _idleCount);
					try
					{
						if (value > _workerCount)
						{
							throw new WorkerThreadsIdleOutOfSyncException($"While worker was going idle, idle count became out of sync.{"_idleCount"}={value} which is greater than the {"_workerCount"} of {_workerCount}");
						}
						if (value == _workerCount)
						{
							_allWorkersIdle.Set();
						}
						using (_schedulingContext.Services.TinyProfiler.Section("Idle Conversion"))
						{
							index = WaitHandle.WaitAny(waitHandles);
						}
						_allWorkersIdle.Reset();
					}
					finally
					{
						Interlocked.Decrement(ref _idleCount);
					}
				}
				switch (index)
				{
				case 0:
					return;
				case 1:
				{
					_workAvailable.Wait(0);
					WorkItem workItem;
					while (_workQueue.TryDequeue(out workItem))
					{
						try
						{
							workItem.Invoke(workerData.Context, workerData.Index);
						}
						catch (Exception ex)
						{
							_stop.Set();
							try
							{
								_workItemActionExceptions.Add(_workItemExceptionHandler(workerData.Context, ex));
								return;
							}
							catch (Exception ex2)
							{
								_workItemActionExceptions.Add(new AggregateException(ex, ex2));
								return;
							}
						}
					}
					break;
				}
				default:
					throw new ArgumentException($"Unhandled wait handle index of {index}");
				}
			}
		}
		catch (Exception ex3)
		{
			_workerThreadDeadExceptions[workerData.Index] = ex3;
			_stop.Set();
		}
		finally
		{
			int value2 = Interlocked.Increment(ref _idleCount);
			try
			{
				if (value2 > _workerCount)
				{
					if (_workerThreadDeadExceptions[workerData.Index] == null)
					{
						_workerThreadDeadExceptions[workerData.Index] = new WorkerThreadsIdleOutOfSyncException($"While worker was exiting gracefully, idle count became out of sync. {"_idleCount"}={value2} which is greater than the {"_workerCount"} of {_workerCount}");
					}
					else
					{
						_workerThreadDeadExceptions[workerData.Index] = new WorkerThreadsIdleOutOfSyncException($"While worker was exiting from a crash, idle count became out of sync. {"_idleCount"}={value2} which is greater than the {"_workerCount"} of {_workerCount}", _workerThreadDeadExceptions[workerData.Index]);
					}
					_stop.Set();
				}
			}
			finally
			{
				if (value2 == _workerCount)
				{
					_allWorkersIdle.Set();
				}
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			Wait();
			_forkedContextScope.Dispose();
		}
		_stop.Dispose();
		_workAvailable.Dispose();
		_allWorkersIdle.Dispose();
		_disposed = true;
	}
}
