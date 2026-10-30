using Beamable.Server;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>
/// <c>beam local up --remote-microservices</c>: microservices deployed from this local stack run as real Fargate
/// tasks that dial back to this machine. Design: DesignDocs/infra/local-stack/remote-microservice-deploy.md. The
/// shared AWS side is BeamablePrivateCloud's terraform-modules/remote-microservices.
///
/// Nothing in AWS is per developer. The developer's name, &lt;dev&gt;, picks three host names under
/// <see cref="Domain"/>: &lt;dev&gt; for the websocket and API, &lt;dev&gt;-registry for image pulls, and
/// &lt;dev&gt;-clickhouse for deployed services' telemetry, which lands in the local ClickHouse. On <c>up</c>:
/// <list type="number">
/// <item><see cref="Preflight"/> checks AWS access, local tools and DNS, and works out &lt;dev&gt;.</item>
/// <item><see cref="Apply"/> layers the registry onto the docker step, points the gateway at the names, and
/// adds the tunnel step that keeps <c>ssh -R</c> open to the shared tunnel host.</item>
/// <item><see cref="EnsureCaddyCaTrusted"/> runs once Caddy is up.</item>
/// </list>
/// </summary>
public static class RemoteMicroservices
{
	// The shared infrastructure's fixed names (terraform-modules/remote-microservices, environment "local").
	public const string Domain = "ms-local.beamable.com";
	public const string Region = "us-west-2";
	public const string AccountId = "386048776778";
	public const string TunnelRoleArn = "arn:aws:iam::" + AccountId + ":role/beamable-local-remote-ms-tunnel-user";
	public const string TunnelInstanceName = "beamable-local-remote-ms-tunnel";
	/// <summary>The SSM session document the tunnel role may use: SSH, port 22 only (the caller can't pick a port).</summary>
	public const string SshDocumentName = "beamable-local-remote-ms-ssh";
	public const string TunnelUser = "tunnel";

	/// <summary>BeamableAPI's compose override that adds the registry and its Caddy site.</summary>
	public const string ComposeOverride = "docker-compose.remote-ms.yml";
	public const string TunnelStepName = "tunnel: remote microservices";

	/// <summary>The tunnel step logs this once ssh has connected; it is the step's readiness signal.</summary>
	public const string TunnelReadyMarker = "remote-microservices tunnel connected";

	// The LocalStackTemplate steps this changes.
	public const string DockerStepName = "docker: api deps + caddy";
	public const string GatewayStepName = "c# gateway";

	private const string RegistrySuffix = "-registry";
	private const string ClickHouseSuffix = "-clickhouse";
	private static readonly string[] ReservedSuffixes = { RegistrySuffix, ClickHouseSuffix };
	private const int MaxNameLength = 63 - 11; // <dev>-clickhouse must fit in one 63-character DNS label

	public static string GatewayHost(string dev) => $"{dev}.{Domain}";
	public static string RegistryHost(string dev) => $"{dev}{RegistrySuffix}.{Domain}";

	/// <summary>
	/// The gateway's ClickHouse endpoint while remote microservices are on. It's handed to deployed services'
	/// OTEL collectors as well as used on this machine, so it has to resolve on both sides: to Caddy here, to the
	/// tunnel host in Fargate.
	/// </summary>
	public static string ClickHouseHost(string dev) => $"{dev}{ClickHouseSuffix}.{Domain}";

