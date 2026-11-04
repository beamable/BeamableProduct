using Beamable.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>
/// <c>beam local run &lt;stack&gt;:remote-ms</c>: microservices deployed from this local stack run as real Fargate
/// tasks that dial back to this machine. Design: DesignDocs/infra/local-stack/remote-microservice-deploy.md.
///
/// Nothing about the shared AWS infrastructure is in this code. <see cref="SettingsFile"/>, in the backend's checkout,
/// names the tunnel role and the SSM parameter that holds the rest; the CLI assumes the role and reads the parameter on
/// every run (<see cref="RemoteMsSettings"/>).
///
/// Nothing in AWS is per developer. The developer's name, &lt;dev&gt;, picks three host names under the shared
/// domain: &lt;dev&gt; for the websocket and API, &lt;dev&gt;-registry for image pulls, and &lt;dev&gt;-clickhouse for
/// deployed services' telemetry, which lands in the local ClickHouse.
///
/// The tunnel step being in the resolved stack is what turns this on (the <c>:remote-ms</c> variant adds it). Then:
/// <list type="number">
/// <item><see cref="Check"/> checks AWS access, local tools and DNS, works out &lt;dev&gt; and reads the settings.</item>
/// <item><see cref="Apply"/> layers the registry onto the docker step, gives the gateway its settings and host
/// names, and gives the tunnel step its script.</item>
/// <item><see cref="EnsureCaddyCaTrusted"/> runs once Caddy is up.</item>
/// </list>
/// </summary>
public static class RemoteMicroservices
{
	/// <summary>In the backend's checkout (the docker step's working directory): <c>{ "roleArn", "settingsParameter" }</c>.</summary>
	public const string SettingsFile = "remote-ms.json";

	/// <summary>BeamableAPI's compose override that adds the registry and its Caddy site.</summary>
	public const string ComposeOverride = "docker-compose.remote-ms.yml";
	public const string TunnelStepName = "tunnel: remote microservices";

	/// <summary>The settings variable that overrides the name derived from the AWS identity.</summary>
	public const string NameVariable = "remoteMsName";

	/// <summary>Settings variables that override <see cref="SettingsFile"/>'s values.</summary>
	public const string RoleArnVariable = "remoteMsRoleArn";
	public const string SettingsParameterVariable = "remoteMsSettingsParameter";

	/// <summary>The tunnel step logs this once ssh has connected; it is the step's readiness signal.</summary>
	public const string TunnelReadyMarker = "remote-microservices tunnel connected";

	// The LocalStackTemplate steps this changes.
	public const string DockerStepName = LocalStackTemplate.ApiDepsStepName;
	public const string GatewayStepName = LocalStackDependencies.GatewayStep;

	public const string RegistrySuffix = "-registry";
	public const string ClickHouseSuffix = "-clickhouse";
	private static readonly string[] ReservedSuffixes = { RegistrySuffix, ClickHouseSuffix };
	public const int MaxNameLength = 63 - 11; // <dev>-clickhouse must fit in one 63-character DNS label

	// Values that end up inside the tunnel step's sh script, between single quotes, and go through token substitution:
	// no quotes, no spaces, no dollar signs or braces.
	private static readonly Regex ScriptSafe = new("^[A-Za-z0-9:/._@+=,-]+$");
	private static readonly Regex RoleArnPattern = new(@"^arn:aws[a-z-]*:iam::\d{12}:role/[A-Za-z0-9+=,.@_/-]+$");
	private static readonly Regex ParameterArnPattern = new(@"^arn:aws[a-z-]*:ssm:([a-z0-9-]+):\d{12}:parameter(/[A-Za-z0-9_.\-/]+)$");
	private static readonly Regex DomainPattern = new(@"^([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$");

