using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.BeamCli;
using cli;
using cli.Commands.Project;
using cli.Services;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using tests.MoqExtensions;

namespace tests.Examples.Project;

/// <summary>
/// Flow coverage for `beam project call`, which calls a microservice endpoint as a player.
/// The HTTP transport (<see cref="MicroserviceHttpCaller"/>) and the auth API are mocked, so these
/// tests check what would be sent and what is reported, without a network or a running service.
/// </summary>
[NonParallelizable]
public class ProjectCallFlows : CLITest
{
	private const string Cid = "123";
	private const string Pid = "456";
	private const string Host = "https://api.beamable.com";
	private const string ServiceUrl = Host + "/basic/123.456.micro_Game/Move";
	private const string AccountsMeUrl = Host + "/basic/accounts/me";

	private class SentRequest
	{
		public HttpMethod method;
		public string url;
		public string body;
		public IReadOnlyDictionary<string, string> headers;
	}

	private List<SentRequest> _sent;
	private Mock<MicroserviceHttpCaller> _caller;
	private Mock<IDataReporterService> _reporter;
	private ProjectCallResult _result;
	private ErrorOutput _error;

	private void WriteWorkspace()
	{
		Directory.CreateDirectory(Path.Combine(".beamable", "temp"));
		File.WriteAllText(Path.Combine(".beamable", "config.beam.json"),
			$$"""
			{
			  "additionalProjectPaths" : [ ],
			  "ignoredProjectPaths" : [ ],
			  "host" : "{{Host}}",
			  "cid" : "{{Cid}}",
			  "pid" : "{{Pid}}",
			  "cliVersion" : "0.0.123"
			}
			""");
		File.WriteAllText(Path.Combine(".beamable", "temp", "auth.beam.json"),
			$$"""
			{
			  "cid" : "{{Cid}}",
			  "pid" : "{{Pid}}",
			  "access_token" : "admin-access",
			  "refresh_token" : "admin-refresh",
			  "expires_at" : "2999-01-01T00:00:00",
			  "expires_in" : 999999999,
			  "issued_at" : "2020-01-01T00:00:00"
			}
			""");
	}

