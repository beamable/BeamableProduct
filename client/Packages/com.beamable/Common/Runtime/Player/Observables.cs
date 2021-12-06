using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Content;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.Player
{
 public interface IObservable
   {
      bool IsLoading { get; }
      event Action OnUpdated;
      event Action OnLoadingStarted;
      event Action OnLoadingFinished;
      Promise Refresh();
   }

   public abstract class AbsObservable : IObservable
   {
      public bool IsLoading { get; private set; }
      public event Action OnUpdated;
      public event Action OnLoadingStarted;
      public event Action OnLoadingFinished;

      private int _lastBroadcastHashcode;
      private Promise _pendingRefresh;

      public abstract object GetData();

      public async Promise Refresh()
      {
         if (IsLoading)
         {
            await _pendingRefresh;
            return;
         }

         IsLoading = true;
         try
         {
            OnLoadingStarted?.Invoke();
            _pendingRefresh = PerformRefresh();
            await _pendingRefresh;
            TriggerUpdate();
         }
         finally
         {
            _pendingRefresh = null;
            IsLoading = false;
            OnLoadingFinished?.Invoke();
         }
      }

      protected void TriggerUpdate()
      {
         // is the data the same as it was before?
         // we make that decision based on the hash code of the element...
         var hash = GetBroadcastHashCode();
         var isDifferent = hash != _lastBroadcastHashcode;
         _lastBroadcastHashcode = hash;

         if (isDifferent)
         {
            OnUpdated?.Invoke();
         }
      }

      protected virtual int GetBroadcastHashCode()
      {
         return GetHashCode();
      }

      protected abstract Promise PerformRefresh();
   }

   [Serializable]
   public class ObservableLong : Observable<long>
   {
	   public static implicit operator ObservableLong(long data) => new ObservableLong{ Value = data };
   }

   [Serializable]
   public class ObservableString : Observable<string>
   {
	   public static implicit operator ObservableString(string data) => new ObservableString{ Value = data };
   }

   public class Observable<T> : AbsObservable
   {
	   [SerializeField]
	   private T _data = default(T);

	   private bool _assigned;

	   public bool IsAssigned => _assigned;

	   public bool IsNullOrUnassigned => !_assigned || _data == null;

	   public event Action<T> OnDataUpdated;

	   public T Value {
		   get => _data;
		   set {
			   _assigned = true;
			   _data = value;
			   TriggerUpdate();
		   }
	   }

	   public static implicit operator T(Observable<T> observable) => observable.Value;

	   public Observable()
	   {
		   OnUpdated += () => { OnDataUpdated?.Invoke(_data); };
	   }

	   public Observable(T data) : this()
	   {
		   _data = data;
	   }

	   public void BindTo(Observable<T> other)
	   {
		   other.OnUpdated += () => Value = other.Value;
	   }

	   public override string ToString()
	   {
		   return _data?.ToString();
	   }

	   protected override Promise PerformRefresh()
	   {
		   // do nothing.
		   return Promise.Success;
	   }

	   public override object GetData() => Value;

	   protected override int GetBroadcastHashCode()
	   {
		   return _assigned ? _data?.GetHashCode() ?? 0 : -1;
	   }
   }

   public interface IObservableReadonlyList<out T> : IReadOnlyCollection<T>, IObservable
   {
      T this[int index] { get; }
   }

   public abstract class AbsObservableReadonlyList<T> : AbsObservable, IObservableReadonlyList<T>
   {
      [SerializeField]
      private List<T> _data = new List<T>(); // set to new() to avoid null
      public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public int Count => _data.Count;
      public T this[int index] => _data[index];
      public event Action<List<T>> OnDataUpdated;


      public bool IsInitialized { get; protected set; }

      public AbsObservableReadonlyList()
      {
	      OnUpdated += () => { OnDataUpdated?.Invoke(_data.ToList()); };
      }

      protected override int GetBroadcastHashCode()
      {
         /*
          * We want to use a hash code based on the elements of the list at the given moment.
          */
         var res = 0x2D2816FE;
         foreach(var item in this)
         {
            res = res * 31 + (item == null ? 0 : item.GetHashCode());
         }
         return res;
      }

      protected void SetData(List<T> nextData)
      {
         _data = nextData;
      }

      public override object GetData() => _data;
   }

}
