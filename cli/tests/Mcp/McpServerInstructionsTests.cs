using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using cli.Mcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NUnit.Framework;

namespace tests.Mcp;

/// <summary>
/// The MCP server advertises instructions during initialize so an agent learns the facts it would
/// otherwise have to discover the hard way (package name, realm prerequisites, `--replace` default).
/// </summary>
public class McpServerInstructionsTests
{
	[Test]
	public void EmbeddedInstructions_ContainKeyFacts()
	{
		var instructions = McpServerBuilder.GetServerInstructions();

		Assert.That(instructions, Is.Not.Empty, $"missing embedded resource {McpServerBuilder.InstructionsResourceName}");
		Assert.That(instructions, Does.Contain("@beamable/sdk"));
		Assert.That(instructions, Does.Contain("notification|publisher"));
		Assert.That(instructions, Does.Contain("beam_get_skill"));
		Assert.That(instructions, Does.Contain("--merge"));
		Assert.That(instructions.Split('\n').Length, Is.LessThanOrEqualTo(40), "keep the instructions short");
	}

	[Test]
	public async Task Server_AdvertisesInstructions_OnInitialize()
	{
		var clientToServer = new Pipe();
		var serverToClient = new Pipe();
		using var cts = new CancellationTokenSource(System.TimeSpan.FromSeconds(30));

		var options = new McpServerOptions();
		McpServerBuilder.ConfigureServerOptions(options);

		await using var server = McpServer.Create(
			new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()),
			options);
		var serverTask = server.RunAsync(cts.Token);

		await using (var client = await McpClient.CreateAsync(
			             new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
			             cancellationToken: cts.Token))
		{
			Assert.That(client.ServerInstructions, Is.Not.Null.And.Not.Empty);
			Assert.That(client.ServerInstructions, Does.Contain("@beamable/sdk"));
			Assert.That(client.ServerInstructions, Does.Contain("notification|publisher"));
		}

		cts.Cancel();
		try { await serverTask; }
		catch (System.OperationCanceledException) { }
	}
}