	/// <summary>
	/// Mocks the transport: the service endpoint answers with <paramref name="status"/>/<paramref name="body"/>,
	/// and /basic/accounts/me answers with player id 777.
	/// </summary>
	private void MockTransport(int status, string body)
	{
		// NUnit reuses the fixture instance across tests, so clear what an earlier test captured.
		_result = null;
		_error = null;
		_sent = new List<SentRequest>();
		_caller = new Mock<MicroserviceHttpCaller>();
		_caller
			.Setup(x => x.Send(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<string>(),
				It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
			.Returns<HttpMethod, string, string, IReadOnlyDictionary<string, string>, CancellationToken>(
				(method, url, reqBody, headers, _) =>
				{
					_sent.Add(new SentRequest { method = method, url = url, body = reqBody, headers = headers });
					var response = url == AccountsMeUrl
						? new MicroserviceHttpResponse { status = 200, body = "{\"id\":777}" }
						: new MicroserviceHttpResponse { status = status, body = body };
					return Task.FromResult(response);
				});

		_reporter = new Mock<IDataReporterService>();
		_reporter
			.Setup(x => x.Report(It.IsAny<string>(), It.IsAny<ProjectCallResult>()))
			.Callback<string, ProjectCallResult>((_, r) => _result = r);
		_reporter
			.Setup(x => x.Report(DefaultErrorStream.CHANNEL, It.IsAny<ErrorOutput>()))
			.Callback<string, ErrorOutput>((_, e) => _error = e);
	}

	private int RunCall(params string[] args)
	{
		// -q skips the first-run telemetry consent prompt, which the test console cannot answer.
		return RunFull(args.Append("-q").ToArray(), configurator: builder =>
		{
			builder.ReplaceSingleton<MicroserviceHttpCaller, MicroserviceHttpCaller>(() => _caller.Object);
			builder.ReplaceSingleton<IDataReporterService>(_reporter.Object);
		});
	}

	private SentRequest ServiceRequest => _sent.Single(r => r.url == ServiceUrl);

	[Test]
	public void Guest_PostsPayloadToServiceRoute_AndReportsResult()
	{
		WriteWorkspace();
		MockTransport(200, "{\"moved\":true}");
		_mockAuth.Setup(x => x.CreateUser())
			.ReturnsPromise(new TokenResponse { access_token = "guest-access", refresh_token = "guest-refresh" });

		var exitCode = RunCall("project", "call", "Game", "Move", "--payload", "{\"x\":1,\"y\":2}", "--target", "remote");

		Assert.That(exitCode, Is.EqualTo(0));
		_mockAuth.Verify(x => x.CreateUser(), Times.Once);

		var request = ServiceRequest;
		Assert.That(request.method, Is.EqualTo(HttpMethod.Post));
		Assert.That(JToken.DeepEquals(JToken.Parse(request.body), JObject.Parse("{\"x\":1,\"y\":2}")), Is.True,
			$"payload was not sent as the body: {request.body}");
		Assert.That(request.headers["Authorization"], Is.EqualTo("Bearer guest-access"));
		Assert.That(request.headers["X-BEAM-SCOPE"], Is.EqualTo("123.456"));
		Assert.That(request.headers.ContainsKey(Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY), Is.False,
			"a remote call must not carry a routing key");

		Assert.That(_result, Is.Not.Null, "the result was not reported");
		Assert.That(_result.service, Is.EqualTo("Game"));
		Assert.That(_result.method, Is.EqualTo("Move"));
		Assert.That(_result.status, Is.EqualTo(200));
		Assert.That(_result.success, Is.True);
		Assert.That(_result.body, Is.EqualTo("{\"moved\":true}"));
		Assert.That(_result.target, Is.EqualTo("remote"));
		Assert.That(_result.routingKey, Is.EqualTo(""));
		Assert.That(_result.url, Is.EqualTo(ServiceUrl));
		Assert.That(_result.createdGuest, Is.True);
		Assert.That(_result.refreshToken, Is.EqualTo("guest-refresh"));
		Assert.That(_result.playerId, Is.EqualTo(777));
	}

	[Test]
	public void RefreshToken_LocalTarget_SendsRoutingKey_AndReusesPlayer()
	{
		WriteWorkspace();
		MockTransport(200, "{}");

		// CLITest's default LoginRefreshToken mock mints access_token = "access".
		var exitCode = RunCall("project", "call", "Game", "Move", "--as", "player-refresh", "--target", "local");

		Assert.That(exitCode, Is.EqualTo(0));
		_mockAuth.Verify(x => x.LoginRefreshToken("player-refresh"), Times.Once);
		_mockAuth.Verify(x => x.CreateUser(), Times.Never);

		var request = ServiceRequest;
		Assert.That(request.body, Is.EqualTo("{}"), "no --payload sends an empty object");
		Assert.That(request.headers["Authorization"], Is.EqualTo("Bearer access"));
		var machineKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine();
		Assert.That(request.headers[Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY],
			Is.EqualTo($"micro_Game:{machineKey}"));

		Assert.That(_result.target, Is.EqualTo("local"));
		Assert.That(_result.routingKey, Is.EqualTo(machineKey));
		Assert.That(_result.createdGuest, Is.False);
		Assert.That(_result.refreshToken, Is.EqualTo("player-refresh"));
	}

	[Test]
	public void NonSuccessStatus_ReportsBody_AndExitsNonZero()
	{
		WriteWorkspace();
		MockTransport(400, "{\"error\":\"NotYourTurn\"}");
		_mockAuth.Setup(x => x.CreateUser())
			.ReturnsPromise(new TokenResponse { access_token = "guest-access", refresh_token = "guest-refresh" });

		var exitCode = RunCall("project", "call", "Game", "Move", "--target", "remote");

		Assert.That(exitCode, Is.Not.EqualTo(0), "a non-2xx response must fail the command");
		Assert.That(_result, Is.Not.Null, "the result must still be reported on failure");
		Assert.That(_result.status, Is.EqualTo(400));
		Assert.That(_result.success, Is.False);
		Assert.That(_result.body, Is.EqualTo("{\"error\":\"NotYourTurn\"}"));
		Assert.That(_result.refreshToken, Is.EqualTo("guest-refresh"), "a follow-up call can still reuse the guest");
	}

	[TestCase("{not json")]
	[TestCase("[1,2]")]
	public void InvalidPayload_FailsBeforeAnyRequest(string payload)
	{
		WriteWorkspace();
		MockTransport(200, "{}");

		var exitCode = RunCall("project", "call", "Game", "Move", "--payload", payload, "--target", "remote", "--raw");

		Assert.That(exitCode, Is.Not.EqualTo(0));
		Assert.That(_error, Is.Not.Null, "the error was not reported");
		Assert.That(_error.message, Does.Contain("--payload"));
		Assert.That(_sent, Is.Empty, "nothing should be sent for an invalid payload");
		_mockAuth.Verify(x => x.CreateUser(), Times.Never, "no guest should be created for an invalid payload");
		Assert.That(_result, Is.Null);
	}

	[Test]
	public void BuildServicePath_MatchesWebSdkRoute()
	{
		Assert.That(ProjectCallCommand.BuildServicePath("123", "456", "Game", "Move"),
			Is.EqualTo("/basic/123.456.micro_Game/Move"));
		Assert.That(ProjectCallCommand.BuildServicePath("123", "456", "Game", "/Move"),
			Is.EqualTo("/basic/123.456.micro_Game/Move"));
	}

	[Test]
	public void BuildHeaders_OnlyIncludesRoutingKeyWhenGiven()
	{
		var remote = ProjectCallCommand.BuildHeaders("123", "456", "tok", "Game", null);
		Assert.That(remote["X-BEAM-SCOPE"], Is.EqualTo("123.456"));
		Assert.That(remote["Authorization"], Is.EqualTo("Bearer tok"));
		Assert.That(remote.ContainsKey(Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY), Is.False);

		var local = ProjectCallCommand.BuildHeaders("123", "456", "tok", "Game", "machine_abc");
		Assert.That(local[Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY], Is.EqualTo("micro_Game:machine_abc"));
	}

	[Test]
	public void ValidatePayload_DefaultsToEmptyObject_AndKeepsOriginalText()
	{
		Assert.That(ProjectCallCommand.ValidatePayload(null), Is.EqualTo("{}"));
		Assert.That(ProjectCallCommand.ValidatePayload("  "), Is.EqualTo("{}"));

		// sent as written, so dates and large numbers are not re-serialized
		const string payload = "{\"when\":\"2026-01-01T00:00:00Z\",\"big\":9223372036854775807}";
		Assert.That(ProjectCallCommand.ValidatePayload(payload), Is.EqualTo(payload));

		Assert.Throws<CliException>(() => ProjectCallCommand.ValidatePayload("{oops"));
		Assert.Throws<CliException>(() => ProjectCallCommand.ValidatePayload("42"));
	}
}
