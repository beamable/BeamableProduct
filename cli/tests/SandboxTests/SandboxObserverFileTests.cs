using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxObserverFileTests
{
	private const long LauncherId = 4827;
	private string _repoRoot = null!;
	private FakeClock _clock = null!;
	private SandboxObserver _observer = null!;

	[SetUp]
	public void SetUp()
	{
		_repoRoot = Path.Combine(Path.GetTempPath(), "sandbox-obs-files-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_repoRoot);
		_clock = new FakeClock(DateTimeOffset.UnixEpoch);
		_observer = new SandboxObserver(
			launcherAccountId: LauncherId,
			cid: "1", pid: "2",
			serviceName: "BeamSandbox_4827_d1f2c3a4-b5e6-4a7b-8c9d-0123456789ab",
			repoRoot: _repoRoot,
			joinCode: "K2M-X9Q3",
			sandboxVersion: "0.0.1",
			label: null,
			clock: _clock);
	}

	[TearDown]
	public void TearDown()
	{
		_observer.Dispose();
		try { Directory.Delete(_repoRoot, recursive: true); } catch { }
	}

	private string Rel(params string[] segments) => string.Join('/', segments);

	// --- Auth/containment guards ------------------------------------------------------

	[Test]
	public void AllFileRoutes_RejectMismatchedAccount()
	{
		var bad = LauncherId + 1;
		Assert.That(_observer.ListDir(bad, ".", false).Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.Stat(bad, "x").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.ReadFile(bad, "x", null, null).Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.WriteFile(bad, "x", "c", null).Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.DeleteFile(bad, "x").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.Rename(bad, "a", "b").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.MakeDir(bad, "a").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.WatchPaths(bad, new[] { "a" }).Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(_observer.UnwatchPaths(bad, "id").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
	}

	[Test]
	public void Read_PathOutsideRoot_Returns403()
	{
		File.WriteAllText(Path.Combine(Path.GetTempPath(), "escape-target-" + Guid.NewGuid()), "secret");
		var outcome = _observer.ReadFile(LauncherId, "../escape.txt", null, null);
		Assert.That((int)outcome.Status, Is.EqualTo(403));
		Assert.That(outcome.Error, Is.EqualTo("path-outside-root"));
	}

	[Test]
	public void Write_PathOutsideRoot_Returns403()
	{
		var outcome = _observer.WriteFile(LauncherId, "../escape.txt", "x", null);
		Assert.That((int)outcome.Status, Is.EqualTo(403));
	}

	[Test]
	public void Rename_EitherPathOutside_Returns403()
	{
		File.WriteAllText(Path.Combine(_repoRoot, "a.txt"), "x");
		var outA = _observer.Rename(LauncherId, "../escape", "a.txt");
		var outB = _observer.Rename(LauncherId, "a.txt", "../escape");
		Assert.That((int)outA.Status, Is.EqualTo(403));
		Assert.That((int)outB.Status, Is.EqualTo(403));
	}

	[Test]
	public void Watch_PathOutsideRoot_Returns403()
	{
		var outcome = _observer.WatchPaths(LauncherId, new[] { "../outside" });
		Assert.That((int)outcome.Status, Is.EqualTo(403));
	}

	// --- Read/write round-trip --------------------------------------------------------

	[Test]
	public void Write_ThenRead_RoundTrips()
	{
		Assert.That(_observer.WriteFile(LauncherId, "foo.txt", "hello", null).Status,
			Is.EqualTo(SandboxRouteStatus.Ok));

		var read = _observer.ReadFile(LauncherId, "foo.txt", null, null);
		Assert.That(read.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		Assert.That(read.Result!.Contents, Is.EqualTo("hello"));
		Assert.That(read.Result.ContentHash, Is.Not.Null);
	}

	[Test]
	public void Write_StaleHash_ReturnsConflictWithCurrentHash()
	{
		_observer.WriteFile(LauncherId, "a.txt", "original", null);
		var current = SandboxFileService.ComputeFileHash(Path.Combine(_repoRoot, "a.txt"));

		var outcome = _observer.WriteFile(LauncherId, "a.txt", "updated",
			expectedContentHash: "deadbeef".PadRight(64, '0'));
		Assert.That((int)outcome.Status, Is.EqualTo(409));
		Assert.That(outcome.ConflictHash, Is.EqualTo(current));
		// Original content must still be on disk.
		Assert.That(File.ReadAllText(Path.Combine(_repoRoot, "a.txt")), Is.EqualTo("original"));
	}

	// --- ListDir / Stat ---------------------------------------------------------------

	[Test]
	public void ListDir_RootListing_Works()
	{
		_observer.WriteFile(LauncherId, "a.txt", "1", null);
		_observer.MakeDir(LauncherId, "sub");

		var outcome = _observer.ListDir(LauncherId, ".", showHidden: false);
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		var names = outcome.Listing!.Entries.Select(e => e.Name).ToArray();
		Assert.That(names, Contains.Item("a.txt"));
		Assert.That(names, Contains.Item("sub"));
	}

	[Test]
	public void Stat_MissingPath_Returns404()
	{
		var outcome = _observer.Stat(LauncherId, "missing.txt");
		Assert.That((int)outcome.Status, Is.EqualTo(404));
	}

	// --- Watch ------------------------------------------------------------------------

	[Test]
	public async Task Watch_FileChange_FlowsThroughBatcher()
	{
		_observer.MakeDir(LauncherId, "watched");
		var watch = _observer.WatchPaths(LauncherId, new[] { "watched" });
		Assert.That(watch.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		Assert.That(watch.WatchId, Is.Not.Empty);

		// Modify a file in the watched directory.
		var path = Path.Combine(_repoRoot, "watched", "new.txt");
		await File.WriteAllTextAsync(path, "content");

		// FileSystemWatcher events fire asynchronously; poll the batcher until events arrive
		// or a generous timeout is exceeded.
		SandboxEvent.FileChanged? observed = null;
		var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
		while (DateTimeOffset.UtcNow < deadline && observed == null)
		{
			_clock.Advance(TimeSpan.FromSeconds(2)); // force batcher window elapsed
			var batch = _observer.PumpBatcher();
			if (batch != null)
			{
				observed = batch.Events.OfType<SandboxEvent.FileChanged>().FirstOrDefault();
			}
			await Task.Delay(50);
		}

		Assert.That(observed, Is.Not.Null, "expected a file-changed event from the watcher");
		Assert.That(observed!.WatchId, Is.EqualTo(watch.WatchId));
	}

	[Test]
	public void Unwatch_KnownId_Succeeds_UnknownId_Returns404()
	{
		_observer.MakeDir(LauncherId, "watched");
		var watchId = _observer.WatchPaths(LauncherId, new[] { "watched" }).WatchId!;

		Assert.That(_observer.ActiveWatchCount, Is.EqualTo(1));
		Assert.That(_observer.UnwatchPaths(LauncherId, watchId).Status, Is.EqualTo(SandboxRouteStatus.Ok));
		Assert.That(_observer.ActiveWatchCount, Is.EqualTo(0));

		Assert.That((int)_observer.UnwatchPaths(LauncherId, "no-such-id").Status, Is.EqualTo(404));
	}

	[Test]
	public void Watch_EmptyPaths_Rejected()
	{
		var outcome = _observer.WatchPaths(LauncherId, Array.Empty<string>());
		Assert.That(outcome.Error, Is.EqualTo("empty-paths"));
	}

	[Test]
	public void Dispose_TearsDownAllWatchers()
	{
		_observer.MakeDir(LauncherId, "w1");
		_observer.MakeDir(LauncherId, "w2");
		_observer.WatchPaths(LauncherId, new[] { "w1" });
		_observer.WatchPaths(LauncherId, new[] { "w2" });
		Assert.That(_observer.ActiveWatchCount, Is.EqualTo(2));

		_observer.Dispose();
		Assert.That(_observer.ActiveWatchCount, Is.EqualTo(0));
	}
}
