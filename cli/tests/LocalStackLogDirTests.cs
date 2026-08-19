using System;
using System.IO;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers <see cref="LocalStackRunStateIO.ResolveRunLogsDir"/> — the per-run log directory scheme that
/// makes `beam local up` collision-proof (a fresh run never reuses a leftover wrapper's log path) and
/// routes logs to a temp folder by default vs. the workspace under `--save-logs`.
/// </summary>
public class LocalStackLogDirTests
{
	private static string Manifest(string dir) => Path.Combine(dir, ".beamable", "local-stack.json");

	[Test]
	public void Save_true_lives_under_the_workspace()
	{
		var manifest = Manifest(@"C:\repos\projectA");
		var dir = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: true);

		Assert.That(dir, Does.Contain(LocalStackRunStateIO.LogsDirName));
		Assert.That(dir, Does.Contain(Path.Combine(".beamable", LocalStackRunStateIO.LogsDirName)));
		Assert.That(Path.GetFileName(dir), Does.StartWith("run-"));
	}

	[Test]
	public void Save_false_lives_under_the_temp_root()
	{
		var manifest = Manifest(@"C:\repos\projectA");
		var dir = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: false);

		Assert.That(dir, Does.StartWith(Path.GetFullPath(Path.GetTempPath()))
			.Or.StartWith(Path.GetTempPath()));
		Assert.That(dir, Does.Contain("beam-local-stack"));
		Assert.That(Path.GetFileName(dir), Does.StartWith("run-"));
	}

	[Test]
	public void Each_call_returns_a_distinct_run_leaf()
	{
		var manifest = Manifest(@"C:\repos\projectA");
		var a = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: false);
		var b = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: false);

		Assert.That(Path.GetFileName(a), Is.Not.EqualTo(Path.GetFileName(b)),
			"two runs must not share a log folder, even within the same second");
	}

	[Test]
	public void Different_workspaces_get_distinct_temp_segments()
	{
		// Same folder NAME under different parents must not collide under the shared temp root.
		var a = LocalStackRunStateIO.ResolveRunLogsDir(Manifest(@"C:\repos\one\agentic-portal"), save: false);
		var b = LocalStackRunStateIO.ResolveRunLogsDir(Manifest(@"C:\repos\two\agentic-portal"), save: false);

		// Compare the parent (…/beam-local-stack/<hash>) — the run-<id> leaf differs regardless.
		Assert.That(Path.GetDirectoryName(a), Is.Not.EqualTo(Path.GetDirectoryName(b)));
	}

	[Test]
	public void Same_workspace_reuses_the_temp_segment_but_new_leaf()
	{
		var manifest = Manifest(@"C:\repos\projectA");
		var a = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: false);
		var b = LocalStackRunStateIO.ResolveRunLogsDir(manifest, save: false);

		Assert.That(Path.GetDirectoryName(a), Is.EqualTo(Path.GetDirectoryName(b)),
			"the same manifest must hash to the same temp segment");
		Assert.That(a, Is.Not.EqualTo(b));
	}
}
