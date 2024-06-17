using Beamable.Common;
using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class HttpClientExtensions
	{
		public static Promise<string> UnityGetStringAsync(this HttpClient client, string url)
		{
			return client.GetStringAsync(url).TaskToPromise();
			// var task = client.GetStringAsync(url);
			// var promise = new Promise<string>();
			// EditorCoroutineUtility.StartCoroutineOwnerless(TaskToPromiseRoutine(task, promise));
			// return promise;
		}

		public static Promise<HttpResponseMessage> UnitySendAsync(this HttpClient client,
		                                                          HttpRequestMessage req,
		                                                          HttpCompletionOption options)
		{
			return client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).TaskToPromise();
			// var promise = new Promise<HttpResponseMessage>();
			// var task = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
			// EditorCoroutineUtility.StartCoroutineOwnerless(TaskToPromiseRoutine(task, promise));
			// return promise;
		}

		public static Promise<T> TaskToPromise<T>(this Task<T> task)
		{
			var promise = new Promise<T>();
			EditorCoroutineUtility.StartCoroutineOwnerless(TaskToPromiseRoutine(task, promise));
			return promise;
		}
		
		// using HttpResponseMessage response =
		// 	await _localClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);


		public static IEnumerator TaskToPromiseRoutine<T>(Task<T> task, Promise<T> promise)
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
