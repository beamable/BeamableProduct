#if !UNITY_WEBGL
using System;
using System.Threading.Tasks;

namespace Beamable.Common
{
    public class BeamableTaskExtensions
    {
        public static Task TaskFromGenericPromise<T>(Promise<T> promise)
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
        
        public static Task TaskFromPromise(Promise promise)
        {
	        var tcs = new System.Threading.Tasks.TaskCompletionSource<Unit>();
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
