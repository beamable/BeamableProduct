using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Beamable.Editor
{
	/// <summary>
	/// The dispatcher allows you to enqueue work to happen on the main unity thread without waiting for editor render frames.
	/// Use the <see cref="Schedule(System.Action)"/> method to schedule work.
	/// </summary>
	public class BeamableDispatcher : IBeamableDisposable, ICoroutineService
	{
		public const string DEFAULT_QUEUE_NAME = "beamable";
		private Dictionary<string, Queue<Func<Promise>>> _workQueues;
		private Dictionary<string, EditorCoroutine> _runningSchedulers;
		private bool _forceStop;

		public bool IsForceStopped => _forceStop;

		public BeamableDispatcher()
		{
			_workQueues = new Dictionary<string, Queue<Func<Promise>>>();
			_runningSchedulers = new Dictionary<string, EditorCoroutine>();
			Start(DEFAULT_QUEUE_NAME);
		}

		IEnumerator Scheduler(string queueName, Queue<Func<Promise>> workQueue)
		{
			while (_workQueues.ContainsKey(queueName) && !_forceStop)
			{
				// Debug.Log("Scheduler waiting for work");
				yield return new WaitForWork(workQueue);
				// Debug.Log("Scheduler woke up");

				if (_forceStop) break;

				Queue<Func<Promise>> pendingWork;
				lock (workQueue)
				{
					pendingWork = new Queue<Func<Promise>>(workQueue);
					workQueue.Clear();
				}

				// Debug.Log("Scheduler has " + pendingWork.Count + " things to do");
				foreach (var workItem in pendingWork)
				{
					Promise promise = null;
					try
					{
						// Debug.Log("Doing work");
						promise = workItem?.Invoke();
						promise.Error(ex =>
						{
							Debug.LogError(ex);
						});
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed scheduled work. queue=[{queueName}]");
						Debug.LogException(ex);
					}
					
					yield return new PromiseYieldInstruction(promise);
				}
			}
			// Debug.Log("Scheduler exited");

			_runningSchedulers.Remove(queueName);
		}
		
		public class PromiseYieldInstruction : CustomYieldInstruction
		{
			private readonly Promise _promise;

			public PromiseYieldInstruction(Promise promise)
			{
				_promise = promise;
			}

			public override bool keepWaiting => !(_promise.IsCompleted || _promise.IsFailed);
		}

		/// <summary>
		/// Begin a new work queue.
		/// There is always a default work queue, but if you'd like to start more for load reasons, use this.
		/// You can stop a work queue by using the <see cref="StopAcceptingWork"/> method
		/// </summary>
		/// <param name="queueName">a unique name for your work queue</param>
		/// <returns>true if the work queue was spawned, or false if the queue name is already running.</returns>
		public bool Start(string queueName)
		{
			if (_workQueues.ContainsKey(queueName))
			{
				return false;
			}

			var queue = new Queue<Func<Promise>>();
			_workQueues.Add(queueName, queue);
			var coroutine = EditorCoroutineUtility.StartCoroutine(Scheduler(queueName, queue), this);
			_runningSchedulers[queueName] = coroutine;
			return true;
		}

		/// <summary>
		/// Stop a work queue.
		/// This will not cancel pending work on the queue, but will disallow new work to be scheduled. Existing work will execute, and then the work queue will stop.
		/// </summary>
		/// <param name="queueName">A queue name that was passed to <see cref="Start"/></param>
		/// <returns>true if the queue was stopped, or false if there was no queue by the given name</returns>
		public bool StopAcceptingWork(string queueName)
		{
			if (!_workQueues.ContainsKey(queueName))
			{
				return false;
			}

			_workQueues.Remove(queueName);
			return true;
		}

		/// <summary>
		/// Schedule a piece of work to happen on the main Unity thread.
		/// This method will automatically place the work on the default queue.
		/// </summary>
		/// <param name="work">The piece of work to execute later.</param>
		public void Schedule(Action work) => Schedule(DEFAULT_QUEUE_NAME, work);
		public void Schedule(Func<Promise> work) => Schedule(DEFAULT_QUEUE_NAME, work);

		/// <summary>
		/// Schedule a piece of work to happen on the main Unity thread.
		/// Work can be split across multiple queues.
		/// </summary>
		/// <param name="queueName">The name of the queue to run the work on. The <see cref="Start"/> method must be called with the given queue name first. </param>
		/// <param name="work">The piece of work to execute later.</param>
		/// <exception cref="Exception">If the <see cref="Start"/> method has not been called with the given queueName, an exception will be thrown.</exception>
		public void Schedule(string queueName, Action work)
		{
			Schedule(queueName, () =>
			{
				work();
				return Promise.Success;
			});
		}

		public void Schedule(string queueName, Func<Promise> work)
		{
			if (_forceStop)
			{
				var ex = new Exception("Cannot schedule work, because the scheduler has been stopped.");
				Debug.LogException(ex);
				throw ex;
			}
			if (_workQueues.TryGetValue(queueName, out var queue))
			{
				lock (queue)
				{
					queue.Enqueue(work);
				}
			}
			else
			{
				throw new Exception(
					$"Cannot schedule work on queue=[{queueName}] because no work queue has been started. Use the {nameof(Start)} method.");
			}
		}

		public Promise OnDispose()
		{
			_forceStop = true;
			_workQueues.Clear();
			foreach (var routine in _runningSchedulers)
			{
				EditorCoroutineUtility.StopCoroutine(routine.Value);
			}
			_runningSchedulers.Clear();
			return Promise.Success;
		}

		private class WaitForWork : CustomYieldInstruction
		{
			private readonly Queue<Func<Promise>> _workQueue;

			public WaitForWork(Queue<Func<Promise>> workQueue)
			{
				_workQueue = workQueue;
			}

			public override bool keepWaiting => _workQueue.Count == 0;
		}

		public void Run(string context, IEnumerator enumerator)
		{
			EditorCoroutineUtility.StartCoroutine(enumerator, this);
		}
	}
}
