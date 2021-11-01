using System;
using System.Collections.Generic;
using UnityEditor;
namespace Beamable.Editor
{
   public static class EditorDebouncer
   {
      private static Dictionary<string, EditorTimeout> _debounces = new Dictionary<string, EditorTimeout>();

      /// <summary>
      /// Only run the given action once, even if this method is called multiple times.
      /// Everytime the method is called, schedule the action to run after some delay.
      /// Cancel the previously scheduled action if it hasn't executed yet.
      /// </summary>
      /// <param name="key">A uniqye key per call site that is used to schedule the action</param>
      /// <param name="cb">the action that will be invoked after the delay</param>
      /// <param name="debounceDelay">the number of seconds to wait</param>
      public static void Debounce(string key, Action cb, float debounceDelay=.3f)
      {
         if (_debounces.TryGetValue(key, out var existingTimeout))
         {
            ClearTimeout(existingTimeout);
         }
         var timeout = SetTimeout(cb, debounceDelay);
         _debounces[key] = timeout;
      }

      /// <summary>
      /// Cancel a timeout from executing
      /// </summary>
      /// <param name="timeout"></param>
      public static void ClearTimeout(EditorTimeout timeout)
      {
         timeout?.Cancel();
      }

      /// <summary>
      /// Schedule some action to run after some delay has passed
      /// </summary>
      /// <param name="action">the action to run</param>
      /// <param name="delay">the amount of seconds to wait before running the action</param>
      /// <returns></returns>
      public static EditorTimeout SetTimeout(Action action, double delay)
      {
         var timeout = new EditorTimeout
         {
            Callback = action,
            ExecuteAfter = EditorApplication.timeSinceStartup + delay
         };

         void Update()
         {
            if (timeout.IsCancelled || EditorApplication.timeSinceStartup > timeout.ExecuteAfter)
            {
               EditorApplication.update -= Update;
               timeout.Invoke();
            }
         }

         EditorApplication.update += Update;

         return timeout;
      }

      public class EditorTimeout
      {
         public Action Callback;
         public double ExecuteAfter;

         public bool IsCancelled;

         public void Cancel()
         {
            IsCancelled = true;
         }

         public void Invoke()
         {
            if (IsCancelled) return;
            Cancel();
            Callback?.Invoke();
         }
      }
   }
}