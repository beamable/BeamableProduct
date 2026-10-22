using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;

namespace Beamable.Editor.ContentService
{
	/// <summary>
	/// Keeps deletion behind every write already submitted for a content/scope key.
	/// Used on the editor thread; this does not queue writes or cancel CLI commands.
	/// </summary>
	internal sealed class ContentWriteTracker
	{
		private sealed class PendingWrite
		{
			public readonly Promise Settled = new();
			public Exception Error;
		}

		private readonly Dictionary<string, List<PendingWrite>> _writes = new();
		private readonly Dictionary<string, Promise> _deletions = new();

		public bool IsDeleting(string key) => _deletions.ContainsKey(key);
		public bool HasWrites(string key) => _writes.ContainsKey(key);

		public async Promise WaitForWrites(string key)
		{
			Exception error = null;
			while (_writes.TryGetValue(key, out var writes))
				foreach (var write in writes.ToArray())
				{
					await write.Settled;
					error ??= write.Error;
				}
			if (error != null) throw error;
		}

		public async Promise Run(string key, Func<Promise> write)
		{
			if (IsDeleting(key)) return;
			if (!_writes.TryGetValue(key, out var writes))
			{
				_writes[key] = writes = new List<PendingWrite>();
			}

			// Register before dispatch: deletion must also see commands that complete synchronously.
			var pending = new PendingWrite();
			writes.Add(pending);
			try
			{
				await write();
			}
			catch (Exception error)
			{
				pending.Error = error;
				throw;
			}
			finally
			{
				writes.Remove(pending);
				if (writes.Count == 0) _writes.Remove(key);
				// Deletion needs settlement, including failures. The caller still receives the write error.
				pending.Settled.CompleteSuccess();
			}
		}

		public Promise Delete(string key, Action delete)
		{
			if (_deletions.TryGetValue(key, out var pending)) return pending;
			var completion = new Promise();
			_deletions.Add(key, completion);
			FinishDelete(key, delete, completion);
			return completion;
		}

		private async void FinishDelete(string key, Action delete, Promise completion)
		{
			Exception error = null;
			try
			{
				// New writes are blocked, so this snapshot includes everything deletion must wait for.
				if (_writes.TryGetValue(key, out var writes))
				{
					foreach (var write in writes.ToArray()) await write.Settled;
				}
				delete();
			}
			catch (Exception ex)
			{
				error = ex;
			}
			finally
			{
				_deletions.Remove(key);
			}

			if (error == null) completion.CompleteSuccess();
			else completion.CompleteError(error);
		}
	}
}
