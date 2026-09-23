using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using cli;
using cli.Mcp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests.Mcp;

/// <summary>
/// `beam mcp setup` must write a command the MCP client can start, and the workspace's dotnet tool
/// manifest must pin beamable.tools even when .NET 10 put a manifest in the workspace root.
/// </summary>
public class McpSetupTests
{
	private string _dir = null!;

	[SetUp]
	public void SetUp()
	{
		_dir = Path.Combine(Path.GetTempPath(), "beam-mcp-setup-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_dir);
	}

	[TearDown]
	public void TearDown()
	{
		try { Directory.Delete(_dir, true); }
		catch (IOException) { }
	}

	private static string ExeName => OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

	private string MakeFakeDotnet(string folder)
	{
		var dir = Path.Combine(_dir, folder);
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, ExeName);
		File.WriteAllText(path, "");
		return path;
	}

	private static Func<string, string> Env(Dictionary<string, string> values) =>
		name => values.TryGetValue(name, out var v) ? v : null;

	[Test]
	public void ResolveDotnet_PrefersDotnetHostPath()
	{
		var host = MakeFakeDotnet("host");
		var root = MakeFakeDotnet("root");
		var resolved = McpSetupCommand.ResolveDotnetExecutable(null, Env(new()
		{
			["DOTNET_HOST_PATH"] = host,
			["DOTNET_ROOT"] = Path.GetDirectoryName(root)!,
		}), processPath: "/nowhere/beam");
		Assert.AreEqual(host, resolved);
	}

	[Test]
	public void ResolveDotnet_UsesDotnetRoot_WhenHostPathMissing()
	{
		var root = MakeFakeDotnet("root");
		var resolved = McpSetupCommand.ResolveDotnetExecutable(null, Env(new()
		{
			["DOTNET_HOST_PATH"] = Path.Combine(_dir, "missing", ExeName),
			["DOTNET_ROOT"] = Path.GetDirectoryName(root)!,
		}), processPath: "/nowhere/beam");
		Assert.AreEqual(root, resolved);
	}

	[Test]
	public void ResolveDotnet_UsesPath_WhenNoDotnetEnvironment()
	{
		var onPath = MakeFakeDotnet("bin");
		var resolved = McpSetupCommand.ResolveDotnetExecutable(null, Env(new()
		{
			["PATH"] = string.Join(Path.PathSeparator, Path.Combine(_dir, "empty"), Path.GetDirectoryName(onPath)),
		}), processPath: "/nowhere/beam");
		Assert.AreEqual(onPath, resolved);
	}

	[Test]
	public void ResolveDotnet_UsesCurrentProcess_WhenItIsDotnet()
	{
		var self = MakeFakeDotnet("self");
		var resolved = McpSetupCommand.ResolveDotnetExecutable(null, Env(new()), processPath: self);
		Assert.AreEqual(self, resolved);
	}

	[Test]
	public void ResolveDotnet_PrefersExplicitRootedDotnetPath()
	{
		var configured = MakeFakeDotnet("configured");
		var host = MakeFakeDotnet("host");
		var resolved = McpSetupCommand.ResolveDotnetExecutable(configured, Env(new() { ["DOTNET_HOST_PATH"] = host }));
		Assert.AreEqual(configured, resolved);
	}

	[Test]
	public void ResolveDotnet_FallsBackToPlainDotnet()
	{
		var resolved = McpSetupCommand.ResolveDotnetExecutable("dotnet", Env(new() { ["PATH"] = Path.Combine(_dir, "empty") }),
			processPath: "/nowhere/beam");
		Assert.AreEqual("dotnet", resolved);
	}

	private static JObject BeamEntry(string manifestPath) =>
		(JObject)JObject.Parse(File.ReadAllText(manifestPath))["tools"]!["beamable.tools"]!;

	[Test]
	public void EnsureManifest_CreatesConfigManifestPinningBeam()
	{
		var path = ConfigService.EnsureDotNetToolsManifest(_dir);

		Assert.AreEqual(Path.Combine(_dir, ".config", "dotnet-tools.json"), path);
		var manifest = JObject.Parse(File.ReadAllText(path));
		Assert.AreEqual(true, manifest.Value<bool>("isRoot"));
		var entry = BeamEntry(path);
		Assert.IsFalse(string.IsNullOrEmpty(entry.Value<string>("version")));
		CollectionAssert.AreEqual(new[] { "beam" }, entry["commands"]!.ToObject<string[]>());
		Assert.IsFalse(File.Exists(Path.Combine(_dir, "dotnet-tools.json")), "should not create a root manifest");
	}