	/// <summary>
	/// &lt;dev&gt; from the caller's ARN: an SSO session's email local part
	/// (<c>…:assumed-role/AWSReservedSSO_…/justin@beamable.com</c> → <c>justin</c>) or an IAM user's name
	/// (<c>…:user/jlefebvre-cli</c> → <c>jlefebvre-cli</c>), reduced to a DNS label. Null when nothing usable is left.
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
	/// Checks, before anything launches, everything the remote pieces need, and returns &lt;dev&gt;. Throws one
	/// <see cref="CliException"/> listing every problem with its fix.
	/// </summary>
	public static string Preflight(string nameOverride)
	{
		if (OperatingSystem.IsWindows())
		{
			throw new CliException("--remote-microservices runs on macOS and Linux for now: its tunnel is a POSIX sh loop.");
		}

		var aws = AwsPreflightService.FindAwsCli()
		          ?? throw new CliException("--remote-microservices needs the AWS CLI v2: https://aws.amazon.com/cli/");

		var identity = AwsPreflightService.Run(aws, "sts get-caller-identity --output json", null);
		if (!identity.ok && LooksLikeExpiredSso(identity.output))
		{
			Log.Information("Your AWS SSO session has expired; running `aws sso login`.");
			RunInteractive(aws, "sso login");
			identity = AwsPreflightService.Run(aws, "sts get-caller-identity --output json", null);
		}

		if (!identity.ok)
		{
			throw new CliException(
				$"Your AWS credentials aren't working: {identity.output?.Trim()}\n" +
				"Sign in (for SSO: `aws sso login`) and run `beam local up --remote-microservices` again.");
		}

		var dev = string.IsNullOrWhiteSpace(nameOverride)
			? DeriveName(AwsPreflightService.TryJsonValue(identity.output, "Arn"))
			: nameOverride.Trim();
		var invalid = ValidateName(dev);
		if (invalid != null)
		{
			throw new CliException(
				$"Can't use '{dev}' as your remote-microservices name: {invalid}. Pass --remote-microservices-name <name>.");
		}

		var problems = new List<string>();

		// The output holds temporary credentials on success, so it's only ever shown on failure.
		var assume = AwsPreflightService.Run(aws, AwsPreflightService.AssumeRoleArgs(TunnelRoleArn, $"beam-preflight-{dev}"), null);
		if (!assume.ok)
		{
			problems.Add(
				$"You can't assume the tunnel role {TunnelRoleArn}. Your AWS account has to allow sts:AssumeRole on it. " +
				$"({assume.output?.Trim()})");
		}

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

		// Public DNS has *.ms-local.beamable.com → 127.0.0.1, but some routers and corporate resolvers drop
		// public names that point at 127.0.0.1 (DNS rebinding protection).
		var unresolved = new[] { RegistryHost(dev), ClickHouseHost(dev) }.Where(h => !ResolvesToLoopback(h)).ToList();
		if (unresolved.Count > 0)
		{
			var (verb, pronoun) = unresolved.Count == 1 ? ("doesn't", "it") : ("don't", "them");
			problems.Add(
				$"{string.Join(" and ", unresolved)} {verb} resolve to 127.0.0.1 here; your DNS resolver may block names " +
				$"that point at 127.0.0.1. Add {pronoun} to /etc/hosts: echo \"127.0.0.1 {string.Join(" ", unresolved)}\" | sudo tee -a /etc/hosts");
		}

		if (problems.Count > 0)
		{
			throw new CliException("--remote-microservices isn't ready:\n- " + string.Join("\n- ", problems));
		}

		return dev;
	}

