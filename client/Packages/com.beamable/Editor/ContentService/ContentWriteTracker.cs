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
		private readonly Dictionary<string, List<Promise>> _writes = new();
		private readonly Dictionary<string, Promise> _deletions = new();

		public bool IsDeleting(string key) => _deletions.ContainsKey(key);
		public bool HasWrites(string key) => _writes.ContainsKey(key);

		public async Promise Run(string key, Func<Promise> write)
		{
			if (IsDeleting(key)) return;
			if (!_writes.TryGetValue(key, out var writes))
			{
				_writes[key] = writes = new List<Promise>();
			}

			// Register before dispatch: deletion must also see commands that complete synchronously.
			var settled = new Promise();
			writes.Add(settled);
			try
			{
				await write();
			}
			finally
			{
				writes.Remove(settled);
				if (writes.Count == 0) _writes.Remove(key);
				// Deletion needs settlement, including failures. The caller still receives the write error.
				settled.CompleteSuccess();
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
					foreach (var settled in writes.ToArray()) await settled;
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
