using Beamable.Common;
using Beamable.Coroutines;
using Beamable.Service;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Core.Platform
{
	public abstract class BeamableTaskLike<TResult> : ITaskLike<TResult, BeamableTaskLike<TResult>>
	{
		public abstract TResult GetResult();

		public abstract bool IsCompleted
		{
			get;
		}

		public Guid Id
		{
			get;
		} = Guid.NewGuid();

		public BeamableTaskLike<TResult> GetAwaiter()
		{
			return this;
		}

		void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
		{
			var coroutineService = ServiceManager.Resolve<CoroutineService>();
			var waitForFrame = new WaitForEndOfFrame();

			IEnumerator Routine()
			{
				while (!IsCompleted)
				{
					yield return waitForFrame;
				}

				continuation();
			}

			coroutineService.StartNew($"task-like-{Id}", Routine());
		}

		void INotifyCompletion.OnCompleted(Action continuation)
		{
			((ICriticalNotifyCompletion)this).UnsafeOnCompleted(continuation);
		}
	}
}
