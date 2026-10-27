using cli.Services.HttpServer;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace tests;

public class ServerLocatorFileTests
{
	private string _tempDir = null!;

	[SetUp]
	public void SetUp()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "beam-locator-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
	}

	[TearDown]
	public void TearDown()
	{
		Directory.Delete(_tempDir, true);
	}

	private static ServerLocator Locator(int pid, long startedAt = 1) => new()
	{
		pid = pid, port = 8400, uri = "http://127.0.0.1:8400/", owner = "tests", version = "0.0.0", token = "secret",
		startedAt = startedAt
	};

	[Test]
	public void Written_locator_is_private_to_the_user()
	{
		var path = ServerLocatorFile.Write(_tempDir, Locator(Environment.ProcessId));
		Assert.That(path, Is.EqualTo(ServerLocatorFile.GetPath(_tempDir, Environment.ProcessId)));
		if (!OperatingSystem.IsWindows())
		{
			Assert.That(File.GetUnixFileMode(path), Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite),
				"The file holds the session token.");
		}
		Assert.That(Directory.GetFiles(ServerLocatorFile.GetFolder(_tempDir)), Has.Length.EqualTo(1),
			"No temp file may be left behind.");
	}

	[Test]
	public void Live_locators_round_trip()
	{
		ServerLocatorFile.Write(_tempDir, Locator(Environment.ProcessId));
		var live = ServerLocatorFile.ReadLive(_tempDir);
		Assert.That(live, Has.Count.EqualTo(1));
		Assert.That(live[0].token, Is.EqualTo("secret"));
		Assert.That(live[0].port, Is.EqualTo(8400));
	}

	[Test]
	public void Locator_of_an_exited_process_is_skipped_and_pruned()
	{
		using var exited = Process.Start(new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true })!;
		exited.WaitForExit();
		ServerLocatorFile.Write(_tempDir, Locator(exited.Id));

		Assert.That(ServerLocatorFile.ReadLive(_tempDir), Is.Empty);
		Assert.That(File.Exists(ServerLocatorFile.GetPath(_tempDir, exited.Id)), Is.True, "Reading alone must not delete.");

		ServerLocatorFile.ReadLive(_tempDir, prune: true);
		Assert.That(File.Exists(ServerLocatorFile.GetPath(_tempDir, exited.Id)), Is.False);
	}

	[Test]
	public void Unreadable_locators_are_ignored()
	{
		Directory.CreateDirectory(ServerLocatorFile.GetFolder(_tempDir));
		File.WriteAllText(Path.Combine(ServerLocatorFile.GetFolder(_tempDir), "123.json"), "{not json");
		Assert.That(ServerLocatorFile.ReadLive(_tempDir), Is.Empty);
	}

	[Test]
	public void Missing_folder_means_no_servers()
	{
		Assert.That(ServerLocatorFile.ReadLive(Path.Combine(_tempDir, "nothing-here")), Is.Empty);
	}

	[Test]
	public void Delete_removes_the_locator()
	{
		var path = ServerLocatorFile.Write(_tempDir, Locator(Environment.ProcessId));
		ServerLocatorFile.Delete(path);
		Assert.That(ServerLocatorFile.ReadLive(_tempDir).Any(), Is.False);
	}
}