	/// <summary>True when <paramref name="steps"/> include the tunnel, which is what turns remote microservices on.</summary>
	public static bool Runs(IEnumerable<LocalStackStep> steps) =>
		steps.Any(s => string.Equals(s.name, TunnelStepName, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Adds the tunnel step to the settings' catalogue so <c>:remote-ms</c> can name it. Its script needs the settings,
	/// so <see cref="Apply"/> fills it in. Only where the settings have the backend's docker step to tunnel to.
	/// </summary>
	public static void AddTunnelStep(List<LocalStackStep> steps)
	{
		var docker = steps.FirstOrDefault(s => string.Equals(s.name, DockerStepName, StringComparison.OrdinalIgnoreCase));
		if (docker == null || Runs(steps))
		{
			return;
		}

		steps.Add(new LocalStackStep
		{
			name = TunnelStepName,
			label = "Remote microservices tunnel",
			tags = new List<string>(),
			dependsOn = new List<string> { GatewayStepName },
			workingDirectory = docker.workingDirectory,
			shell = true,
			shellKind = "sh",
			readyWhenLogContains = TunnelReadyMarker,
			readyTimeoutSeconds = 90
		});
	}

	/// <summary>
	/// &lt;dev&gt; from the caller's ARN: an SSO session's email local part
	/// (<c>…:assumed-role/AWSReservedSSO_…/jane@example.com</c> → <c>jane</c>) or an IAM user's name
	/// (<c>…:user/jane-cli</c> → <c>jane-cli</c>), reduced to a DNS label. Null when nothing usable is left.
	/// </summary>
	public static string DeriveName(string callerArn)
	{
		if (string.IsNullOrWhiteSpace(callerArn))
		{
			return null;
		}

		var local = callerArn.Split('/').Last().Split('@')[0];
		var name = Regex.Replace(local.ToLowerInvariant(), "[^a-z0-9-]+", "-").Trim('-');
		if (name.Length > MaxNameLength)
		{
			name = name.Substring(0, MaxNameLength).TrimEnd('-');
		}

		return name.Length == 0 ? null : name;
	}

	/// <summary>Null when <paramref name="name"/> works as &lt;dev&gt;; otherwise the reason it doesn't.</summary>
	public static string ValidateName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "it is empty";
		}

		if (name.Length > MaxNameLength)
		{
			return $"it is longer than {MaxNameLength} characters";
		}

		if (!Regex.IsMatch(name, "^[a-z0-9]([a-z0-9-]*[a-z0-9])?$"))
		{
			return "it must be lowercase letters, digits and hyphens, and can't start or end with a hyphen";
		}

		// <x>-registry and <x>-clickhouse are x's other names, so a <dev> ending in either would collide with
		// someone else's.
		var reserved = ReservedSuffixes.FirstOrDefault(name.EndsWith);
		if (reserved != null)
		{
			return $"it can't end with {reserved}";
		}

		return null;
	}

	/// <summary>
	/// The tunnel role and settings parameter: the settings variables when set, otherwise <see cref="SettingsFile"/> in
	/// the backend's checkout. Exactly one of the result and the problem is null.
	/// </summary>
	public static ((string roleArn, string parameterArn) found, string problem) Locate(LocalStackConfig config,
		IReadOnlyCollection<LocalStackStep> steps)
	{
		string roleArn = null, parameterArn = null;
		config.variables?.TryGetValue(RoleArnVariable, out roleArn);
		config.variables?.TryGetValue(SettingsParameterVariable, out parameterArn);

		var docker = steps.FirstOrDefault(s => string.Equals(s.name, DockerStepName, StringComparison.OrdinalIgnoreCase));
		var path = docker == null
			? null
			: Path.Combine(LocalStackConfigIO.Substitute(docker.workingDirectory, config) ?? "", SettingsFile);
		if ((string.IsNullOrWhiteSpace(roleArn) || string.IsNullOrWhiteSpace(parameterArn)) && path != null && File.Exists(path))
		{
			try
			{
				var file = JObject.Parse(File.ReadAllText(path));
				roleArn = string.IsNullOrWhiteSpace(roleArn) ? file.Value<string>("roleArn") : roleArn;
				parameterArn = string.IsNullOrWhiteSpace(parameterArn) ? file.Value<string>("settingsParameter") : parameterArn;
			}
			catch (Exception e) when (e is JsonException or IOException or InvalidCastException)
			{
				return (default, $"Can't read {path}: {e.Message}");
			}
		}

		if (string.IsNullOrWhiteSpace(roleArn) || string.IsNullOrWhiteSpace(parameterArn))
		{
			return (default, path != null && !File.Exists(path)
				? $"{path} is missing; update the backend's checkout. (Or set \"{RoleArnVariable}\" and \"{SettingsParameterVariable}\" under \"variables\" in the stack settings.)"
				: $"It needs \"roleArn\" and \"settingsParameter\" in {SettingsFile}, or \"{RoleArnVariable}\" and \"{SettingsParameterVariable}\" under \"variables\" in the stack settings.");
		}

		roleArn = roleArn.Trim();
		parameterArn = parameterArn.Trim();
		if (!RoleArnPattern.IsMatch(roleArn))
		{
			return (default, $"'{roleArn}' isn't an IAM role ARN.");
		}

		if (!ParameterArnPattern.IsMatch(parameterArn))
		{
			return (default, $"'{parameterArn}' isn't an SSM parameter ARN.");
		}

		return ((roleArn, parameterArn), null);
	}

