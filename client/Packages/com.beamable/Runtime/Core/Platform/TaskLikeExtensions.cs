using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Beamable.Common;
using Beamable.Coroutines;
using Beamable.Service;
using UnityEngine;

namespace Core.Platform
{


   public abstract class BeamableTaskLike<TResult> : ITaskLike<TResult, BeamableTaskLike<TResult>>
   {
	   public abstract TResult GetResult();
      public abstract bool IsCompleted { get; }

      public Guid Id { get; } = Guid.NewGuid();

      public BeamableTaskLike<TResult> GetAwaiter()
      {
         return this;
      }

      void INotifyCompletion.OnCompleted(Action continuation)
      {
         ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(continuation);
      }

      public abstract void UnsafeOnCompleted(Action continuation);
   }
}
