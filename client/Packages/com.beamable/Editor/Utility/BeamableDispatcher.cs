using Beamable.Common;
using Beamable.Common.Dependencies;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Beamable.Editor
{
	public class BeamableDispatcher : IBeamableDisposable
	{
		public const string DEFAULT_QUEUE_NAME = "beamable";
		private Dictionary<string, Queue<Action>> _workQueues;
		private Dictionary<string, EditorCoroutine> _runningSchedulers;
		private bool _forceStop;

		public BeamableDispatcher()
		{
			_workQueues = new Dictionary<string, Queue<Action>>();
			_runningSchedulers = new Dictionary<string, EditorCoroutine>();
			Start(DEFAULT_QUEUE_NAME);
		}

		IEnumerator Scheduler(string queueName, Queue<Action> workQueue)
		{
			while (_workQueues.ContainsKey(queueName) && !_forceStop)
			{
				// Debug.Log("Scheduler waiting for work");
				yield return new WaitForWork(workQueue);
				// Debug.Log("Scheduler woke up");

				if (_forceStop) break;

				Queue<Action> pendingWork;
				lock (workQueue)
				{
					pendingWork = new Queue<Action>(workQueue);
					workQueue.Clear();
				}

				// Debug.Log("Scheduler has " + pendingWork.Count + " things to do");
				foreach (var workItem in pendingWork)
				{
					try
					{
						// Debug.Log("Doing work");
						workItem?.Invoke();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed scheduled work. queue=[{queueName}]");
						Debug.LogException(ex);
					}
				}
			}
			// Debug.Log("Scheduler exited");

			_runningSchedulers.Remove(queueName);
		}

		public bool Start(string queueName)
		{
			if (_workQueues.ContainsKey(queueName))
			{
				return false;
			}

			var queue = new Queue<Action>();
			_workQueues.Add(queueName, queue);
			var coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(Scheduler(queueName, queue));
			_runningSchedulers[queueName] = coroutine;
			return true;
		}

		public bool StopAcceptingWork(string queueName)
		{
			if (!_workQueues.ContainsKey(queueName))
			{
				return false;
			}

			_workQueues.Remove(queueName);
			return true;
		}

		public void Schedule(Action work) => Schedule(DEFAULT_QUEUE_NAME, work);

		/// <summary>
		/// Schedule a piece of work to happen on the main Unity thread.
		/// Work can be split across multiple queues.
		/// </summary>
		/// <param name="queueName">The name of the queue to run the work on. The <see cref="Start"/> method must be called with the given queue name first. </param>
		/// <param name="work">The piece of work to execute later.</param>
		/// <exception cref="Exception">If the <see cref="Start"/> method has not been called with the given queueName, an exception will be thrown.</exception>
		public void Schedule(string queueName, Action work)
		{
			if (_forceStop) throw new Exception("Cannot schedule work, because the scheduler has been stopped.");
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

		public class WaitForWork : CustomYieldInstruction
		{
			private readonly Queue<Action> _workQueue;

			public WaitForWork(Queue<Action> workQueue)
			{
				_workQueue = workQueue;
			}

			public override bool keepWaiting => _workQueue.Count == 0;
		}

	}
}
