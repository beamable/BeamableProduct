using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Coroutines;
using Beamable.Platform.SDK;
using Beamable.Service;
using UnityEngine;

namespace Beamable
{
   public static class PromiseExtensions
   {

      private static HashSet<Task> _uncaughtTasks = new HashSet<Task>();
      public static async Task WaitForAllUncaughtHandlers()
      {
         var tasks = _uncaughtTasks.ToArray();
         await Task.WhenAll(tasks);
      }

      public static void RegisterUncaughtPromiseHandler()
      {
         PromiseBase.SetPotentialUncaughtErrorHandler(PromiseBaseOnPotentialOnPotentialUncaughtError);
      }

      private static void PromiseBaseOnPotentialOnPotentialUncaughtError(PromiseBase promise, Exception ex)
      {
         // we need to wait one frame before logging anything.
         async Task DelayedCheck()
         {
            await Task.Yield();
            // execute check.
            if (!promise.HadAnyErrbacks)
            {
               Beamable.Common.BeamableLogger.LogException(new UncaughtPromiseException(promise, ex));
            }
         }
         var t = DelayedCheck(); // we don't want to await this call.
         _uncaughtTasks.Add(t);
         t.ContinueWith(_ => _uncaughtTasks.Remove(t));
      }

      public static Promise<T> WaitForSeconds<T>(this Promise<T> promise, float seconds)
      {
         var result = new Promise<T>();
         IEnumerator Wait()
         {
            yield return Yielders.Seconds(seconds);
            promise.Then(x => result.CompleteSuccess(x));
         };

         ServiceManager.Resolve<CoroutineService>().StartCoroutine(Wait());

         return result;
      }

      public static CustomYieldInstruction ToYielder<T>(this Promise<T> self)
      {
         return new PromiseYieldInstruction<T>(self);
      }
   }

   public class PromiseYieldInstruction<T> : CustomYieldInstruction
   {
      private readonly Promise<T> _promise;

      public PromiseYieldInstruction(Promise<T> promise)
      {
         _promise = promise;
      }

      public override bool keepWaiting => !_promise.IsCompleted;
   }
}