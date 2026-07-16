using Beamable.Editor.BeamCli.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.ContentService
{
	public class ContentHistoryEntryCache
	{
		private readonly Dictionary<string, BeamContentHistoryEntry> _entriesByManifestUid = new();
		private IReadOnlyList<BeamContentHistoryEntry> _sortedEntries = new List<BeamContentHistoryEntry>();
		private bool _isSortedEntriesDirty = true;

		public IReadOnlyList<BeamContentHistoryEntry> Entries
		{
			get
			{
				if (_isSortedEntriesDirty)
				{
					_sortedEntries = _entriesByManifestUid.Values
						.OrderByDescending(entry => entry.CreatedDate)
						.ToList();
					_isSortedEntriesDirty = false;
				}

				return _sortedEntries;
			}
		}

		public void Apply(IEnumerable<BeamContentHistoryEntry> entries, IEnumerable<string> entriesToRemove = null)
		{
			var hasChanges = false;
			if (entriesToRemove != null)
			{
				foreach (var manifestUid in entriesToRemove)
				{
					hasChanges |= _entriesByManifestUid.Remove(manifestUid);
				}
			}

			if (entries == null)
			{
				if (hasChanges)
				{
					_isSortedEntriesDirty = true;
				}

				return;
			}

			foreach (var entry in entries)
			{
				if (!string.IsNullOrEmpty(entry?.ManifestUid))
				{
					_entriesByManifestUid[entry.ManifestUid] = entry;
					hasChanges = true;
				}
			}

			if (hasChanges)
			{
				_isSortedEntriesDirty = true;
			}
		}

		public void Clear()
		{
			if (_entriesByManifestUid.Count == 0)
			{
				return;
			}

			_entriesByManifestUid.Clear();
			_isSortedEntriesDirty = true;
		}
	}
}