	/// <summary><c>ssm get-parameter</c> arguments for <paramref name="parameterArn"/>, which <see cref="Locate"/> has checked.</summary>
	public static string GetParameterArgs(string parameterArn)
	{
		var match = ParameterArnPattern.Match(parameterArn);
		return $"ssm get-parameter --region {match.Groups[1].Value} --name {match.Groups[2].Value} --query Parameter.Value --output text";
	}

	/// <summary>
	/// What a stack with the tunnel needs before anything launches, and the settings to run it with (null when they
	/// can't be worked out, or the stack doesn't run the backend here to apply them to). Every problem comes back with
	/// its fix, so a run can refuse with all of them and a dry run can list them.
	/// <paramref name="canLogin"/> lets it run <c>aws sso login</c> when the SSO session has expired.
	/// </summary>
	public static (RemoteMsSettings settings, List<string> problems) Check(LocalStackConfig config,
		IReadOnlyCollection<LocalStackStep> steps, bool runsLocal, bool canLogin)
	{
		if (OperatingSystem.IsWindows())
		{
			return (null, new List<string> { "It runs on macOS and Linux for now: its tunnel is a POSIX sh loop." });
		}

		var problems = StackProblems(steps, runsLocal);
		if (!OnPath("session-manager-plugin"))
		{
			problems.Add("The AWS Session Manager plugin isn't installed. " + (OperatingSystem.IsMacOS()
				? "Install it with `brew install --cask session-manager-plugin`."
				: "See https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html"));
		}

		if (!OnPath("ssh") || !OnPath("ssh-keygen"))
		{
			problems.Add("ssh and ssh-keygen have to be on your PATH.");
		}

		var aws = AwsPreflightService.FindAwsCli();
		if (aws == null)
		{
			problems.Add("It needs the AWS CLI v2: https://aws.amazon.com/cli/");
			return (null, problems);
		}

		var identity = AwsPreflightService.Run(aws, "sts get-caller-identity --output json", null);
		if (!identity.ok && LooksLikeExpiredSso(identity.output) && canLogin)
		{
			Log.Information("Your AWS SSO session has expired; running `aws sso login`.");
			RunInteractive(aws, "sso login");
			identity = AwsPreflightService.Run(aws, "sts get-caller-identity --output json", null);
		}

		if (!identity.ok)
		{
			problems.Add($"Your AWS credentials aren't working; sign in (for SSO: `aws sso login`). ({identity.output?.Trim()})");
			return (null, problems);
		}

		string overrideName = null;
		config.variables?.TryGetValue(NameVariable, out overrideName);
		var dev = string.IsNullOrWhiteSpace(overrideName)
			? DeriveName(AwsPreflightService.TryJsonValue(identity.output, "Arn"))
			: overrideName.Trim();
		var invalid = ValidateName(dev);
		if (invalid != null)
		{
			problems.Add($"Can't use '{dev}' as your remote-microservices name: {invalid}. Set \"{NameVariable}\" under \"variables\" in the stack settings.");
			return (null, problems);
		}

		var (found, locateProblem) = Locate(config, steps);
		if (locateProblem != null)
		{
			problems.Add(locateProblem);
			return (null, problems);
		}

		// The output holds temporary credentials on success, so it's only ever shown on failure.
		var assume = AwsPreflightService.Run(aws, AwsPreflightService.AssumeRoleArgs(found.roleArn, $"beam-preflight-{dev}"), null);
		if (!assume.ok)
		{
			problems.Add(
				$"You can't assume the tunnel role {found.roleArn}. Your AWS account has to allow sts:AssumeRole on it. " +
				$"({assume.output?.Trim()})");
			return (null, problems);
		}

		var read = AwsPreflightService.Run(aws, GetParameterArgs(found.parameterArn), assume.output);
		if (!read.ok)
		{
			problems.Add($"Can't read the remote-microservices settings from {found.parameterArn} as the tunnel role. ({read.output?.Trim()})");
			return (null, problems);
		}

		var (settings, settingsProblem) = RemoteMsSettings.Parse(read.output, found.roleArn, dev);
		if (settingsProblem != null)
		{
			problems.Add($"The remote-microservices settings in {found.parameterArn} aren't usable: {settingsProblem}.");
			return (null, problems);
		}

		// Public DNS sends every name under the domain to 127.0.0.1, but some routers and corporate resolvers drop
		// public names that point at 127.0.0.1 (DNS rebinding protection).
		var unresolved = new[] { settings.RegistryHost, settings.ClickHouseHost }.Where(h => !ResolvesToLoopback(h)).ToList();
		if (unresolved.Count > 0)
		{
			var (verb, pronoun) = unresolved.Count == 1 ? ("doesn't", "it") : ("don't", "them");
			problems.Add(
				$"{string.Join(" and ", unresolved)} {verb} resolve to 127.0.0.1 here; your DNS resolver may block names " +
				$"that point at 127.0.0.1. Add {pronoun} to /etc/hosts: echo \"127.0.0.1 {string.Join(" ", unresolved)}\" | sudo tee -a /etc/hosts");
		}

		// A stack without the backend here has nothing to apply the settings to.
		return (StackProblems(steps, runsLocal).Count > 0 ? null : settings, problems);
	}

