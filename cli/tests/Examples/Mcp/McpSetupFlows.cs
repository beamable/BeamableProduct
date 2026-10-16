using System.Diagnostics;
using System.IO;
using cli;
using cli.Mcp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests.Examples.Mcp;

public class McpSetupFlows : CLITest
{
	private static JObject ReadMcpJson(string dir) => JObject.Parse(File.ReadAllText(Path.Combine(dir, ".mcp.json")));

	[Test]
	public void Setup_WritesResolvableDotnetCommand()
	{
		Run("mcp", "setup", "--project-path", WorkingDir, "-q");

		var server = (JObject)ReadMcpJson(WorkingDir)["mcpServers"]!["beamable"]!;
		var command = server.Value<string>("command")!;
		Assert.That(Path.IsPathRooted(command), $"expected an absolute dotnet path, got '{command}'");
		Assert.That(File.Exists(command), $"'{command}' does not exist");
		CollectionAssert.AreEqual(McpSetupCommand.ServeArgs, server["args"]!.ToObject<string[]>());

		var manifest = JObject.Parse(File.ReadAllText(Path.Combine(WorkingDir, ".config", "dotnet-tools.json")));
		Assert.IsNotNull(manifest["tools"]?["beamable.tools"]?["version"], ".config/dotnet-tools.json must pin beamable.tools");
	}

	[Test]
	public void Setup_KeepsOtherServers()
	{
		File.WriteAllText(Path.Combine(WorkingDir, ".mcp.json"), "{ \"mcpServers\": { \"other\": { \"command\": \"node\" } } }");

		Run("mcp", "setup", "--project-path", WorkingDir, "-q");

		var servers = (JObject)ReadMcpJson(WorkingDir)["mcpServers"]!;
		Assert.AreEqual("node", servers["other"]!.Value<string>("command"));
		Assert.IsNotNull(servers["beamable"]);
	}

	[Test]
	public void Setup_PinsBeamWhenDotnet10CreatedRootManifest()
	{
		var dotnet = McpSetupCommand.ResolveDotnetExecutable();
		// On .NET 10 this writes ./dotnet-tools.json with an empty "tools" object.
		var create = Process.Start(new ProcessStartInfo(dotnet, "new tool-manifest --force")
		{
			WorkingDirectory = WorkingDir, RedirectStandardOutput = true, RedirectStandardError = true,
		})!;
		create.WaitForExit();
		Assert.AreEqual(0, create.ExitCode, "dotnet new tool-manifest failed");

		Run("mcp", "setup", "--project-path", WorkingDir, "-q");

		foreach (var manifestPath in new[] { Path.Combine(WorkingDir, ".config", "dotnet-tools.json"), Path.Combine(WorkingDir, "dotnet-tools.json") })
		{
			if (!File.Exists(manifestPath)) continue;
			var manifest = JObject.Parse(File.ReadAllText(manifestPath));
			Assert.IsNotNull(manifest["tools"]?["beamable.tools"]?["version"], $"{manifestPath} must pin beamable.tools");
		}

		var expectedVersion = JObject.Parse(File.ReadAllText(Path.Combine(WorkingDir, ".config", "dotnet-tools.json")))["tools"]!["beamable.tools"]!.Value<string>("version")!;
		var problem = ConfigService.VerifyLocalBeamTool(dotnet, WorkingDir, expectedVersion).GetAwaiter().GetResult();
		Assert.IsNull(problem, problem);
	}
}
