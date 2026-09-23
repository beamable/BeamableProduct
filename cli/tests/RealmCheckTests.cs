using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cli;
using NUnit.Framework;

namespace tests;

public class RealmCheckTests
{
	private class FakeRealm : IRealmCheckEnvironment
	{
		public string Provider = "beamable";
		public string Uri = "wss://socket.test";
		public bool HasGlobal = true;
		public string SocketProblem;
		public List<(string name, bool running, bool isCurrent)> Services = new();
		public bool ConfigFixSticks = true;
		public readonly List<string> Calls = new();

		public Task<(string provider, string uri)> GetWebsocketConfig() => Task.FromResult((Provider, Uri));

		public Task SetRealmConfig(string namespaceKey, string value)
		{
			Calls.Add($"set {namespaceKey}={value}");
			if (ConfigFixSticks && namespaceKey == RealmCheck.PublisherKey) Provider = value;
			return Task.CompletedTask;
		}

		public Task<bool> HasPublishedManifest(string manifestId) => Task.FromResult(manifestId == "global" && HasGlobal);

		public Task PublishManifest(string manifestId)
		{
			Calls.Add($"publish {manifestId}");
			if (manifestId == "global") HasGlobal = true;
			return Task.CompletedTask;
		}

		public Task<List<(string name, bool running, bool isCurrent)>> GetServiceStatuses() => Task.FromResult(Services);

		public Task<string> CheckSocketReachable(string websocketUri)
		{
			Calls.Add($"reach {websocketUri}");
			return Task.FromResult(SocketProblem);
		}
	}

	private static RealmCheckEntry Check(RealmCheckResult result, string id) => result.checks.Single(c => c.id == id);

	[Test]
	public async Task HealthyRealm_Passes()
	{
		var realm = new FakeRealm { Services = { ("MatchService", true, true) } };
		var result = await RealmCheck.RunWeb(realm, fix: false);

		Assert.IsTrue(result.ok);
		Assert.AreEqual("web", result.target);
		CollectionAssert.AreEqual(new[] { "notification-publisher", "global-manifest", "services", "realtime-socket" }, result.checks.Select(c => c.id));
		Assert.That(result.checks.Select(c => c.status), Is.All.EqualTo(RealmCheck.Pass));
	}

	[Test]
	public async Task PubnubRealmWithoutManifest_FailsWithFixCommands_AndDoesNotChangeAnything()
	{
		var realm = new FakeRealm { Provider = "pubnub", HasGlobal = false };
		var result = await RealmCheck.RunWeb(realm, fix: false);

		Assert.IsFalse(result.ok);
		Assert.AreEqual(RealmCheck.Fail, Check(result, "notification-publisher").status);
		Assert.That(Check(result, "notification-publisher").message, Does.Contain("Unsupported websocket provider"));
		Assert.AreEqual(RealmCheck.PublisherFixCommand, Check(result, "notification-publisher").fixCommand);
		Assert.AreEqual(RealmCheck.Fail, Check(result, "global-manifest").status);
		Assert.That(Check(result, "global-manifest").message, Does.Contain("id=global"));
		Assert.AreEqual(RealmCheck.ManifestFixCommand, Check(result, "global-manifest").fixCommand);
		Assert.IsFalse(realm.Calls.Any(c => c.StartsWith("set") || c.StartsWith("publish")), "no changes without --fix");
	}

	[Test]
	public async Task Fix_SetsPublisherAndPublishesGlobal()
	{
		var realm = new FakeRealm { Provider = "pubnub", HasGlobal = false };
		var result = await RealmCheck.RunWeb(realm, fix: true);

		Assert.IsTrue(result.ok);
		Assert.AreEqual(RealmCheck.Fixed, Check(result, "notification-publisher").status);
		Assert.AreEqual(RealmCheck.Fixed, Check(result, "global-manifest").status);
		CollectionAssert.Contains(realm.Calls, "set notification|publisher=beamable");
		CollectionAssert.Contains(realm.Calls, "publish global");
	}

	[Test]
	public async Task Fix_ReportsFailure_WhenPublisherDoesNotChange()
	{
		var realm = new FakeRealm { Provider = "pubnub", ConfigFixSticks = false };
		var result = await RealmCheck.RunWeb(realm, fix: true);

		Assert.IsFalse(result.ok);
		Assert.AreEqual(RealmCheck.Fail, Check(result, "notification-publisher").status);
		Assert.That(Check(result, "notification-publisher").message, Does.Contain("still reports provider 'pubnub'"));
	}

	[Test]
	public async Task Fix_DoesNotTouchHealthyRealm()
	{
		var realm = new FakeRealm();
		await RealmCheck.RunWeb(realm, fix: true);
		Assert.IsFalse(realm.Calls.Any(c => c.StartsWith("set") || c.StartsWith("publish")));
	}

	[Test]
	public void Services_NoneDeployed_IsSkipped()
	{
		Assert.AreEqual(RealmCheck.Skipped, RealmCheck.CheckServices(new()).status);
	}

	[Test]
	public void Services_NotReady_AreNamed()
	{
		var entry = RealmCheck.CheckServices(new() { ("A", true, true), ("B", false, true), ("C", true, false) });
		Assert.AreEqual(RealmCheck.Fail, entry.status);
		Assert.That(entry.message, Does.Contain("B (not running)").And.Contain("C (not current)").And.Not.Contain("A ("));
		Assert.AreEqual(RealmCheck.ServicesFixCommand, entry.fixCommand);
	}

	[Test]
	public async Task UnreachableSocket_Fails()
	{
		var realm = new FakeRealm { SocketProblem = "the request timed out" };
		var result = await RealmCheck.RunWeb(realm, fix: false);

		Assert.IsFalse(result.ok);
		Assert.That(Check(result, "realtime-socket").message, Does.Contain("socket.test").And.Contain("timed out"));
	}

	[Test]
	public async Task MissingSocketUri_FailsWithoutProbing()
	{
		var realm = new FakeRealm { Uri = "" };
		var result = await RealmCheck.RunWeb(realm, fix: false);

		Assert.AreEqual(RealmCheck.Fail, Check(result, "realtime-socket").status);
		Assert.IsFalse(realm.Calls.Any(c => c.StartsWith("reach")));
	}

	[TestCase("wss://socket.beamable.com", "https://socket.beamable.com/connect")]
	[TestCase("wss://socket.beamable.com/", "https://socket.beamable.com/connect")]
	[TestCase("ws://localhost:8080", "http://localhost:8080/connect")]
	public void ToHttpUrl_MapsSocketToHandshakeUrl(string ws, string http)
	{
		Assert.AreEqual(http, RealmCheck.ToHttpUrl(ws));
	}
}
