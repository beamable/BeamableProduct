using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Commands.Project;

public class ProjectCallCommandArgs : CommandArgs
{
	public string service;
	public string method;
	public string payload;
	public string asPlayer;
	public string target;

	/// <summary>
	/// Set by <see cref="ProjectCallCommand.GetResult"/> when the service answered with a non-2xx status,
	/// so the command can fail after the result has been reported.
	/// </summary>
	public ProjectCallResult failedResult;
}

[CliContractType, Serializable]
public class ProjectCallResult
{
	/// <summary>The beamo id of the service that was called.</summary>
	public string service;

	/// <summary>The endpoint (method) that was called.</summary>
	public string method;

	/// <summary>"local" when the call was routed to a service running on this machine, "remote" for the deployed service.</summary>
	public string target;

	/// <summary>The routing key sent with the request, or empty when calling the deployed service.</summary>
	public string routingKey;

	/// <summary>The full URL that was called.</summary>
	public string url;

	/// <summary>The HTTP status code of the response.</summary>
	public int status;

	/// <summary>True when <see cref="status"/> is 2xx.</summary>
	public bool success;

	/// <summary>The raw response body, usually JSON.</summary>
	public string body;

	/// <summary>The id (gamer tag) of the player that made the call, or 0 if it could not be read.</summary>
	public long playerId;

	/// <summary>True when a new guest player was created for this call.</summary>
	public bool createdGuest;

	/// <summary>The caller's refresh token. Pass it as --as to make the next call as the same player.</summary>
	public string refreshToken;
}

public class ProjectCallCommand : AtomicCommand<ProjectCallCommandArgs, ProjectCallResult>, ISkipManifest
{
	public const string AS_GUEST = "guest";
	public const string TARGET_AUTO = "auto";
	public const string TARGET_LOCAL = "local";
	public const string TARGET_REMOTE = "remote";

	/// <summary>
	/// How long auto targeting listens for a locally running instance of the service.
	/// </summary>
	public static readonly TimeSpan LocalDiscoveryPeriod = TimeSpan.FromSeconds(1);

