// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

#if !UNITY_WEBGL || UNITY_EDITOR
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
