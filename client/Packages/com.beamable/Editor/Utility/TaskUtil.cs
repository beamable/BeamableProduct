using Beamable.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class TaskUtil
	{
		
		/// <summary>
		/// Start an editor coroutine to watch the task, and returns a promise that
		/// will be completed from the coroutine when the task is finished. 
		/// </summary>
		/// <param name="task"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Promise<T> ToPromiseRoutine<T>(this Task<T> task)
		{
			var promise = new Promise<T>();
			EditorCoroutineUtility.StartCoroutineOwnerless(TaskToPromiseRoutine(task, promise));
			return promise;
		}

		static IEnumerator TaskToPromiseRoutine<T>(Task<T> task, Promise<T> promise)
		{
			yield return task.ToYielder();
			if (task.IsFaulted)
			{
				promise.CompleteError(task.Exception);
			}
			else
			{
				promise.CompleteSuccess(task.Result);
			}
		}

		public static WaitForTask ToYielder(this Task task)
		{
			return new WaitForTask(task);
		}

		public class WaitForTask : CustomYieldInstruction
		{
			private readonly Task _task;
			public override bool keepWaiting => !_task.IsCompleted;

			public WaitForTask(Task task)
			{
				_task = task;
			}
		}
	}
}