	/// <summary>What's wrong with the stack itself: the backend has to run here, docker deps and gateway included.</summary>
	public static List<string> StackProblems(IReadOnlyCollection<LocalStackStep> steps, bool runsLocal)
	{
		var problems = new List<string>();
		if (!runsLocal)
		{
			problems.Add("It needs the stack's backend on this machine, and this stack runs against a remote one.");
		}

		var missing = new[] { DockerStepName, GatewayStepName }
			.Where(n => !steps.Any(s => string.Equals(s.name, n, StringComparison.OrdinalIgnoreCase))).ToList();
		if (missing.Count > 0)
		{
			problems.Add($"It needs a stack that runs {string.Join(" and ", missing)}, for example full:remote-ms or backend:remote-ms.");
		}

		return problems;
	}

	/// <summary>The message a run refuses with when <see cref="Check"/> found problems.</summary>
	public static string NotReady(IEnumerable<string> problems) =>
		":remote-ms isn't ready:\n- " + string.Join("\n- ", problems);

	/// <summary>
	/// Changes this run's steps, never the settings file: the docker step also starts the registry, the gateway gets
	/// the shared settings and the host names, and the tunnel step gets its script. Applying twice changes nothing.
	/// </summary>
	public static void Apply(IReadOnlyCollection<LocalStackStep> steps, RemoteMsSettings settings)
	{
		var docker = FindStep(steps, DockerStepName);
		var gateway = FindStep(steps, GatewayStepName);
		var tunnel = FindStep(steps, TunnelStepName);

		docker.arguments = WithOverride(docker.arguments);
		docker.stopArguments = WithOverride(docker.stopArguments);
		docker.purgeStopArguments = WithOverride(docker.purgeStopArguments);
		docker.environment ??= new Dictionary<string, string>();
		// The override won't even parse without them, so `beam local stop` needs them too (the run state records them).
		docker.environment["MS_REGISTRY_HOST"] = settings.RegistryHost;
		docker.environment["MS_CLICKHOUSE_HOST"] = settings.ClickHouseHost;

		gateway.environment ??= new Dictionary<string, string>();
		// Subnets, roles and the registry secret: only for this run, so a stack without :remote-ms never starts Fargate
		// tasks that can't pull from localhost.
		foreach (var (key, value) in settings.GatewayEnvironment)
		{
			gateway.environment[key] = value;
		}

		gateway.environment["BEAMABLE_Beamo__WebsocketUri"] = $"wss://{settings.GatewayHost}/socket";
		gateway.environment["BEAMABLE_Beamo__DockerRegistryHostAndPort"] = $"{settings.RegistryHost}:443";
		gateway.environment["BEAMABLE_Beamo__DockerRegistryProtocol"] = "https";
		// Deployed services' collectors export here, so their logs and traces land in the local ClickHouse.
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Host"] = settings.ClickHouseHost;
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Port"] = "443";
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Protocol"] = "https";

		tunnel.arguments = TunnelScript(settings);
	}

