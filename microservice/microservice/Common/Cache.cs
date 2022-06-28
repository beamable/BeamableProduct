using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace microservice.Common
{
	public class Cache<TKey, TValue>
	{
		public delegate Task<TValue> Resolver(TKey key);

		private ConcurrentDictionary<TKey, Task<TValue>> _data = new ConcurrentDictionary<TKey, Task<TValue>>();
		private Resolver _resolver;

		public Cache(Resolver resolver)
		{
			_resolver = resolver;
		}

		public void PurgeAllExcept(HashSet<TKey> keysToKeep)
		{
			var existingKeys = _data.Keys.ToHashSet();
			var keysToRemove = existingKeys.Except(keysToKeep);

			foreach (var key in keysToRemove)
			{
				Purge(key);
			}
		}

		public void PurgeAll()
		{
			_data.Clear();
		}

		public void Purge(TKey key)
		{
			_data.TryRemove(key, out _);
		}

		public Task<TValue> Get(TKey key)
		{
			if (_data.TryGetValue(key, out var existing))
			{
				return existing;
			}

			var task = _resolver(key);
			_data.AddOrUpdate(key, task, (_, oldValue) => task);

			return task;
		}
	}
}
