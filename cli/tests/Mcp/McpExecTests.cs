using System.Threading.Tasks;
using cli.Commands.Mcp;
using NUnit.Framework;

namespace tests.Mcp;

/// <summary>
/// beam_exec should cost one call per command: it adds -q under the MCP server so nothing blocks on a
/// prompt, and it answers a parse error with the command's help instead of a "go read the docs" message.
/// </summary>
public class McpExecTests
{
	[TestCase("project list", false, "project list --emit-log-streams")]
	[TestCase("project list", true, "project list --emit-log-streams -q")]
	[TestCase("project list -q", true, "project list -q --emit-log-streams")]
	[TestCase("project list --quiet --emit-log-streams", true, "project list --quiet --emit-log-streams")]
	[TestCase("project run -- --some-arg", true, "project run --emit-log-streams -q -- --some-arg")]
	[TestCase("project run -- -q", true, "project run --emit-log-streams -q -- -q")]
	[TestCase("  config set pid \"a b\"  ", true, "config set pid \"a b\" --emit-log-streams -q")]
	public void PrepareCommandLine_AddsFlags(string input, bool inMcp, string expected)
	{
		Assert.AreEqual(expected, McpToolExecutor.PrepareCommandLine(input, inMcp));
	}

	[TestCase("project new service Foo --link-to Bar", "project new service Foo")]
	[TestCase("deploy release --merge", "deploy release")]
	[TestCase("--help", "")]
	[TestCase("config", "config")]
	public void GetCommandPath_TakesLeadingCommandWords(string input, string expected)
	{
		Assert.AreEqual(expected, McpToolExecutor.GetCommandPath(input));
	}

	[TestCase("Unrecognized command or argument '--nope'.", true)]
	[TestCase("Required argument missing for command: 'set'.", true)]
	[TestCase("Option '--name' is required.", true)]
	[TestCase("Cannot parse argument 'abc' for option '--port' as expected type 'System.Int32'.", true)]
	[TestCase("No beamable project exists. Please use beam init", false)]
	[TestCase("", false)]
	public void IsParseError_RecognizesSystemCommandLineErrors(string error, bool expected)
	{
		Assert.AreEqual(expected, McpToolExecutor.IsParseError(error));
	}

	[Test]
	public async Task ExecuteAsync_ReturnsHelp_OnInvalidArguments()
	{
		var output = await new McpToolExecutor().ExecuteAsync("mcp setup --definitely-not-an-option");

		Assert.That(output, Does.StartWith("Invalid arguments for `beam mcp setup --definitely-not-an-option`"));
		Assert.That(output, Does.Contain("--definitely-not-an-option"));
		Assert.That(output, Does.Contain("Help for `beam mcp setup`"));
		Assert.That(output, Does.Contain("--project-path"), "the help of the command itself must be included");
	}

	[Test]
	public async Task ExecuteAsync_ReturnsRootHelp_ForUnknownCommand()
	{
		var output = await new McpToolExecutor().ExecuteAsync("definitely-not-a-command");

		Assert.That(output, Does.StartWith("Invalid arguments for `beam definitely-not-a-command`"));
		Assert.That(output, Does.Contain("project"), "the root command list should be included");
	}
}
