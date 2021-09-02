using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Service;
using UnityEngine;

namespace Beamable.Coroutines
{
   [EditorServiceResolver(typeof(EditorSingletonMonoBehaviourServiceResolver<CoroutineService>))]
   public class CoroutineService : MonoBehaviour
   {
      private Dictionary<string, List<IEnumerator>> coroutines = new Dictionary<string, List<IEnumerator>>();
      private event Action _everySecond;

      public virtual Coroutine StartNew(string context, IEnumerator enumerator)
      {
         List<IEnumerator> contextCoroutines = null;
         if (!coroutines.TryGetValue(context, out contextCoroutines))
         {
            contextCoroutines = new List<IEnumerator>();
            coroutines[context] = contextCoroutines;
         }

         contextCoroutines.Add(enumerator);
         return StartCoroutine(RunCoroutine(contextCoroutines, enumerator));
      }

      public void StopAll(string context)
      {
         List<IEnumerator> contextCoroutines = null;
         if (coroutines.TryGetValue(context, out contextCoroutines))
         {
            coroutines.Remove(context);
            for (int i = 0; i < contextCoroutines.Count; i++)
            {
               StopCoroutine(contextCoroutines[i]);
            }
         }
      }

      public event Action EverySecond
      {
         add
         {
            if (_everySecond == null)
            {
               StartNew("everySecond", FireEverySecond());
            }
            _everySecond += value;
         }

         remove
         {
            _everySecond -= value;
            if (_everySecond == null)
            {
               StopAll("everySecond");
            }
         }
      }

      private IEnumerator RunCoroutine(List<IEnumerator> coroutines, IEnumerator enumerator)
      {
         yield return enumerator;
         coroutines.Remove(enumerator);
      }

      private IEnumerator FireEverySecond()
      {
         while(true)
         {
            yield return Yielders.Seconds(1.0f);
            _everySecond?.Invoke();
         }
      }
   }
}
