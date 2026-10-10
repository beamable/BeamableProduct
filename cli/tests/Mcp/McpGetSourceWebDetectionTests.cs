using System;
using System.IO;
using cli.Commands.Mcp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests.Mcp;

/// <summary>
/// Covers how beam_get_source finds the web SDK version: subdirectories of a workspace root,
/// devDependencies, installed node_modules versions winning over ranges, never borrowing another
/// platform's version, and saying so instead of inventing a version when nothing is found.
/// </summary>
public class McpGetSourceWebDetectionTests
{
	private string _root = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "beam-mcp-web-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);
	}

	[TearDown]
	public void TearDown()
	{
		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// best effort
		}
	}

	private void WriteFile(string relativePath, string content)
	{
		var full = Path.Combine(_root, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllText(full, content);
	}

	private void WriteCliWorkspace(string cliVersion)
	{
		WriteFile(Path.Combine(".config", "dotnet-tools.json"),
			"{\"version\":1,\"isRoot\":true,\"tools\":{\"beamable.tools\":{\"version\":\"" + cliVersion + "\",\"commands\":[\"beam\"]}}}");
		Directory.CreateDirectory(Path.Combine(_root, ".beamable"));
	}

	private JObject GetSource(string platform = "", string version = "", string filePath = "")
	{
		var json = new McpToolExecutor().GetSourceCode(platform, version, filePath, startDirectory: _root).Result;
		return JObject.Parse(json);
	}

	[Test]
	public void Detects_dependency_in_web_subdirectory_of_workspace_root()
	{
		WriteFile(Path.Combine("web", "package.json"), "{\"dependencies\":{\"@beamable/sdk\":\"1.2.1\"}}");

		var detection = McpToolExecutor.DetectWebSdk(_root);

		Assert.That(detection, Is.Not.Null);
		Assert.That(detection!.Version, Is.EqualTo("1.2.1"));
		Assert.That(detection.IsRange, Is.False);
		Assert.That(detection.SourceFile, Is.EqualTo(Path.Combine(_root, "web", "package.json")));
	}

	[Test]
	public void Detects_dev_dependency_two_levels_deep_and_strips_range_operator()
	{
		WriteFile(Path.Combine("apps", "portal", "package.json"), "{\"devDependencies\":{\"@beamable/sdk\":\"^1.2.1\"}}");

		var detection = McpToolExecutor.DetectWebSdk(_root);

		Assert.That(detection, Is.Not.Null);
		Assert.That(detection!.Version, Is.EqualTo("1.2.1"));
		Assert.That(detection.IsRange, Is.True);
		Assert.That(detection.Range, Is.EqualTo("^1.2.1"));
	}

	[Test]
	public void Does_not_scan_node_modules_or_hidden_directories_for_package_json()
	{
		WriteFile(Path.Combine("node_modules", "some-lib", "package.json"), "{\"dependencies\":{\"@beamable/sdk\":\"9.9.9\"}}");
		WriteFile(Path.Combine(".cache", "package.json"), "{\"dependencies\":{\"@beamable/sdk\":\"8.8.8\"}}");

		Assert.That(McpToolExecutor.DetectWebSdk(_root), Is.Null);
	}

	[Test]
	public void Installed_node_modules_version_is_preferred_over_declared_range()
	{
		WriteFile(Path.Combine("web", "package.json"), "{\"dependencies\":{\"@beamable/sdk\":\"^1.2.1\"}}");
		WriteFile(Path.Combine("web", "node_modules", "@beamable", "sdk", "package.json"),
			"{\"name\":\"@beamable/sdk\",\"version\":\"1.3.0\"}");

		var detection = McpToolExecutor.DetectWebSdk(_root);
		Assert.That(detection, Is.Not.Null);
		Assert.That(detection!.Version, Is.EqualTo("1.3.0"));
		Assert.That(detection.IsRange, Is.False);
		Assert.That(detection.LocalSdkDir, Is.EqualTo(Path.Combine(_root, "web", "node_modules", "@beamable", "sdk")));

		var response = GetSource(platform: "web");
		Assert.That(response["detectedVersion"]?.ToString(), Is.EqualTo("1.3.0"));
		Assert.That(response["sourcePath"]?.ToString(), Is.EqualTo(detection.LocalSdkDir));
		Assert.That(response["versionRange"], Is.Null);
	}

	[Test]
	public void Web_request_in_cli_workspace_does_not_use_cli_version()
	{
		WriteCliWorkspace("7.2.3");
		WriteFile(Path.Combine("web", "package.json"), "{\"devDependencies\":{\"@beamable/sdk\":\"~1.2.0\"}}");

		var response = GetSource(platform: "web");

		Assert.That(response["error"], Is.Null, response.ToString());
		Assert.That(response["platform"]?.ToString(), Is.EqualTo("web"));
		Assert.That(response["detectedVersion"]?.ToString(), Is.EqualTo("1.2.0"));
		Assert.That(response["versionRange"]?.ToString(), Is.EqualTo("~1.2.0"));
		Assert.That(response["sourcePath"]?.ToString(),
			Is.EqualTo("https://github.com/beamable/BeamableProduct/tree/web-sdk-1.2.0"));
		Assert.That(response.ToString(), Does.Not.Contain("7.2.3"));
	}

	[Test]
	public void Web_request_in_cli_workspace_without_web_sdk_reports_not_found()
	{
		WriteCliWorkspace("7.2.3");

		var response = GetSource(platform: "web");

		Assert.That(response["error"]?.ToString(), Does.Contain("Could not detect the Beamable web SDK version"));
		Assert.That(response["detectedVersion"]?.Type, Is.EqualTo(JTokenType.Null));
		Assert.That(response["sourcePath"], Is.Null);
		Assert.That(response.ToString(), Does.Not.Contain("7.2.3"));
		Assert.That(response.ToString(), Does.Not.Contain("web-sdk-"));
	}

	[Test]
	public void Nothing_found_reports_not_found_without_inventing_a_version()
	{
		Assert.That(McpToolExecutor.DetectWebSdk(_root), Is.Null);

		var response = GetSource(platform: "web");

		Assert.That(response["error"]?.ToString(), Does.Contain("Could not detect the Beamable web SDK version"));
		Assert.That(response["detectedVersion"]?.Type, Is.EqualTo(JTokenType.Null));
		Assert.That(response["searchedFrom"]?.ToString(), Is.EqualTo(_root));
		Assert.That(response.ToString(), Does.Not.Contain("github.com"));
	}

	[Test]
	public void Compound_range_is_reported_as_range_without_a_version()
	{
		WriteFile(Path.Combine("web", "package.json"), "{\"dependencies\":{\"@beamable/sdk\":\">=1.0.0 <2.0.0\"}}");

		var response = GetSource(platform: "web");

		Assert.That(response["error"]?.ToString(), Does.Contain("Could not detect the Beamable web SDK version"));
		Assert.That(response["versionRange"]?.ToString(), Is.EqualTo(">=1.0.0 <2.0.0"));
		Assert.That(response.ToString(), Does.Not.Contain("github.com"));
	}

	[Test]
	public void Explicit_web_version_links_web_sdk_tag()
	{
		WriteCliWorkspace("7.2.3");

		var response = GetSource(platform: "web", version: "1.2.1");

		Assert.That(response["detectedVersion"]?.ToString(), Is.EqualTo("1.2.1"));
		Assert.That(response["sourcePath"]?.ToString(),
			Is.EqualTo("https://github.com/beamable/BeamableProduct/tree/web-sdk-1.2.1"));
	}

	[TestCase("1.2.1", "1.2.1")]
	[TestCase("^1.2.1", "1.2.1")]
	[TestCase("~1.3.0-rc.2", "1.3.0-rc.2")]
	[TestCase(">=1.0.0", "1.0.0")]
	[TestCase("v1.2.0", "1.2.0")]
	[TestCase(">=1.0.0 <2.0.0", null)]
	[TestCase("1.x", null)]
	[TestCase("latest", null)]
	[TestCase("workspace:*", null)]
	[TestCase("file:../sdk", null)]
	public void VersionFromRange_strips_simple_range_operators_only(string spec, string? expected)
	{
		Assert.That(McpToolExecutor.VersionFromRange(spec), Is.EqualTo(expected));
	}
}
