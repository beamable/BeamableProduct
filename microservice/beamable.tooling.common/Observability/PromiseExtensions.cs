// using System.Diagnostics;
// using Beamable.Common;
//
// namespace Beamable.Server;
//
// public static class PromiseExtensions
// {
// no good, because errors can be handled... 
//     public static Promise<T> WithActivity<T>(this Promise<T> promise, Func<BeamActivity> activityGenerator)
//     {
//         var activity = activityGenerator();
//         promise.Then(_ =>
//         {
//             activity.Stop(ActivityStatusCode.Ok);
//             activity.Dispose();
//         });
//         promise.Error(ex =>
//         {
//             activity.Stop(ex);
//             activity.Dispose();
//         });
//         
//         return promise;
//     }
// }