	/// <summary><c>compose up -d</c> → <c>compose -f docker-compose.yml -f docker-compose.remote-ms.yml up -d</c>.</summary>
	public static string WithOverride(string composeArguments)
	{
		const string verb = "compose ";
		if (string.IsNullOrWhiteSpace(composeArguments) || composeArguments.Contains(ComposeOverride))
		{
			return composeArguments;
		}

		if (!composeArguments.StartsWith(verb))
		{
			throw new CliException(
				$"The '{DockerStepName}' step runs `docker {composeArguments}`, which isn't a `docker compose` command, so " +
				":remote-ms can't add its registry to it. Run `beam local setup` to rewrite the settings.");
		}

		return $"compose -f docker-compose.yml -f {ComposeOverride} {composeArguments.Substring(verb.Length)}";
	}

	/// <summary>
	/// The tunnel step: a POSIX sh loop that keeps three <c>ssh -R</c> forwards open to the shared tunnel host over
	/// SSM Session Manager, reconnecting whenever ssh exits. Each pass assumes the tunnel role afresh and pushes a
	/// 60-second EC2 Instance Connect key, so nothing long-lived is stored. The step's arguments go through token
	/// substitution, so the script must not contain a dollar sign followed by a brace; <see cref="RemoteMsSettings.Parse"/>
	/// keeps the values it interpolates to <see cref="ScriptSafe"/> characters.
	/// </summary>
	public static string TunnelScript(RemoteMsSettings settings) => string.Join("\n", new[]
	{
		"set -u",
		$"dev='{settings.Name}'",
		$"role='{settings.RoleArn}'",
		$"region='{settings.Region}'",
		$"ssh_document='{settings.SshDocument}'",
		$"instance_name='{settings.TunnelInstanceName}'",
		$"user='{settings.TunnelUser}'",
		"keydir=$(mktemp -d)",
		"trap 'rm -rf \"$keydir\"' EXIT INT TERM",
		"ssh-keygen -q -t ed25519 -N '' -C beam-local-tunnel -f \"$keydir/key\" || exit 1",
		"while :; do",
		"  unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN",
		"  creds=$(aws sts assume-role --role-arn \"$role\" --role-session-name \"beam-tunnel-$dev\" --query 'Credentials.[AccessKeyId,SecretAccessKey,SessionToken]' --output text) || { echo 'could not assume the tunnel role; retrying in 15s'; sleep 15; continue; }",
		"  set -- $creds",
		"  export AWS_ACCESS_KEY_ID=\"$1\" AWS_SECRET_ACCESS_KEY=\"$2\" AWS_SESSION_TOKEN=\"$3\" AWS_REGION=\"$region\" AWS_PAGER=''",
		"  unset AWS_PROFILE AWS_DEFAULT_PROFILE",
		"  instance=$(aws ec2 describe-instances --filters \"Name=tag:Name,Values=$instance_name\" Name=instance-state-name,Values=running --query 'Reservations[0].Instances[0].InstanceId' --output text)",
		"  case \"$instance\" in i-*) ;; *) echo \"tunnel host $instance_name isn't running; retrying in 15s\"; sleep 15; continue;; esac",
		"  aws ec2-instance-connect send-ssh-public-key --instance-id \"$instance\" --instance-os-user \"$user\" --ssh-public-key \"file://$keydir/key.pub\" >/dev/null || { echo 'could not push the ssh key; retrying in 15s'; sleep 15; continue; }",
		"  # The SSM session already authenticates the instance, and its host key changes whenever the host is",
		"  # replaced, so no host key is pinned. -F /dev/null keeps the user's ~/.ssh/config out of it.",
		"  ssh -N -F /dev/null -i \"$keydir/key\" -o IdentitiesOnly=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR \\",
		"    -o ServerAliveInterval=30 -o ServerAliveCountMax=3 -o ExitOnForwardFailure=yes \\",
		$"    -o PermitLocalCommand=yes -o 'LocalCommand=echo {TunnelReadyMarker}' \\",
		"    -o \"ProxyCommand=aws ssm start-session --region $region --target %h --document-name $ssh_document\" \\",
		"    -R \"/run/tunnels/$dev.sock:127.0.0.1:8080\" -R \"/run/tunnels/$dev-registry.sock:127.0.0.1:5002\" \\",
		"    -R \"/run/tunnels/$dev-clickhouse.sock:127.0.0.1:8123\" \\",
		"    \"$user@$instance\"",
		"  echo \"tunnel closed (ssh exit $?); reconnecting in 5s\"",
		"  sleep 5",
		"done",
	});

