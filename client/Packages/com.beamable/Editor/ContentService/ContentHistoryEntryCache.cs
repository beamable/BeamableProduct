using Beamable.Editor.BeamCli.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.ContentService
{
	public class ContentHistoryEntryCache
	{
		private readonly Dictionary<string, BeamContentHistoryEntry> _entriesByManifestUid = new();

		public IReadOnlyList<BeamContentHistoryEntry> Entries => _entriesByManifestUid.Values
			.OrderByDescending(entry => entry.CreatedDate)
			.ToList();

		public void Apply(IEnumerable<BeamContentHistoryEntry> entries, IEnumerable<string> entriesToRemove = null)
		{
			if (entriesToRemove != null)
			{
				foreach (var manifestUid in entriesToRemove)
				{
					_entriesByManifestUid.Remove(manifestUid);
				}
			}

			if (entries == null)
			{
				return;
			}

			foreach (var entry in entries)
			{
				if (!string.IsNullOrEmpty(entry?.ManifestUid))
				{
					_entriesByManifestUid[entry.ManifestUid] = entry;
				}
			}
		}

		public void Clear()
		{
			_entriesByManifestUid.Clear();
		}
	}
}
