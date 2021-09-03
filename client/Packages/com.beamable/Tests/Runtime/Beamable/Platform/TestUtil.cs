using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Beamable.Common;
using Beamable.Platform.SDK;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Platform.Tests
{
   public static class TestUtil
   {
      public static WaitForPromise<T> AsYield<T>(this Promise<T> arg, float timeout=.2f)
      {
         return new WaitForPromise<T>(arg, timeout);
      }

      public static string GetField(this WWWForm form, string field)
      {
         var fieldNames = typeof(WWWForm).GetField("fieldNames", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form) as List<string>;
         var formData = typeof(WWWForm).GetField("formData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form) as List<byte[]>;

         var formIndex = fieldNames.IndexOf(field);
         if (formIndex == -1)
         {
            throw new Exception($"Form does not contain value for {field}");
         }
         var data = formData[formIndex];
         var decoded = Encoding.UTF8.GetString(data);
         return decoded;
      }
   }

   public class WaitForPromise<T> : CustomYieldInstruction
   {
      private readonly Promise<T> _promise;
      private float _murderAt;
      private long tickCounter = 0;
      private long tickLimit;

      public WaitForPromise(Promise<T> promise, float timeOut=.2f)
      {
         _promise = promise;
         tickLimit = (long)(timeOut * 1000 * 1000 * 10000 * 1000);
         _murderAt = Time.realtimeSinceStartup + timeOut;
      }

      public override bool keepWaiting
      {
         get
         {
            if (_promise.IsCompleted)
            {
               return false;
            }

            tickCounter++;

            if (tickCounter > tickLimit)
            // if (Time.realtimeSinceStartup > _murderAt)
            {
               Debug.LogError("Yielded timeout");
               _promise.CompleteError(new Exception("The WaitForPromise timed out after " + tickLimit + " ticks.") );
               return false;
            }
            return true;
         }
      }
   }
}