	/// <summary>
	/// Once Caddy is up: make sure this machine trusts Caddy's local CA, which signs the certificate for the
	/// registry name the CLI and gateway connect to. On macOS it's added to the login keychain, which takes one
	/// password prompt and no sudo; elsewhere this says how. Already trusted (every run after the first, until a
	/// purge replaces Caddy's data), it does nothing.
	/// </summary>
	public static async Task EnsureCaddyCaTrusted(IReadOnlyCollection<LocalStackStep> steps, LocalStackConfig config,
		RemoteMsSettings settings, CancellationToken token)
	{
		var url = $"https://{settings.RegistryHost}/v2/";
		if (await AcceptsCertificate(url, token))
		{
			return;
		}

		var docker = FindStep(steps, DockerStepName);
		var apiDir = LocalStackConfigIO.Substitute(docker.workingDirectory, config);
		var cert = Path.Combine(Path.GetTempPath(), $"beam-caddy-root-{Guid.NewGuid():N}.crt");
		var copied = RunQuiet("docker",
			new[] { "compose", "-f", "docker-compose.yml", "-f", ComposeOverride, "cp", "caddy:/data/caddy/pki/authorities/local/root.crt", cert },
			apiDir, docker.environment);
		if (!copied)
		{
			Log.Warning($"Couldn't copy Caddy's local CA out of its container, so pushes to {settings.RegistryHost} will fail certificate checks. Trust /data/caddy/pki/authorities/local/root.crt from the caddy container by hand.");
			return;
		}

		if (!OperatingSystem.IsMacOS())
		{
			Log.Warning($"Trust Caddy's local CA, now at {cert}, so pushes to {settings.RegistryHost} pass certificate checks (for example, copy it into /usr/local/share/ca-certificates and run update-ca-certificates).");
			return;
		}

		try
		{
			// A warning, not information: while the progress board shows, only warnings print, and the password
			// dialog needs saying why it appeared.
			Log.Warning($"Adding Caddy's local CA to your login keychain, so the CLI and gateway trust {settings.RegistryHost}. macOS will ask for your password.");
			var keychain = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Keychains", "login.keychain-db");
			RunInteractive("security", $"add-trusted-cert -r trustRoot -k \"{keychain}\" \"{cert}\"");
			if (!await AcceptsCertificate(url, token))
			{
				Log.Warning($"{settings.RegistryHost} still isn't trusted, so pushes will fail certificate checks. Open Keychain Access and set Caddy's local CA to Always Trust.");
			}
		}
		finally
		{
			File.Delete(cert);
		}
	}

