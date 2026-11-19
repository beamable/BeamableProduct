using Beamable.Server;
using cli;
using cli.Services;
using cli.Services.PortalExtension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tests.PortalExtensionTests;

/// <summary>
/// Covers how the CLI reads portal extension package.json files and carries them into metadata.json:
/// a flagged extension that fails to parse is logged instead of vanishing, a package.json that isn't an
/// extension is skipped quietly, fields the model doesn't declare pass through, and args keep their JSON types.
/// </summary>
[NonParallelizable]
public class PortalExtensionDiscoveryTests
{
	private string _root;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "pe-discovery-tests", Path.GetRandomFileName());
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, true);
		}
	}

	private string WritePackage(string folder, string json)
	{
		var dir = Path.Combine(_root, folder);
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, "package.json");
		File.WriteAllText(path, json);
		return path;
	}

	[Test]
	public void FlaggedExtension_WithABadValue_IsLoggedWithFileAndPath()
	{
		var path = WritePackage("bad", @"{ ""name"": ""bad-ext"", ""beamable"": { ""portalExtension"": true,
			""mounts"": [ { ""page"": ""x"", ""siteIds"": [ ""extension-page"" ], ""navGroupOrder"": null } ] } }");
		using var capture = new LogCapture();

		var info = BeamoLocalSystem.TryReadPortalExtensionPackage(path);

		Assert.That(info, Is.Null);
		var warning = capture.Warnings.SingleOrDefault();
		Assert.That(warning, Is.Not.Null, "a flagged extension that fails to parse must be logged");
		Assert.That(warning, Does.Contain(path));
		Assert.That(warning, Does.Contain("navGroupOrder"));
	}

	[Test]
	public void FlaggedExtension_WithArgsThatAreNotAnObject_IsLogged()
	{
		var path = WritePackage("bad-args", @"{ ""name"": ""bad-args"", ""beamable"": { ""portalExtension"": true,
			""mounts"": [ { ""page"": ""x"", ""siteIds"": [ ""extension-page"" ], ""args"": [ 1 ] } ] } }");
		using var capture = new LogCapture();

		Assert.That(BeamoLocalSystem.TryReadPortalExtensionPackage(path), Is.Null);
		Assert.That(capture.Warnings, Has.Count.EqualTo(1));
		Assert.That(capture.Warnings[0], Does.Contain(path).And.Contain("args"));
	}

	[TestCase(@"{ ""name"": ""portal"", ""scripts"": { ""dev"": ""vite"" } }", TestName = "NoBeamableBlock")]
	[TestCase(@"{ ""name"": ""other"", ""beamable"": { ""portalExtension"": false, ""mounts"": 3 } }", TestName = "NotFlagged")]
	[TestCase(@"{ this is not json", TestName = "InvalidJson")]
	public void PackageThatIsNotAnExtension_IsSkippedWithNoWarning(string json)
	{
		var path = WritePackage("plain", json);
		using var capture = new LogCapture();

		Assert.That(BeamoLocalSystem.TryReadPortalExtensionPackage(path), Is.Null);
		Assert.That(capture.Warnings, Is.Empty);
	}

	[Test]
	public void FindPortalExtensionProjects_KeepsGoodExtensions_AndLogsOnlyTheBadOne()
	{
		var good = WritePackage("good", @"{ ""name"": ""good-ext"", ""beamable"": { ""portalExtension"": true, ""mounts"": [] } }");
		var bad = WritePackage("bad", @"{ ""name"": ""bad-ext"", ""beamable"": { ""portalExtension"": true, ""mounts"": [ { ""args"": ""x"" } ] } }");
		var plain = WritePackage("plain", @"{ ""name"": ""root"" }");
		using var capture = new LogCapture();

		var found = ProjectContextUtil.FindPortalExtensionProjects(_root, new[] { good, bad, plain });

		Assert.That(found.Select(p => p.Name), Is.EqualTo(new[] { "good-ext" }));
		Assert.That(capture.Warnings, Has.Count.EqualTo(1));
		Assert.That(capture.Warnings[0], Does.Contain(bad));
	}

	[Test]
	public void UnknownFields_AndArgsTypes_ReachMetadataJsonUnchanged()
	{
		var path = WritePackage("ext", @"{ ""name"": ""offer-ext"", ""beamable"": {
			""portalExtension"": true,
			""sites"": [ { ""id"": ""campaign-offer"", ""presentation"": ""stack"", ""input"": { ""type"": ""object"" } } ],
			""mounts"": [
				{ ""siteIds"": [ ""campaign-offer"" ],
				  ""args"": { ""federationId"": ""beamable_reward"", ""offerContractVersion"": 2, ""nested"": { ""on"": true } },
				  ""static"": { ""federationId"": ""beamable_reward"" } },
				{ ""page"": ""no-args"", ""siteIds"": [ ""extension-page"" ] }
			] } }");

		var def = ProjectContextUtil.FindPortalExtensionProjects(_root, new[] { path }).Single();
		var observer = new PortalExtensionObserver { ExtensionMetaData = def };
		observer.CreateMetaDataFile();

		var properties = (JObject)JObject.Parse(File.ReadAllText(observer.MetadataPath))["Properties"];
		Assert.That(properties["sites"]?[0]?["id"]?.ToString(), Is.EqualTo("campaign-offer"));
		Assert.That(properties["sites"]?[0]?["input"]?["type"]?.ToString(), Is.EqualTo("object"), "unknown site field");

		var mount = (JObject)properties["mounts"][0];
		Assert.That(mount["siteIds"]?[0]?.ToString(), Is.EqualTo("campaign-offer"));
		Assert.That(mount["static"]?["federationId"]?.ToString(), Is.EqualTo("beamable_reward"), "unknown mount field");
		Assert.That(mount["args"]["offerContractVersion"].Type, Is.EqualTo(JTokenType.Integer), "a number stays a number");
		Assert.That((int)mount["args"]["offerContractVersion"], Is.EqualTo(2));
		Assert.That(mount["args"]["nested"]["on"].Type, Is.EqualTo(JTokenType.Boolean));

		var noArgs = (JObject)properties["mounts"][1];
		Assert.That(noArgs["args"], Is.InstanceOf<JObject>(), "an absent args is still written as {}");
		Assert.That(((JObject)noArgs["args"]).Count, Is.EqualTo(0));
	}

	/// <summary>Routes <see cref="Log"/> to an in-memory list for the duration of a test.</summary>
	private sealed class LogCapture : ILogger, IDisposable
	{
		private readonly ILogger _previous = BeamableZLoggerProvider.LogContext.Value;
		private readonly List<(LogLevel level, string message)> _entries = new();

		public LogCapture() => BeamableZLoggerProvider.LogContext.Value = this;

		public List<string> Warnings => _entries.Where(e => e.level == LogLevel.Warning).Select(e => e.message).ToList();

		public void Dispose() => BeamableZLoggerProvider.LogContext.Value = _previous;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			=> _entries.Add((logLevel, formatter(state, exception)));

		public bool IsEnabled(LogLevel logLevel) => true;

		public IDisposable BeginScope<TState>(TState state) => null;
	}
}
