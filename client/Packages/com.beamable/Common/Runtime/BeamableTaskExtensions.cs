// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

﻿#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Threading.Tasks;

namespace Beamable.Common
{
	public static class BeamableTaskExtensions
	{
		public static Task TaskFromPromise<T>(this Promise<T> promise)
		{
			var tcs = new System.Threading.Tasks.TaskCompletionSource<T>();
			promise.Then(obj =>
			{
				tcs.SetResult(obj);

			}).Error((Exception e) =>
			{
				tcs.SetException(e);
			});

			return tcs.Task;
		}
	}
}
#endif
