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
		event Action OnUpdated;
	}

	public interface IRefreshable
	{
		Promise Refresh();
	}

	public class DefaultObservable : IObservable
	{
		public event Action OnUpdated;

		private int _lastBroadcastChecksum;

		protected void TriggerUpdate()
		{
			// is the data the same as it was before?
			// we make that decision based on the hash code of the element...
			var hash = GetBroadcastChecksum();
			var isDifferent = hash != _lastBroadcastChecksum;
			// var oldLast = _lastBroadcastChecksum;
			_lastBroadcastChecksum = hash;

			if (isDifferent)
			{
				OnUpdated?.Invoke();
			}
		}

		public virtual int GetBroadcastChecksum()
		{
			return GetHashCode();
		}
	}

	public abstract class AbsRefreshableObservable : DefaultObservable, IRefreshable
	{

		public event Action OnLoadingStarted;
		public event Action OnLoadingFinished;
		private Promise _pendingRefresh;

		public bool IsLoading
		{
			get;
			private set;
		}


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
			// TODO: error case?
			finally
			{
				_pendingRefresh = null;
				IsLoading = false;
				OnLoadingFinished?.Invoke();
			}
		}

		protected abstract Promise PerformRefresh();
	}

	[Serializable]
	public class ObservableLong : Observable<long>
	{
		public static implicit operator ObservableLong(long data) => new ObservableLong {Value = data};
	}

	[Serializable]
	public class ObservableString : Observable<string>
	{
		public static implicit operator ObservableString(string data) => new ObservableString {Value = data};
	}

	public class Observable<T> : AbsRefreshableObservable
	{
		[SerializeField]
		private T _data = default(T);

		private bool _assigned;

		public bool IsAssigned => _assigned;

		public bool IsNullOrUnassigned => !_assigned || _data == null;

		public event Action<T> OnDataUpdated;

		public T Value
		{
			get => _data;
			set
			{
				_assigned = true;
				_data = value;
				TriggerUpdate();
			}
		}

		public static implicit operator T(Observable<T> observable) => observable.Value;

		public Observable()
		{
			OnUpdated += () =>
			{
				OnDataUpdated?.Invoke(_data);
			};
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

		public override int GetBroadcastChecksum()
		{
			return _assigned ? _data?.GetHashCode() ?? 0 : -1;
		}
	}

	public interface IObservableReadonlyList<out T> : IReadOnlyCollection<T>, IObservable
	{
		T this[int index]
		{
			get;
		}
	}

	public interface IObservableReadonlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IObservable { }

	public interface IGetProtectedDataList<T>
	{
		List<T> Data
		{
			get;
		}

		void CheckForUpdate();
	}

	public abstract class AbsObservableReadonlyList<T> : AbsRefreshableObservable, IObservableReadonlyList<T>,
	                                                     IGetProtectedDataList<T>
	{
		[SerializeField]
		private List<T> _data = new List<T>(); // set to new() to avoid null

		public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count => _data.Count;
		public T this[int index] => _data[index];
		public event Action<List<T>> OnDataUpdated;

		public event Action<IEnumerable<T>> OnElementsAdded;
		public event Action<IEnumerable<T>> OnElementRemoved;

		public bool IsInitialized
		{
			get;
			protected set;
		}

		public AbsObservableReadonlyList()
		{
			OnUpdated += () =>
			{
				OnDataUpdated?.Invoke(_data.ToList());
			};
		}

		public override int GetBroadcastChecksum()
		{
			/*
			 * We want to use a hash code based on the elements of the list at the given moment.
			 */
			var res = 0x2D2816FE;
			foreach (var item in this)
			{
				var itemCode = 0;
				switch (item)
				{
					case DefaultObservable obs:
						itemCode = obs.GetBroadcastChecksum();
						break;
					case object obj:
						itemCode = obj.GetHashCode();
						break;
				}
				res = res * 31 + (itemCode);
			}

			return res;
		}

		protected void SetData(List<T> nextData)
		{
			// check for additions, and deletions...
			var added = new HashSet<T>();
			var existing = new HashSet<T>(_data);
			foreach (var next in nextData)
			{
				if (!existing.Contains(next))
				{
					added.Add(next);
				}
				else
				{
					existing.Remove(next);
				}
			}

			if (existing.Count > 0)
			{
				OnElementRemoved?.Invoke(existing);
			}

			if (added.Count > 0)
			{
				OnElementsAdded?.Invoke(added);
			}

			_data = nextData;
		}

		public override object GetData() => _data;
		List<T> IGetProtectedDataList<T>.Data => _data;
		void IGetProtectedDataList<T>.CheckForUpdate() => TriggerUpdate();
	}

	public abstract class AbsObservableReadonlyDictionary<TValue, TDict>
		: AbsRefreshableObservable, IObservableReadonlyDictionary<string, TValue>
		where TDict : SerializableDictionaryStringToSomething<TValue>, new()
	{

		[SerializeField]
		private TDict _data =
			new TDict();

		public event Action<SerializableDictionaryStringToSomething<TValue>> OnDataUpdated;


		public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => _data.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

		public int Count => _data.Count;

		public bool ContainsKey(string key) => _data.ContainsKey(key);

		public bool TryGetValue(string key, out TValue value) => _data.TryGetValue(key, out value);

		public TValue this[string key] => _data[key];

		public IEnumerable<string> Keys => _data.Keys;
		public IEnumerable<TValue> Values => _data.Values;

		public override object GetData() => _data;

		public bool IsInitialized
		{
			get;
			protected set;
		}

		public AbsObservableReadonlyDictionary()
		{
			OnUpdated += () =>
			{
				OnDataUpdated?.Invoke(_data);
			};
		}

		protected void SetData(TDict nextData)
		{
			_data = nextData;
		}

		public override int GetBroadcastChecksum()
		{
			/*
			  * We want to use a hash code based on the elements of the list at the given moment.
			  */
			var res = 0x2D2816FE;
			foreach (var item in this)
			{
				res = res * 31 + (item.Value == null ? 0 : item.GetHashCode());
			}

			// TODO: need to include keys in hash.
			return res;
		}
	}

}
