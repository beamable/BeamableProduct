using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using cli;
using cli.Content;
using NUnit.Framework;

namespace tests;

public class ContentFilesystemWatcherCleanupTests
{
	[TestCase(0, "cancel")]
	[TestCase(1, "cancel")]
	[TestCase(2, "cancel")]
	[TestCase(0, "failure")]
	[TestCase(1, "failure")]
	[TestCase(2, "failure")]
	[TestCase(0, "break")]
	[TestCase(1, "break")]
	[TestCase(2, "break")]
	public async Task Listener_releases_its_watcher_and_can_restart(int kind, string exit)
	{
		var root = Path.Combine(Path.GetTempPath(), "beam-watcher-cleanup-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, ConfigService.CFG_FOLDER));
		var config = new ConfigService(null);
		config.SetWorkingDir(root);
		var service = new ContentService(new CliRequester(new RequesterInfo("cid", "pid", "test", "test", "http://localhost")),
			config, null!, null!, null!, null!, null!);
		var dictionaryName = new[] { "_watchers", "_contentHistoryEntryWatchers", "_contentHistoryChangelistWatchers" }[kind];
		var watchers = (Dictionary<(string, string), FileSystemWatcher>)typeof(ContentService)
			.GetField(dictionaryName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
		var folder = kind switch
		{
			0 => service.EnsureContentPathForRealmExists(out _, "pid", "global"),
			1 => service.EnsureContentHistoryEntriesFolderExists(out _, "pid", "global"),
			_ => service.EnsureContentHistoryChangelistsFolderExists(out _, "pid", "global")
		};
		using var cancellation = new CancellationTokenSource();
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		try
		{
			async Task Consume()
			{
				await foreach (var _ in Watch(cancellation.Token))
				{
					if (exit == "failure") throw new InvalidOperationException("consumer failed");
					break;
				}
			}
			var consume = Consume();
			var original = watchers.Values.Single();
			var disposed = false;
			original.Disposed += (_, _) => disposed = true;
			if (exit == "cancel") { cancellation.Cancel(); cancellation.Cancel(); }
			else await File.WriteAllTextAsync(NewPath(), "{}");
			if (exit == "failure")
				Assert.ThrowsAsync<InvalidOperationException>(async () => await consume.WaitAsync(deadline.Token));
			else await consume.WaitAsync(deadline.Token);
			Assert.That(disposed, Is.True, "Iterator exit must dispose its filesystem watcher.");
			Assert.That(watchers, Is.Empty, "A completed listener must release its registration.");

			// Restart on the same service, then require an event for a newly created file.
			await using (var restarted = Watch(deadline.Token).GetAsyncEnumerator())
			{
				var move = restarted.MoveNextAsync().AsTask();
				Assert.That(watchers.Values.Single(), Is.Not.SameAs(original));
				var path = NewPath();
				await File.WriteAllTextAsync(path, "{}");
				while (await move.WaitAsync(deadline.Token))
				{
					if (ContainsPath(restarted.Current, path)) break;
					move = restarted.MoveNextAsync().AsTask();
				}
				Assert.That(ContainsPath(restarted.Current, path), Is.True);
			}
			Assert.That(watchers, Is.Empty);
		}
		finally
		{
			cancellation.Cancel();
			deadline.Cancel();
			// Cleanup also runs on the pre-fix failure; assertions above happen before this fallback.
			foreach (var watcher in watchers.Values.ToArray()) watcher.Dispose();
			Directory.Delete(root, true);
		}

		string NewPath()
		{
			var uid = Guid.NewGuid().ToString("N");
			var name = kind == 1 ? ChangedContentHistoryEntryFile.BuildEntryFileName(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), uid) : uid;
			return Path.Combine(folder, name + ".json");
		}
		IAsyncEnumerable<object> Watch(CancellationToken token) => kind switch
		{
			0 => Box(service.ListenToLocalContentFileChanges("pid", token: token)),
			1 => Box(service.ListenToContentHistoryEntries("pid", token: token)),
			_ => Box(service.ListenToContentHistoryChangelists("pid", token: token))
		};
	}

	private static async IAsyncEnumerable<object> Box<T>(IAsyncEnumerable<T> source)
	{
		await foreach (var item in source) yield return item!;
	}
	private static bool ContainsPath(object change, string path) => change switch
	{
		LocalContentFileChanges batch => batch.AllFileChanges.Any(item => item.FullFilePath == path),
		LocalContentHistoryEntryFileChanges batch => batch.AllFileChanges.Any(item => item.FullFilePath == path),
		LocalContentHistoryChangelistFileChanges batch => batch.AllFileChanges.Any(item => item.FullFilePath == path),
		_ => false
	};
}