	/// <summary>False only when the server's certificate is rejected; any other outcome leaves nothing to trust.</summary>
	internal static async Task<bool> AcceptsCertificate(string url, CancellationToken token)
	{
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
		try
		{
			using var _ = await http.GetAsync(url, token);
			return true;
		}
		catch (HttpRequestException e) when (e.HttpRequestError == HttpRequestError.SecureConnectionError)
		{
			return false;
		}
		catch (HttpRequestException)
		{
			return true;
		}
	}

	public static bool LooksLikeExpiredSso(string awsOutput) =>
		awsOutput != null
		&& awsOutput.Contains("sso", StringComparison.OrdinalIgnoreCase)
		&& (awsOutput.Contains("expired", StringComparison.OrdinalIgnoreCase)
		    || awsOutput.Contains("refresh failed", StringComparison.OrdinalIgnoreCase));

	/// <summary>True when <paramref name="value"/> can go between single quotes in the tunnel script.</summary>
	internal static bool IsScriptSafe(string value) => value != null && ScriptSafe.IsMatch(value);

	internal static bool IsDomain(string value) => value != null && DomainPattern.IsMatch(value);

	private static LocalStackStep FindStep(IEnumerable<LocalStackStep> steps, string name) =>
		steps.FirstOrDefault(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase))
		?? throw new CliException($":remote-ms needs the '{name}' step in the stack, and it isn't there.");

	private static bool ResolvesToLoopback(string host)
	{
		try
		{
			return Dns.GetHostAddresses(host).Any(IPAddress.IsLoopback);
		}
		catch (SocketException)
		{
			return false;
		}
	}

	private static bool OnPath(string executable) =>
		(Environment.GetEnvironmentVariable("PATH") ?? "")
		.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
		.Any(dir => File.Exists(Path.Combine(dir, executable)));

	/// <summary>Runs a command with the terminal attached, for anything that may prompt (a browser sign-in, a password).</summary>
	private static void RunInteractive(string fileName, string arguments)
	{
		using var proc = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false });
		proc?.WaitForExit();
	}

	private static bool RunQuiet(string fileName, IEnumerable<string> arguments, string workingDirectory, IDictionary<string, string> environment)
	{
		var psi = new ProcessStartInfo(fileName)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = workingDirectory
		};
		foreach (var a in arguments)
		{
			psi.ArgumentList.Add(a);
		}

		foreach (var (k, v) in environment ?? new Dictionary<string, string>())
		{
			psi.Environment[k] = v;
		}

		using var proc = Process.Start(psi);
		if (proc == null)
		{
			return false;
		}

		proc.StandardOutput.ReadToEnd();
		proc.StandardError.ReadToEnd();
		proc.WaitForExit();
		return proc.ExitCode == 0;
	}
}

/// <summary>
/// One <c>:remote-ms</c> run's settings: the developer's name, the tunnel role, and what the shared infrastructure's
/// SSM parameter says (written by BeamablePrivateCloud's terraform-modules/remote-microservices):
/// <code>{ "region", "domain", "tunnel": { "instanceName", "sshDocument", "user" }, "beamo": { … } }</code>
/// </summary>
public sealed class RemoteMsSettings
{
	/// <summary>&lt;dev&gt;, the first label of this developer's host names.</summary>
	public string Name { get; private init; }
	public string RoleArn { get; private init; }
	public string Region { get; private init; }
	public string Domain { get; private init; }
	public string TunnelInstanceName { get; private init; }
	/// <summary>The SSM session document the tunnel role may use: SSH, port 22 only (the caller can't pick a port).</summary>
	public string SshDocument { get; private init; }
	public string TunnelUser { get; private init; }