	[Test]
	public void EnsureManifest_PinsBeamInRootManifestCreatedByDotnet10()
	{
		// what `dotnet new tool-manifest` writes on .NET 10
		File.WriteAllText(Path.Combine(_dir, "dotnet-tools.json"), "{\n  \"version\": 1,\n  \"isRoot\": true,\n  \"tools\": {}\n}");

		var configPath = ConfigService.EnsureDotNetToolsManifest(_dir);

		var version = BeamEntry(configPath).Value<string>("version");
		Assert.AreEqual(version, BeamEntry(Path.Combine(_dir, "dotnet-tools.json")).Value<string>("version"));
	}

	[Test]
	public void EnsureManifest_UpdatesExistingEntry_AndKeepsOtherTools()
	{
		Directory.CreateDirectory(Path.Combine(_dir, ".config"));
		var path = Path.Combine(_dir, ".config", "dotnet-tools.json");
		File.WriteAllText(path, @"{
  ""version"": 1,
  ""isRoot"": true,
  ""tools"": {
    ""dotnet-ef"": { ""version"": ""9.0.0"", ""commands"": [ ""dotnet-ef"" ] },
    ""Beamable.Tools"": { ""commands"": [ ""beam"" ], ""version"": ""1.0.0"", ""rollForward"": true }
  }
}");

		ConfigService.EnsureDotNetToolsManifest(_dir);

		var manifest = JObject.Parse(File.ReadAllText(path));
		var tools = (JObject)manifest["tools"]!;
		Assert.AreEqual("9.0.0", tools["dotnet-ef"]!.Value<string>("version"));
		Assert.IsNull(tools["beamable.tools"], "existing casing is reused instead of adding a duplicate");
		var entry = (JObject)tools["Beamable.Tools"]!;
		Assert.AreNotEqual("1.0.0", entry.Value<string>("version"));
		Assert.AreEqual(true, entry.Value<bool>("rollForward"));
		Assert.That(File.ReadAllText(path), Does.Match("(?is)beamable.tools\": \\{\\s*\"version\": \"[^\"]+\","),
			"version must stay first so the regex readers still match");
	}

	[Test]
	public void EnsureManifest_ThrowsOnInvalidJson()
	{
		Directory.CreateDirectory(Path.Combine(_dir, ".config"));
		File.WriteAllText(Path.Combine(_dir, ".config", "dotnet-tools.json"), "{ not json");
		Assert.Throws<CliException>(() => ConfigService.EnsureDotNetToolsManifest(_dir));
	}

	[TestCase("0.0.123.0", "0.0.123")]
	[TestCase("7.2.3", "7.2.3")]
	[TestCase("7.2.3.1", "7.2.3.1")]
	[TestCase("7.2.3-PREVIEW.RC1+abc", "7.2.3-PREVIEW.RC1")]
	[TestCase("07.02.3", "7.2.3")]
	public void NormalizeNuGetVersion(string input, string expected)
	{
		Assert.AreEqual(expected, ConfigService.NormalizeNuGetVersion(input));
	}

	[Test]
	public async Task VerifyLocalBeamTool_DetectsRootManifestWithoutBeam()
	{
		var dotnet = McpSetupCommand.ResolveDotnetExecutable();
		File.WriteAllText(Path.Combine(_dir, "dotnet-tools.json"), "{ \"version\": 1, \"isRoot\": true, \"tools\": {} }");

		var problem = await ConfigService.VerifyLocalBeamTool(dotnet, _dir, "7.2.3");
		Assert.That(problem, Does.Contain("does not list beamable.tools"));
	}

	[Test]
	public async Task VerifyLocalBeamTool_AcceptsPinnedVersion_AndReportsMismatch()
	{
		var dotnet = McpSetupCommand.ResolveDotnetExecutable();
		Directory.CreateDirectory(Path.Combine(_dir, ".config"));
		ConfigService.WriteBeamToolToManifest(Path.Combine(_dir, ".config", "dotnet-tools.json"), "7.2.3");

		Assert.IsNull(await ConfigService.VerifyLocalBeamTool(dotnet, _dir, "7.2.3"));
		Assert.IsNull(await ConfigService.VerifyLocalBeamTool(dotnet, _dir, "7.2.3.0"), "versions compare NuGet-normalized");
		Assert.That(await ConfigService.VerifyLocalBeamTool(dotnet, _dir, "7.2.4"), Does.Contain("resolves beamable.tools 7.2.3"));
	}
}
