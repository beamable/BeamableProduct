using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.UI.ContentWindow;
using NUnit.Framework;
using System.Linq;

namespace Beamable.Editor.Tests
{
	public class ContentWindowSnapshotTests
	{
		private static BeamManifestSnapshotItem Snapshot(string name, string path)
		{
			return new BeamManifestSnapshotItem { Name = name, Path = path };
		}

		[Test]
		public void BuildSnapshotLookup_DuplicateNamesAcrossRealms_KeepsAllSnapshots()
		{
			// Auto snapshots share the name "LastPublished-<manifestId>" across realm folders.
			var snapshots = new[]
			{
				Snapshot("LastPublished-global", ".beamable/content-snapshots/DE_111/LastPublished-global.json"),
				Snapshot("LastPublished-global", ".beamable/content-snapshots/DE_222/LastPublished-global.json"),
			};

			var lookup = ContentWindow.BuildSnapshotLookup(snapshots);

			Assert.AreEqual(2, lookup.Count);
			CollectionAssert.AreEquivalent(snapshots.Select(item => item.Path), lookup.Keys);
		}

		[Test]
		public void BuildSnapshotLookup_KeysByPath()
		{
			var snapshots = new[]
			{
				Snapshot("my-snapshot", ".beamable/content-snapshots/DE_111/my-snapshot.json"),
				Snapshot("other-snapshot", ".beamable/temp/content-snapshots/DE_111/other-snapshot.json"),
			};

			var lookup = ContentWindow.BuildSnapshotLookup(snapshots);

			Assert.AreSame(snapshots[0], lookup[snapshots[0].Path]);
			Assert.AreSame(snapshots[1], lookup[snapshots[1].Path]);
		}

		[Test]
		public void BuildSnapshotLookup_EmptyInput_ReturnsEmptyLookup()
		{
			var lookup = ContentWindow.BuildSnapshotLookup(System.Array.Empty<BeamManifestSnapshotItem>());

			Assert.AreEqual(0, lookup.Count);
		}
	}
}