	public ProjectCallCommand() : base("call",
		"Call a microservice endpoint as a player and return the HTTP status and response body")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("service", "The beamo id of the microservice to call"),
			(args, i) => args.service = i);
		AddArgument(new Argument<string>("method", "The endpoint to call, usually the name of a [ClientCallable] method"),
			(args, i) => args.method = i);

		AddOption(new Option<string>("--payload",
				"A JSON object whose keys are the endpoint's parameter names. Defaults to {}"),
			(args, i) => args.payload = i);

		var asOption = new Option<string>("--as", () => AS_GUEST,
			"Who makes the call: 'guest' creates a new guest player, anything else is used as a player refresh token (e.g. the refreshToken of an earlier call)");
		asOption.ArgumentHelpName = "guest|refresh-token";
		AddOption(asOption, (args, i) => args.asPlayer = i);

		var targetOption = new Option<string>("--target", () => TARGET_AUTO,
			"Where to route the call: 'local' uses this machine's routing key, 'remote' calls the deployed service, 'auto' calls a locally running instance when one is discovered and the deployed service otherwise");
		targetOption.FromAmong(TARGET_AUTO, TARGET_LOCAL, TARGET_REMOTE);
		AddOption(targetOption, (args, i) => args.target = i);
	}

	public override async Task Handle(ProjectCallCommandArgs args)
	{
		await base.Handle(args);
		if (args.failedResult != null)
		{
			throw new CliException(
				$"{args.failedResult.service}/{args.failedResult.method} returned HTTP {args.failedResult.status}: {args.failedResult.body}");
		}
	}

	public override async Task<ProjectCallResult> GetResult(ProjectCallCommandArgs args)
	{
		// validate everything before creating a guest, so a typo does not leave an orphan player behind.
		var payload = ValidatePayload(args.payload);
		if (string.IsNullOrWhiteSpace(args.service))
		{
			throw new CliException("A service name is required");
		}
		if (string.IsNullOrWhiteSpace(args.method))
		{
			throw new CliException("A method name is required");
		}

		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(pid))
		{
			throw new CliException("No realm is selected. Run `beam init` or `beam config realm use <name>` first");
		}

		var routingKey = await ResolveRoutingKey(args);
		var (token, createdGuest, refreshToken) = await GetPlayerToken(args);

		var caller = args.Provider.GetService<MicroserviceHttpCaller>();
		var host = args.AppContext.Host.TrimEnd('/');
		var url = host + BuildServicePath(cid, pid, args.service, args.method);
		var headers = BuildHeaders(cid, pid, token.access_token, args.service, routingKey);

		Log.Debug($"calling url=[{url}] routing-key=[{routingKey}]");
		var response = await caller.Send(HttpMethod.Post, url, payload, headers, args.Lifecycle.CancellationToken);

		var result = new ProjectCallResult
		{
			service = args.service,
			method = args.method,
			target = string.IsNullOrEmpty(routingKey) ? TARGET_REMOTE : TARGET_LOCAL,
			routingKey = routingKey ?? "",
			url = url,
			status = response.status,
			success = response.status is >= 200 and < 300,
			body = response.body ?? "",
			playerId = await GetPlayerId(caller, host, cid, pid, token.access_token, args.Lifecycle.CancellationToken),
			createdGuest = createdGuest,
			refreshToken = refreshToken,
		};

		if (!result.success)
		{
			args.failedResult = result;
		}

		return result;
	}

	/// <summary>
	/// The gateway path for a microservice endpoint, matching the web SDK's <c>makeMicroServiceRequest</c>.
	/// </summary>
	public static string BuildServicePath(string cid, string pid, string service, string method)
	{
		return $"/basic/{cid}.{pid}.{ServiceRoutingStrategyExtensions.BeamoIdsToServiceNames(service)}/{method.TrimStart('/')}";
	}

	/// <summary>
	/// The value of the routing key header that sends a call for <paramref name="service"/> to the
	/// instance registered with <paramref name="routingKey"/>, e.g. <c>micro_Game:mymachine_abc</c>.
	/// </summary>
	public static string BuildRoutingHeader(string service, string routingKey)
	{
		return $"{ServiceRoutingStrategyExtensions.BeamoIdsToServiceNames(service)}:{routingKey}";
	}

	public static Dictionary<string, string> BuildHeaders(string cid, string pid, string accessToken, string service, string routingKey)
	{
		var headers = new Dictionary<string, string>
		{
			["X-BEAM-SCOPE"] = $"{cid}.{pid}",
			["Authorization"] = $"Bearer {accessToken}",
		};
		if (!string.IsNullOrEmpty(routingKey))
		{
			headers[Beamable.Common.Constants.Requester.HEADER_ROUTINGKEY] = BuildRoutingHeader(service, routingKey);
		}

		return headers;
	}

	/// <summary>
	/// Returns the payload to send, or throws a <see cref="CliException"/> when it is not a JSON object.
	/// The original text is sent (not a re-serialization) so values like dates and large numbers are untouched.
	/// </summary>
	public static string ValidatePayload(string payload)
	{
		if (string.IsNullOrWhiteSpace(payload))
		{
			return "{}";
		}

		JToken parsed;
		try
		{
			parsed = JToken.Parse(payload);
		}
		catch (JsonReaderException ex)
		{
			throw new CliException(
				$"--payload is not valid JSON ({ex.Message}). Pass a JSON object whose keys are the endpoint's parameter names, e.g. --payload '{{\"amount\":5}}'");
		}

		if (parsed.Type != JTokenType.Object)
		{
			throw new CliException(
				$"--payload must be a JSON object whose keys are the endpoint's parameter names, but got a JSON {parsed.Type.ToString().ToLowerInvariant()}");
		}

		return payload.Trim();
	}

	/// <returns>The routing key to send, or null to call the deployed service.</returns>
	private static async Task<string> ResolveRoutingKey(ProjectCallCommandArgs args)
	{
		switch (args.target)
		{
			case TARGET_REMOTE:
				return null;
			case TARGET_LOCAL:
				return ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine();
			default:
				var discovered = await DiscoverLocalRoutingKey(args);
				if (discovered == null)
				{
					Log.Debug($"no local instance of service=[{args.service}] was discovered, calling the deployed service");
				}
				return discovered;
		}
	}

	/// <summary>
	/// Listens briefly for a locally running (host process or docker) instance of the service and
	/// returns its routing key, preferring this machine's default key. Null when none is running.
	/// </summary>
	private static async Task<string> DiscoverLocalRoutingKey(ProjectCallCommandArgs args)
	{
		// this command skips the manifest for speed; discovery needs it.
		await args.BeamoLocalSystem.InitManifest();

		var machineKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine();
		string found = null;
		await foreach (var status in CheckStatusCommand.CheckStatus(args, LocalDiscoveryPeriod, DiscoveryMode.LOCAL,
			               new List<string> { args.service }, args.Lifecycle.CancellationToken))
		{
			if (!status.TryGetStatus(args.service, out var service))
			{
				continue;
			}
			foreach (var route in service.availableRoutes)
			{
				var isLocal = route.instances.Any(i => i.latestHostEvent != null || i.latestDockerEvent != null);
				if (!route.knownToBeRunning || !isLocal)
				{
					continue;
				}
				if (found == null || route.routingKey == machineKey)
				{
					found = route.routingKey;
				}
			}
		}

		return found;
	}

	private static async Task<(TokenResponse token, bool createdGuest, string refreshToken)> GetPlayerToken(ProjectCallCommandArgs args)
	{
		var auth = args.Provider.GetService<IAuthApi>();
		if (string.IsNullOrWhiteSpace(args.asPlayer) || args.asPlayer == AS_GUEST)
		{
			var guest = await auth.CreateUser();
			Log.Debug("created a new guest player");
			return (guest, true, guest.refresh_token);
		}

		try
		{
			var token = await auth.LoginRefreshToken(args.asPlayer);
			return (token, false, args.asPlayer);
		}
		catch (RequesterException ex)
		{
			throw new CliException(
				$"Could not sign in with the refresh token given to --as (HTTP {ex.Status}). Use --as guest, or the refreshToken of an earlier `project call` in this realm");
		}
	}

	/// <summary>
	/// Reads the caller's id from <c>/basic/accounts/me</c>. Best effort: a failure only leaves the id at 0.
	/// </summary>
	private static async Task<long> GetPlayerId(MicroserviceHttpCaller caller, string host, string cid, string pid, string accessToken, CancellationToken token)
	{
		try
		{
			var headers = new Dictionary<string, string>
			{
				["X-BEAM-SCOPE"] = $"{cid}.{pid}",
				["Authorization"] = $"Bearer {accessToken}",
			};
			var response = await caller.Send(HttpMethod.Get, $"{host}/basic/accounts/me", null, headers, token);
			if (response == null || response.status is < 200 or >= 300)
			{
				return 0;
			}
			return JObject.Parse(response.body)["id"]?.Value<long>() ?? 0;
		}
		catch (Exception ex)
		{
			Log.Debug($"could not read the caller's player id: {ex.Message}");
			return 0;
		}
	}
}