	/// <summary>The parameter's <c>beamo</c> object as the gateway's <c>BEAMABLE_Beamo__*</c> variables.</summary>
	public IReadOnlyDictionary<string, string> GatewayEnvironment { get; private init; }

	public string GatewayHost => $"{Name}.{Domain}";
	public string RegistryHost => $"{Name}{RemoteMicroservices.RegistrySuffix}.{Domain}";

	/// <summary>
	/// The gateway's ClickHouse endpoint while remote microservices are on. It's handed to deployed services'
	/// OTEL collectors as well as used on this machine, so it has to resolve on both sides: to Caddy here, to the
	/// tunnel host in Fargate.
	/// </summary>
	public string ClickHouseHost => $"{Name}{RemoteMicroservices.ClickHouseSuffix}.{Domain}";

	/// <summary>
	/// The settings from the parameter's <paramref name="json"/>, or why they can't be used. Every value the tunnel
	/// script interpolates has to be <see cref="RemoteMicroservices.IsScriptSafe"/>.
	/// </summary>
	public static (RemoteMsSettings settings, string problem) Parse(string json, string roleArn, string name)
	{
		JObject root;
		try
		{
			root = JObject.Parse(json ?? "");
		}
		catch (JsonException e)
		{
			return (null, $"it isn't a JSON object ({e.Message})");
		}

		string Text(JToken token) => token?.Type == JTokenType.String ? token.Value<string>() : null;

		var values = new Dictionary<string, string>
		{
			["region"] = Text(root["region"]),
			["tunnel.instanceName"] = Text(root["tunnel"]?["instanceName"]),
			["tunnel.sshDocument"] = Text(root["tunnel"]?["sshDocument"]),
			["tunnel.user"] = Text(root["tunnel"]?["user"]),
		};
		var unsafeKey = values.FirstOrDefault(kv => !RemoteMicroservices.IsScriptSafe(kv.Value)).Key;
		if (unsafeKey != null)
		{
			return (null, $"\"{unsafeKey}\" is missing or has characters it can't have");
		}

		var domain = Text(root["domain"]);
		if (!RemoteMicroservices.IsDomain(domain))
		{
			return (null, "\"domain\" is missing or isn't a domain name");
		}

		if (root["beamo"] is not JObject beamo)
		{
			return (null, "\"beamo\" is missing or isn't an object");
		}

		var environment = new Dictionary<string, string>();
		Flatten(beamo, "BEAMABLE_Beamo", environment);

		return (new RemoteMsSettings
		{
			Name = name,
			RoleArn = roleArn,
			Region = values["region"],
			Domain = domain,
			TunnelInstanceName = values["tunnel.instanceName"],
			SshDocument = values["tunnel.sshDocument"],
			TunnelUser = values["tunnel.user"],
			GatewayEnvironment = environment,
		}, null);
	}

	/// <summary>
	/// ASP.NET configuration's environment variable form: objects nest with <c>__</c>, arrays by index, booleans as
	/// <c>true</c>/<c>false</c>.
	/// </summary>
	private static void Flatten(JToken token, string prefix, Dictionary<string, string> into)
	{
		switch (token)
		{
			case JObject obj:
				foreach (var property in obj.Properties())
				{
					Flatten(property.Value, $"{prefix}__{property.Name}", into);
				}

				break;
			case JArray array:
				for (var i = 0; i < array.Count; i++)
				{
					Flatten(array[i], $"{prefix}__{i}", into);
				}

				break;
			case JValue { Type: JTokenType.Boolean } boolean:
				into[prefix] = (bool)boolean ? "true" : "false";
				break;
			case JValue { Type: JTokenType.Null }:
				break;
			case JValue value:
				into[prefix] = Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture);
				break;
		}
	}
}
