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

	/// <summary>The tunnel step's shell here: <see cref="TunnelPowerShellScript"/> on Windows, <see cref="TunnelScript"/> elsewhere.</summary>
	public static string TunnelShellKind => OperatingSystem.IsWindows() ? "powershell" : "sh";

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
	/// True when the stack in <paramref name="state"/> still runs <c>:remote-ms</c>: it started with it, and one of the
	/// steps it changes (the docker deps, the gateway or the tunnel) is in <paramref name="running"/>. Stopping only the
	/// tunnel leaves the docker deps and gateway running the remote way, so the tunnel's entry alone can't say.
	/// </summary>
	public static bool RanIn(LocalStackRunState state, IReadOnlyCollection<string> running)
	{
		if (state == null)
		{
			return false;
		}

		bool Running(string name) => running.Contains(name, StringComparer.OrdinalIgnoreCase);
		var started = !string.IsNullOrEmpty(state.remoteMsHost)
		              // Run states written before remoteMsHost: the tunnel's entry is all there is.
		              || state.steps.Any(e => string.Equals(e.name, TunnelStepName, StringComparison.OrdinalIgnoreCase));
		return started && new[] { DockerStepName, GatewayStepName, TunnelStepName }.Any(Running);
	}

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
			shellKind = TunnelShellKind,
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
	/// <paramref name="canPrompt"/> (a real run in a terminal) lets it run <c>aws sso login</c> when the SSO session
	/// has expired, and add the developer's names to the hosts file when the DNS resolver blocks them.
	/// </summary>
	public static (RemoteMsSettings settings, List<string> problems) Check(LocalStackConfig config,
		IReadOnlyCollection<LocalStackStep> steps, bool runsLocal, bool canPrompt)
	{
		var problems = StackProblems(steps, runsLocal);
		if (!HasSessionManagerPlugin() && canPrompt && (OperatingSystem.IsWindows() || (OperatingSystem.IsMacOS() && OnPath("brew"))))
		{
			InstallSessionManagerPlugin();
		}

		if (!HasSessionManagerPlugin())
		{
			var install = OperatingSystem.IsWindows()
				? $"install it from {SessionManagerPluginWindowsInstaller}"
				: OperatingSystem.IsMacOS()
					? "install it with `brew install --cask session-manager-plugin`"
					: "see https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html";
			var auto = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
			problems.Add("The AWS Session Manager plugin isn't installed. " + (!auto
				? char.ToUpper(install[0]) + install.Substring(1) + "."
				: canPrompt
					? $"Installing it didn't work; {install}."
					: $"A run in a terminal installs it, or {install}."));
		}

		if (!OnPath("ssh") || !OnPath("ssh-keygen"))
		{
			problems.Add("ssh and ssh-keygen have to be on your PATH." + (OperatingSystem.IsWindows()
				? " Add Windows' OpenSSH Client: Settings > System > Optional features."
				: ""));
		}

		var aws = AwsPreflightService.FindAwsCli();
		if (aws == null)
		{
			problems.Add("It needs the AWS CLI v2: https://aws.amazon.com/cli/");
			return (null, problems);
		}

		var identity = AwsPreflightService.Run(aws, "sts get-caller-identity --output json", null);
		if (!identity.ok && LooksLikeExpiredSso(identity.output) && canPrompt)
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
		var assume = AwsPreflightService.Run(aws, AwsPreflightService.AssumeRoleArgs(found.roleArn, PreflightSessionName(dev)), null);
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
		// public names that point at 127.0.0.1 (DNS rebinding protection). The hosts file works on any network.
		var unresolved = Unresolved(settings, ResolvesToLoopback);
		if (unresolved.Count > 0 && canPrompt)
		{
			Log.Warning($"{string.Join(" and ", unresolved)} {(unresolved.Count == 1 ? "doesn't" : "don't")} resolve to 127.0.0.1 here " +
			            $"(your DNS resolver may block names that point at 127.0.0.1), so they're being added to {HostsPath}. " +
			            (OperatingSystem.IsWindows() ? "Windows will ask to allow the change." : "sudo will ask for your password."));
			AppendToHosts(HostsEntry(unresolved));
			unresolved = Unresolved(settings, ResolvesToLoopback);
		}

		if (unresolved.Count > 0)
		{
			var (verb, pronoun) = unresolved.Count == 1 ? ("doesn't", "it") : ("don't", "them");
			var manual = OperatingSystem.IsWindows()
				? $"as administrator, add this line to {HostsPath}: {HostsEntry(unresolved)}"
				: $"echo \"{HostsEntry(unresolved)}\" | sudo tee -a {HostsPath}";
			problems.Add(
				$"{string.Join(" and ", unresolved)} {verb} resolve to 127.0.0.1 here; your DNS resolver may block names " +
				$"that point at 127.0.0.1. {(canPrompt ? "Adding" : "A run in a terminal adds")} {pronoun} to {HostsPath}" +
				$"{(canPrompt ? " didn't work; add" : ", or add")} {pronoun} yourself: {manual}");
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

		tunnel.shellKind = TunnelShellKind;
		tunnel.arguments = OperatingSystem.IsWindows() ? TunnelPowerShellScript(settings) : TunnelScript(settings);
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
	/// The role session name the checks assume the tunnel role with. STS allows 64 characters, and a name is at most
	/// <see cref="MaxNameLength"/>, so the prefix leaves room for the longest.
	/// </summary>
	public static string PreflightSessionName(string dev) => $"beam-check-{dev}";

	/// <summary>
	/// The tunnel step: a POSIX sh loop that keeps three <c>ssh -R</c> forwards open to the shared tunnel host over
	/// SSM Session Manager, reconnecting whenever ssh exits. Each pass assumes the tunnel role afresh, with the
	/// caller's own credentials (a profile or environment variables), and pushes a 60-second EC2 Instance Connect key,
	/// so nothing long-lived is stored. Each connection runs in a subshell, so the role's credentials it exports never
	/// replace the caller's for the next pass. The step's arguments go through token substitution, so the script must
	/// not contain a dollar sign followed by a brace; <see cref="RemoteMsSettings.Parse"/> keeps the values it
	/// interpolates to <see cref="ScriptSafe"/> characters.
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
		"  creds=$(aws sts assume-role --role-arn \"$role\" --role-session-name \"beam-tunnel-$dev\" --query 'Credentials.[AccessKeyId,SecretAccessKey,SessionToken]' --output text) || { echo 'could not assume the tunnel role; retrying in 15s'; sleep 15; continue; }",
		"  # One connection, as the tunnel role. Exit 75 asks for a retry in 15 seconds.",
		"  (",
		"    set -- $creds",
		"    export AWS_ACCESS_KEY_ID=\"$1\" AWS_SECRET_ACCESS_KEY=\"$2\" AWS_SESSION_TOKEN=\"$3\" AWS_REGION=\"$region\" AWS_PAGER=''",
		"    unset AWS_PROFILE AWS_DEFAULT_PROFILE",
		"    instance=$(aws ec2 describe-instances --filters \"Name=tag:Name,Values=$instance_name\" Name=instance-state-name,Values=running --query 'Reservations[0].Instances[0].InstanceId' --output text)",
		"    case \"$instance\" in i-*) ;; *) echo \"tunnel host $instance_name isn't running; retrying in 15s\"; exit 75;; esac",
		"    aws ec2-instance-connect send-ssh-public-key --instance-id \"$instance\" --instance-os-user \"$user\" --ssh-public-key \"file://$keydir/key.pub\" >/dev/null || { echo 'could not push the ssh key; retrying in 15s'; exit 75; }",
		"    # The SSM session already authenticates the instance, and its host key changes whenever the host is",
		"    # replaced, so no host key is pinned. -F /dev/null keeps the user's ~/.ssh/config out of it.",
		"    ssh -N -F /dev/null -i \"$keydir/key\" -o IdentitiesOnly=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR \\",
		"      -o ServerAliveInterval=30 -o ServerAliveCountMax=3 -o ExitOnForwardFailure=yes \\",
		$"      -o PermitLocalCommand=yes -o 'LocalCommand=echo {TunnelReadyMarker}' \\",
		"      -o \"ProxyCommand=aws ssm start-session --region $region --target %h --document-name $ssh_document\" \\",
		"      -R \"/run/tunnels/$dev.sock:127.0.0.1:8080\" -R \"/run/tunnels/$dev-registry.sock:127.0.0.1:5002\" \\",
		"      -R \"/run/tunnels/$dev-clickhouse.sock:127.0.0.1:8123\" \\",
		"      \"$user@$instance\"",
		"  )",
		"  status=$?",
		"  if [ \"$status\" -eq 75 ]; then sleep 15; continue; fi",
		"  echo \"tunnel closed (ssh exit $status); reconnecting in 5s\"",
		"  sleep 5",
		"done",
	});

	/// <summary>
	/// <see cref="TunnelScript"/> for Windows PowerShell 5.1, which the step runner runs <c>powershell</c> shell steps
	/// with. The ssh options go in a config file of its own (which also keeps ~/.ssh/config out of it), so no option
	/// with a path in it passes through PowerShell's native argument quoting. The key lives in a fixed folder per
	/// developer that every start empties, since a stopped step is killed before any cleanup can run. The known-hosts
	/// file is deleted before each connection, as /dev/null does for the sh loop: the host key changes whenever the
	/// host is replaced. Each pass sets the role's credentials and puts the caller's back afterwards. Same token
	/// substitution rule as <see cref="TunnelScript"/>: no dollar sign followed by a brace.
	/// </summary>
	public static string TunnelPowerShellScript(RemoteMsSettings settings) => string.Join("\n", new[]
	{
		"$ErrorActionPreference = 'Continue'",
		"# Windows PowerShell drops an empty native argument, so the empty passphrase goes as '\"\"'; Legacy makes PowerShell 7 do the same.",
		"$PSNativeCommandArgumentPassing = 'Legacy'",
		$"$dev = '{settings.Name}'",
		$"$role = '{settings.RoleArn}'",
		$"$region = '{settings.Region}'",
		$"$sshDocument = '{settings.SshDocument}'",
		$"$instanceName = '{settings.TunnelInstanceName}'",
		$"$user = '{settings.TunnelUser}'",
		"$keydir = Join-Path ([IO.Path]::GetTempPath()) ('beam-tunnel-' + $dev)",
		"Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $keydir",
		"New-Item -ItemType Directory -Force $keydir | Out-Null",
		"$key = Join-Path $keydir 'key'",
		"$knownHosts = Join-Path $keydir 'known_hosts'",
		"$config = Join-Path $keydir 'config'",
		"$slashed = $keydir.Replace('\\', '/')",
		"Set-Content -Encoding Ascii -Path $config -Value @(",
		"  ('IdentityFile \"' + $slashed + '/key\"'),",
		"  'IdentitiesOnly yes',",
		"  ('UserKnownHostsFile \"' + $slashed + '/known_hosts\"'),",
		"  'StrictHostKeyChecking no',",
		"  'LogLevel ERROR',",
		"  'ServerAliveInterval 30',",
		"  'ServerAliveCountMax 3',",
		"  'ExitOnForwardFailure yes',",
		"  'PermitLocalCommand yes',",
		$"  'LocalCommand echo {TunnelReadyMarker}',",
		"  ('ProxyCommand aws ssm start-session --region ' + $region + ' --target %h --document-name ' + $sshDocument)",
		")",
		"ssh-keygen -q -t ed25519 -N '\"\"' -C beam-local-tunnel -f $key",
		"if ($LASTEXITCODE -ne 0) { exit 1 }",
		"$pub = 'file://' + $key + '.pub'",
		"$callerVars = 'AWS_PROFILE', 'AWS_DEFAULT_PROFILE', 'AWS_ACCESS_KEY_ID', 'AWS_SECRET_ACCESS_KEY', 'AWS_SESSION_TOKEN', 'AWS_REGION'",
		"$caller = @{}",
		"foreach ($name in $callerVars) { $caller[$name] = [Environment]::GetEnvironmentVariable($name) }",
		"try {",
		"  while ($true) {",
		"    $wait = 5",
		"    $creds = aws sts assume-role --no-cli-pager --role-arn $role --role-session-name ('beam-tunnel-' + $dev) --query 'Credentials.[AccessKeyId,SecretAccessKey,SessionToken]' --output text",
		"    if ($LASTEXITCODE -ne 0) {",
		"      'could not assume the tunnel role; retrying in 15s'",
		"      $wait = 15",
		"    } else {",
		"      # One connection, as the tunnel role.",
		"      $parts = -split ($creds -join ' ')",
		"      try {",
		"        $env:AWS_ACCESS_KEY_ID = $parts[0]",
		"        $env:AWS_SECRET_ACCESS_KEY = $parts[1]",
		"        $env:AWS_SESSION_TOKEN = $parts[2]",
		"        $env:AWS_REGION = $region",
		"        Remove-Item Env:AWS_PROFILE, Env:AWS_DEFAULT_PROFILE -ErrorAction SilentlyContinue",
		"        $instance = (\"\" + (aws ec2 describe-instances --no-cli-pager --filters ('Name=tag:Name,Values=' + $instanceName) Name=instance-state-name,Values=running --query 'Reservations[0].Instances[0].InstanceId' --output text)).Trim()",
		"        if ($instance -notlike 'i-*') {",
		"          \"tunnel host $instanceName isn't running; retrying in 15s\"",
		"          $wait = 15",
		"        } else {",
		"          aws ec2-instance-connect send-ssh-public-key --no-cli-pager --instance-id $instance --instance-os-user $user --ssh-public-key $pub | Out-Null",
		"          if ($LASTEXITCODE -ne 0) {",
		"            'could not push the ssh key; retrying in 15s'",
		"            $wait = 15",
		"          } else {",
		"            Remove-Item -Force -ErrorAction SilentlyContinue $knownHosts",
		"            ssh -N -F $config -R ('/run/tunnels/' + $dev + '.sock:127.0.0.1:8080') -R ('/run/tunnels/' + $dev + '-registry.sock:127.0.0.1:5002') `",
		"              -R ('/run/tunnels/' + $dev + '-clickhouse.sock:127.0.0.1:8123') ($user + '@' + $instance) 2>&1 | ForEach-Object { \"$_\" }",
		"            \"tunnel closed (ssh exit $LASTEXITCODE); reconnecting in 5s\"",
		"          }",
		"        }",
		"      } finally {",
		"        foreach ($name in $callerVars) { [Environment]::SetEnvironmentVariable($name, $caller[$name]) }",
		"      }",
		"    }",
		"    Start-Sleep -Seconds $wait",
		"  }",
		"} finally {",
		"  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $keydir",
		"}",
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

		if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows())
		{
			Log.Warning($"Trust Caddy's local CA, now at {cert}, so pushes to {settings.RegistryHost} pass certificate checks (for example, copy it into /usr/local/share/ca-certificates and run update-ca-certificates).");
			return;
		}

		try
		{
			// A warning, not information: while the progress board shows, only warnings print, and the password
			// dialog needs saying why it appeared.
			if (OperatingSystem.IsWindows())
			{
				// The current user's Root store: no administrator rights, and Windows asks to confirm the certificate.
				Log.Warning($"Adding Caddy's local CA to your Windows trusted root certificates, so the CLI and gateway trust {settings.RegistryHost}. Windows will ask you to confirm.");
				RunInteractive("certutil", $"-user -addstore -f Root \"{cert}\"");
			}
			else
			{
				Log.Warning($"Adding Caddy's local CA to your login keychain, so the CLI and gateway trust {settings.RegistryHost}. macOS will ask for your password.");
				var keychain = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Keychains", "login.keychain-db");
				RunInteractive("security", $"add-trusted-cert -r trustRoot -k \"{keychain}\" \"{cert}\"");
			}

			if (!await AcceptsCertificate(url, token))
			{
				Log.Warning($"{settings.RegistryHost} still isn't trusted, so pushes will fail certificate checks. " + (OperatingSystem.IsWindows()
					? "Import Caddy's local CA into Trusted Root Certification Authorities (certmgr.msc)."
					: "Open Keychain Access and set Caddy's local CA to Always Trust."));
			}
		}
		finally
		{
			File.Delete(cert);
		}
	}

	/// <summary>
	/// False only when the server's certificate is rejected; any other outcome leaves nothing to trust, including the
	/// probe timing out. Only the caller's own cancellation propagates.
	/// </summary>
	public static async Task<bool> AcceptsCertificate(string url, CancellationToken token, TimeSpan? timeout = null)
	{
		using var http = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
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
		catch (TaskCanceledException) when (!token.IsCancellationRequested)
		{
			// HttpClient's own timeout: a stalled registry, not a certificate problem and not the caller's cancellation.
			return true;
		}
	}

	public static bool LooksLikeExpiredSso(string awsOutput) =>
		awsOutput != null
		&& awsOutput.Contains("sso", StringComparison.OrdinalIgnoreCase)
		&& (awsOutput.Contains("expired", StringComparison.OrdinalIgnoreCase)
		    || awsOutput.Contains("refresh failed", StringComparison.OrdinalIgnoreCase));

	/// <summary>Ends the hosts file line <see cref="HostsEntry"/> builds, so it's easy to find and remove.</summary>
	public const string HostsMarker = "# beam local run :remote-ms";

	public static string HostsPath => OperatingSystem.IsWindows()
		? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts")
		: "/etc/hosts";

	/// <summary>The developer's names this machine has to resolve to itself (the gateway name never is) that don't.</summary>
	public static List<string> Unresolved(RemoteMsSettings settings, Func<string, bool> resolvesToLoopback) =>
		new[] { settings.RegistryHost, settings.ClickHouseHost }.Where(h => !resolvesToLoopback(h)).ToList();

	/// <summary>The hosts file line that sends <paramref name="hosts"/> to this machine.</summary>
	public static string HostsEntry(IEnumerable<string> hosts) => $"127.0.0.1 {string.Join(" ", hosts)} {HostsMarker}";

	/// <summary>
	/// Appends <paramref name="line"/> to the hosts file. Elsewhere that's <c>sudo tee -a</c>, the line on stdin and no
	/// shell, and sudo asks for the password on the terminal; on Windows, an elevated PowerShell, which asks through
	/// UAC and then flushes the DNS cache. The names in it passed <see cref="ValidateName"/> and <see cref="IsDomain"/>.
	/// </summary>
	private static void AppendToHosts(string line)
	{
		var hosts = HostsPath;
		var current = File.Exists(hosts) ? File.ReadAllText(hosts) : "";
		var text = (current.Length > 0 && !current.EndsWith('\n') ? Environment.NewLine : "") + line + Environment.NewLine;
		if (OperatingSystem.IsWindows())
		{
			// The line is only names and an IP, but it goes in as a single-quoted literal all the same.
			var script = $"[IO.File]::AppendAllText('{hosts.Replace("'", "''")}', '{text.Replace("'", "''")}'); ipconfig /flushdns | Out-Null";
			var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
			try
			{
				using var elevated = Process.Start(new ProcessStartInfo("powershell", $"-NoProfile -NonInteractive -EncodedCommand {encoded}")
				{
					UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden
				});
				elevated?.WaitForExit();
			}
			catch (System.ComponentModel.Win32Exception e)
			{
				// UAC declined: the check after this reports the names as unresolved, with the line to add by hand.
				Log.Warning($"Couldn't update {hosts}: {e.Message}");
			}

			return;
		}

		var psi = new ProcessStartInfo("sudo") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
		foreach (var a in new[] { "tee", "-a", hosts })
		{
			psi.ArgumentList.Add(a);
		}

		using var proc = Process.Start(psi);
		if (proc == null)
		{
			return;
		}

		proc.StandardInput.Write(text);
		proc.StandardInput.Close();
		proc.StandardOutput.ReadToEnd();
		proc.WaitForExit();
	}

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

	public const string SessionManagerPluginWindowsInstaller =
		"https://s3.amazonaws.com/session-manager-downloads/plugin/latest/windows/SessionManagerPluginSetup.exe";

	/// <summary>Where AWS's own installers put the plugin. The tunnel's aws finds it through PATH.</summary>
	private static string SessionManagerPluginInstallDir => OperatingSystem.IsWindows()
		? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Amazon", "SessionManagerPlugin", "bin")
		: "/usr/local/sessionmanagerplugin/bin";

	/// <summary>
	/// True when <c>session-manager-plugin</c> is on PATH. An install this terminal's PATH predates (the Windows
	/// installer only changes PATH for new terminals) is put on this process's PATH, which every step inherits.
	/// </summary>
	private static bool HasSessionManagerPlugin()
	{
		if (OnPath("session-manager-plugin"))
		{
			return true;
		}

		var dir = SessionManagerPluginInstallDir;
		var exe = Path.Combine(dir, OperatingSystem.IsWindows() ? "session-manager-plugin.exe" : "session-manager-plugin");
		if (!File.Exists(exe))
		{
			return false;
		}

		Environment.SetEnvironmentVariable("PATH", dir + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"));
		return true;
	}

	/// <summary>
	/// Installs the AWS Session Manager plugin: with Homebrew on macOS; on Windows, AWS's installer, run quietly and
	/// elevated (one UAC prompt) once its Authenticode signature is checked to be AWS's. Failures are logged; the
	/// check after this reports the plugin as missing, with how to install it by hand.
	/// </summary>
	private static void InstallSessionManagerPlugin()
	{
		if (OperatingSystem.IsMacOS())
		{
			Log.Warning("The AWS Session Manager plugin, which the tunnel needs, isn't installed; installing it with Homebrew.");
			RunInteractive("brew", "install --cask session-manager-plugin");
			return;
		}

		Log.Warning("The AWS Session Manager plugin, which the tunnel needs, isn't installed; installing it. Windows will ask to allow the installer.");
		var dir = Directory.CreateTempSubdirectory("beam-ssm-plugin-").FullName;
		try
		{
			var installer = Path.Combine(dir, "SessionManagerPluginSetup.exe");
			using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
			{
				File.WriteAllBytes(installer, http.GetByteArrayAsync(SessionManagerPluginWindowsInstaller).GetAwaiter().GetResult());
			}

			var signature = RunCapture("powershell", "-NoProfile -NonInteractive -Command " +
				$"\"$s = Get-AuthenticodeSignature -LiteralPath '{installer.Replace("'", "''")}'; $s.Status.ToString() + '|' + $s.SignerCertificate.Subject\"");
			if (!signature.StartsWith("Valid|CN=\"Amazon Web Services, Inc.\"", StringComparison.Ordinal))
			{
				Log.Warning($"The downloaded Session Manager plugin installer isn't signed by AWS ({signature.Trim()}), so it wasn't run.");
				return;
			}

			using var proc = Process.Start(new ProcessStartInfo(installer, "/install /quiet /norestart") { UseShellExecute = true, Verb = "runas" });
			proc?.WaitForExit();
			// 3010: installed, and a restart is wanted (not needed for the plugin itself).
			if (proc != null && proc.ExitCode != 0 && proc.ExitCode != 3010)
			{
				Log.Warning($"The Session Manager plugin installer exited with {proc.ExitCode}.");
			}
		}
		catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or System.ComponentModel.Win32Exception)
		{
			// Win32Exception: UAC declined.
			Log.Warning($"Couldn't install the Session Manager plugin: {e.Message}");
		}
		finally
		{
			try
			{
				Directory.Delete(dir, true);
			}
			catch (IOException)
			{
				// The installer can hold its file briefly after exiting; the temp folder is the OS's to clean.
			}
		}
	}

	/// <summary>Runs a command and returns its standard output, or an empty string when it can't start.</summary>
	private static string RunCapture(string fileName, string arguments)
	{
		using var proc = Process.Start(new ProcessStartInfo(fileName, arguments)
		{
			UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
		});
		if (proc == null)
		{
			return "";
		}

		var stderr = proc.StandardError.ReadToEndAsync();
		var output = proc.StandardOutput.ReadToEnd();
		proc.WaitForExit();
		_ = stderr.Result;
		return output;
	}

	private static bool OnPath(string executable)
	{
		// Windows finds a bare command name through PATHEXT: ssh is ssh.exe there.
		var names = OperatingSystem.IsWindows()
			? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
			.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(ext => executable + ext)
			.Prepend(executable)
			.ToList()
			: new List<string> { executable };
		return (Environment.GetEnvironmentVariable("PATH") ?? "")
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Any(dir => names.Any(name => File.Exists(Path.Combine(dir, name))));
	}

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