	/// <summary>
	/// Changes this run's copy of the manifest, never the file: the docker step also starts the registry, the
	/// gateway gets the two host names, and the tunnel step is added right after the gateway.
	/// </summary>
	public static void Apply(LocalStackConfig config, string dev)
	{
		var docker = FindStep(config, DockerStepName);
		var gateway = FindStep(config, GatewayStepName);

		docker.arguments = WithOverride(docker.arguments);
		docker.stopArguments = WithOverride(docker.stopArguments);
		docker.purgeStopArguments = WithOverride(docker.purgeStopArguments);
		docker.environment ??= new Dictionary<string, string>();
		// The override won't even parse without it, so `beam local stop` needs it too (the run state records it).
		docker.environment["MS_REGISTRY_HOST"] = RegistryHost(dev);
		docker.environment["MS_CLICKHOUSE_HOST"] = ClickHouseHost(dev);

		gateway.environment ??= new Dictionary<string, string>();
		gateway.environment["BEAMABLE_Beamo__WebsocketUri"] = $"wss://{GatewayHost(dev)}/socket";
		gateway.environment["BEAMABLE_Beamo__DockerRegistryHostAndPort"] = $"{RegistryHost(dev)}:443";
		gateway.environment["BEAMABLE_Beamo__DockerRegistryProtocol"] = "https";
		// Deployed services' collectors export here, so their logs and traces land in the local ClickHouse.
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Host"] = ClickHouseHost(dev);
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Port"] = "443";
		gateway.environment["BEAMABLE_Beamo__Otel__ClickHouse__Endpoint__Protocol"] = "https";

		if (config.steps.Any(s => s.name == TunnelStepName))
		{
			return;
		}

		config.steps.Insert(config.steps.IndexOf(gateway) + 1, new LocalStackStep
		{
			name = TunnelStepName,
			workingDirectory = docker.workingDirectory,
			shell = true,
			shellKind = "sh",
			arguments = TunnelScript(dev),
			readyWhenLogContains = TunnelReadyMarker,
			readyTimeoutSeconds = 90
		});
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
				"--remote-microservices can't add its registry to it. Re-run `beam local init` to regenerate the manifest.");
		}

		return $"compose -f docker-compose.yml -f {ComposeOverride} {composeArguments.Substring(verb.Length)}";
	}

	/// <summary>
	/// The tunnel step: a POSIX sh loop that keeps three <c>ssh -R</c> forwards open to the shared tunnel host over
	/// SSM Session Manager, reconnecting whenever ssh exits. Each pass assumes the tunnel role afresh and pushes a
	/// 60-second EC2 Instance Connect key, so nothing long-lived is stored. The step's arguments go through token
	/// substitution, so the script must not contain a dollar sign followed by a brace.
	/// </summary>
	public static string TunnelScript(string dev) => string.Join("\n", new[]
	{
		"set -u",
		$"dev='{dev}'",
		$"role='{TunnelRoleArn}'",
		$"region='{Region}'",
		$"ssh_document='{SshDocumentName}'",
		$"instance_name='{TunnelInstanceName}'",
		$"user='{TunnelUser}'",
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
	/// password prompt and no sudo; elsewhere this says how.
	/// </summary>
	public static async Task EnsureCaddyCaTrusted(LocalStackConfig config, string dev, CancellationToken token)
	{
		var url = $"https://{RegistryHost(dev)}/v2/";
		if (await AcceptsCertificate(url, token))
		{
			return;
		}

		var docker = FindStep(config, DockerStepName);
		var apiDir = LocalStackConfigIO.Substitute(docker.workingDirectory, config);
		var cert = Path.Combine(Path.GetTempPath(), $"beam-caddy-root-{Guid.NewGuid():N}.crt");
		var copied = RunQuiet("docker",
			new[] { "compose", "-f", "docker-compose.yml", "-f", ComposeOverride, "cp", "caddy:/data/caddy/pki/authorities/local/root.crt", cert },
			apiDir, docker.environment);
		if (!copied)
		{
			Log.Warning($"Couldn't copy Caddy's local CA out of its container, so pushes to {RegistryHost(dev)} will fail certificate checks. Trust /data/caddy/pki/authorities/local/root.crt from the caddy container by hand.");
			return;
		}

		if (!OperatingSystem.IsMacOS())
		{
			Log.Warning($"Trust Caddy's local CA, now at {cert}, so pushes to {RegistryHost(dev)} pass certificate checks (for example, copy it into /usr/local/share/ca-certificates and run update-ca-certificates).");
			return;
		}

		try
		{
			Log.Information($"Adding Caddy's local CA to your login keychain, so the CLI and gateway trust {RegistryHost(dev)}. macOS will ask for your password.");
			var keychain = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Keychains", "login.keychain-db");
			RunInteractive("security", $"add-trusted-cert -r trustRoot -k \"{keychain}\" \"{cert}\"");
			if (!await AcceptsCertificate(url, token))
			{
				Log.Warning($"{RegistryHost(dev)} still isn't trusted, so pushes will fail certificate checks. Open Keychain Access and set Caddy's local CA to Always Trust.");
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

	private static LocalStackStep FindStep(LocalStackConfig config, string name) =>
		config.steps.FirstOrDefault(s => s.name == name)
		?? throw new CliException(
			$"--remote-microservices needs the '{name}' step, which this manifest doesn't have. Re-run `beam local init` to regenerate it.");

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
