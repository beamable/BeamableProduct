using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>One AWS prerequisite, and what to do when it is not satisfied.</summary>
public class AwsCheck
{
	public string name;
	public bool ok;

	/// <summary>What was observed (identity, ARN, bucket name). Never contains a secret value.</summary>
	public string detail;

	/// <summary>Shown when <see cref="ok"/> is false: the concrete action that fixes it.</summary>
	public string remediation;

	/// <summary>
	/// What AWS actually said. Kept separate from <see cref="remediation"/> and always reported alongside it:
	/// the guessed cause is only a guess, and hiding the real error turns a five-second diagnosis (an expired SSO
	/// session, a bad region, a throttle) into a wild goose chase after the wrong problem. Never populated for the
	/// secret check, whose output is discarded unread.
	/// </summary>
	public string awsError;

	/// <summary>
	/// True when a failure is informational rather than blocking — e.g. a resource this developer's realm does not
	/// use. Reported but does not make the run fail.
	/// </summary>
	public bool warning;
}

public class AwsPreflightResult
{
	public List<AwsCheck> checks = new List<AwsCheck>();

	/// <summary>True when every non-warning check passed.</summary>
	public bool allOk => checks.All(c => c.ok || c.warning);

	/// <summary>True when the <c>aws</c> CLI itself was missing, so nothing could be checked.</summary>
	public bool skipped;
}

/// <summary>
/// Verifies the AWS prerequisites the local stack has but never states. This is <b>check-only</b> — it creates,
/// deletes and modifies nothing.
///
/// The dependency is real and there is no LocalStack anywhere in the stack. <c>DefaultAWSCredentials</c> in
/// BeamableBackend reads a named <c>~/.aws</c> profile, then <c>AssumeRole</c>s into per-scope roles for S3,
/// Athena, ECR/ECS and <b>Secrets Manager</b> — and the Scala <c>auth</c> service fetches its JWT signing key
/// from Secrets Manager at runtime. So without working credentials the stack comes all the way up, every process
/// reports healthy, and then nothing can log in. That failure is indistinguishable from a broken backend, which
/// is exactly why it is worth a named preflight check.
///
/// Checks run through the <c>aws</c> CLI rather than the AWS SDK: it is already a de-facto prerequisite for this
/// stack, and shelling out keeps three more NuGet dependencies out of the CLI. Crucially, the Secrets Manager and
/// S3 checks run with <em>assumed-role</em> credentials, mirroring what the backend actually does — checking them
/// with the base profile would pass for a principal that cannot assume the role, which is the most common way
/// this is misconfigured.
/// </summary>
public class AwsPreflightService
{
	/// <summary>
	/// The region the platform's secrets and analytics resources live in. Not present in <c>awsglobal.conf</c> —
	/// it is a code default in <c>DefaultAWSCredentials</c> — so it is defaulted here and overridable.
	/// </summary>
	public const string DefaultRegion = "us-west-2";

	/// <summary>Session name used for the assume-role probes, so they are identifiable in CloudTrail.</summary>
	private const string SessionName = "beam-local-setup-preflight";

	/// <summary>
	/// Conf keys naming the roles the backend assumes. Read from the <em>rendered</em> <c>awsglobal.conf</c> rather
	/// than hardcoded, so a private-cloud or self-hosted setup with different accounts is checked against its own
	/// roles instead of Beamable's.
	/// </summary>
	private static readonly (string label, string key)[] RoleKeys =
	{
		("services (S3, ECR, ECS, Secrets Manager)", "aws.credentials.s3.services.role.arn"),
		("storage (microservice containers)", "aws.credentials.s3.storage.role.arn"),
		("analytics (S3 + Athena)", "aws.credentials.athena.analytics.role.arn"),
	};

	/// <summary>Conf keys naming the buckets the backend reads/writes locally.</summary>
	private static readonly (string label, string key)[] BucketKeys =
	{
		("trials", "aws.buckets.trials"),
		("content/comet", "aws.buckets.comet"),
		("geolocation", "aws.buckets.geolocation"),
	};

	/// <summary>The conf key holding the Secrets Manager id of the JWT signing key.</summary>
	private const string JwtSecretKey = "jwtToken.secret.reference";

	/// <summary>The conf key naming the role that has Secrets Manager access.</summary>
	private const string SecretsRoleKey = "aws.credentials.secretsmanager.services.role.arn";

	private readonly string _region;

	public AwsPreflightService(string region = null)
	{
		_region = string.IsNullOrWhiteSpace(region) ? DefaultRegion : region;
	}

	/// <summary>
	/// Runs the full preflight against a BeamableBackend checkout (for <c>awsglobal.conf</c>) and, optionally, a
	/// BeamableAPI checkout (for the scheduler queue in <c>appsettings.Local.json</c>).
	/// </summary>
	public AwsPreflightResult Run(string scalaDir, string apiDir)
	{
		var result = new AwsPreflightResult();

		var aws = FindAwsCli();
		if (aws == null)
		{
			result.skipped = true;
			result.checks.Add(new AwsCheck
			{
				name = "aws cli",
				ok = false,
				detail = "not found on PATH",
				remediation = "Install the AWS CLI v2 (https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html), " +
				              "then re-run `beam local setup --only aws`."
			});
			return result;
		}

		// 1. Base credentials. Everything else assumes a role FROM these, so a failure here makes the rest moot.
		var identity = Run(aws, "sts get-caller-identity --output json", null);
		if (!identity.ok)
		{
			result.checks.Add(new AwsCheck
			{
				name = "base credentials",
				ok = false,
				detail = Summarize(identity.output),
				remediation =
					"No usable AWS credentials. Configure the profile the backend reads (`default` unless " +
					"`aws.default.profile.name` says otherwise) with `aws configure` or `aws sso login`, or set " +
					"AWS_PROFILE to a working profile before running the stack."
			});
			return result; // nothing downstream can succeed
		}

		var arn = TryJsonValue(identity.output, "Arn");
		result.checks.Add(new AwsCheck { name = "base credentials", ok = true, detail = arn ?? "authenticated" });

		// 2. The rendered awsglobal.conf tells us which roles/buckets/secret to check. Without it we can still
		//    report the credential check above, which is the more common failure.
		var confPath = scalaDir == null
			? null
			: Path.Combine(scalaDir, "core", "src", "main", "resources", "awsglobal.conf");

		if (confPath == null || !File.Exists(confPath))
		{
			result.checks.Add(new AwsCheck
			{
				name = "awsglobal.conf",
				ok = false,
				detail = confPath == null ? "no BeamableBackend path given" : $"missing at {confPath}",
				remediation =
					"Run `beam local setup --only scala-config` first — the role ARNs, bucket names and secret id " +
					"to verify are read from that generated file."
			});
			return result;
		}

		// 3. Assume each role. This is the check that usually fails on a new machine: the credentials are fine, but
		//    the developer's IAM principal is not in the role's trust policy.
		string servicesCredentials = null;
		foreach (var (label, key) in RoleKeys)
		{
			var roleArn = ScalaLocalVarsService.ReadConfValue(confPath, key);
			if (string.IsNullOrWhiteSpace(roleArn))
			{
				result.checks.Add(new AwsCheck
				{
					name = $"assume role: {label}",
					ok = false,
					warning = true,
					detail = $"{key} is empty in awsglobal.conf",
					remediation = "Re-run `beam local setup --only scala-config --force` to re-render it from the GitHub `local` environment."
				});
				continue;
			}

			// Try the direct assume first (what the backend does from the base profile). If that is denied, retry
			// CHAINED through the services role: some roles trust the platform service role rather than individual
			// developers, and a chained success is a materially different answer from a flat failure — it means the
			// role is reachable, just not directly. Reporting them the same way would send a developer to ask for a
			// trust-policy change they do not need.
			var assumed = AssumeRole(aws, roleArn, null);
			var chained = false;
			if (!assumed.ok && servicesCredentials != null)
			{
				var viaServices = AssumeRole(aws, roleArn, servicesCredentials);
				if (viaServices.ok)
				{
					assumed = viaServices;
					chained = true;
				}
			}

			result.checks.Add(new AwsCheck
			{
				name = $"assume role: {label}",
				ok = assumed.ok,
				detail = chained ? $"{roleArn} (via the services role)" : roleArn,
				awsError = assumed.ok ? null : Summarize(assumed.output),
				remediation = assumed.ok
					? null
					: $"Your IAM principal ({arn}) cannot assume {roleArn}, directly or through the services role. " +
					  "An AWS administrator has to add it to that role's trust policy (sts:AssumeRole). This is the " +
					  "step a new developer usually needs."
			});

			if (assumed.ok && key == RoleKeys[0].key)
				servicesCredentials = assumed.output;
		}

		// 4. The JWT signing key, fetched the way `auth` fetches it: with the assumed role, not the base profile.
		var secretId = ScalaLocalVarsService.ReadConfValue(confPath, JwtSecretKey);
		var secretsRole = ScalaLocalVarsService.ReadConfValue(confPath, SecretsRoleKey);
		var secretsCredentials = servicesCredentials;
		if (secretsCredentials == null && !string.IsNullOrWhiteSpace(secretsRole))
		{
			var assumed = Run(aws, $"sts assume-role --role-arn {secretsRole} --role-session-name {SessionName} --output json", null);
			if (assumed.ok) secretsCredentials = assumed.output;
		}

		if (string.IsNullOrWhiteSpace(secretId))
		{
			result.checks.Add(new AwsCheck
			{
				name = "jwt signing key",
				ok = false,
				detail = $"{JwtSecretKey} is empty in awsglobal.conf",
				remediation = "Re-render the config (`beam local setup --only scala-config --force`); without this id `auth` cannot sign tokens."
			});
		}
		else
		{
			// get-secret-value, not describe-secret: GetSecretValue is the permission `auth` actually needs, and a
			// principal can be able to describe a secret it cannot read. The output is never captured into the
			// report — only the exit code is used — so the signing key does not reach a log or the console.
			var secret = Run(aws,
				$"secretsmanager get-secret-value --secret-id {secretId} --region {_region} --output json",
				secretsCredentials, redactOutput: true);

			result.checks.Add(new AwsCheck
			{
				name = "jwt signing key",
				ok = secret.ok,
				detail = $"{secretId} ({_region})",
				awsError = secret.ok ? null : secret.output,
				remediation = secret.ok
					? null
					: $"Cannot read the secret '{secretId}' in {_region} via the services role. Either the secret " +
					  "does not exist (an AWS administrator must create it) or the role lacks " +
					  "secretsmanager:GetSecretValue on it. Until this works the Scala `auth` service starts but " +
					  "cannot mint tokens, so no login to the local stack will succeed."
			});
		}

		// 5. Buckets. A missing one breaks a specific feature (content publish, trials, geolocation) rather than
		//    the whole stack, so these are warnings.
		foreach (var (label, key) in BucketKeys)
		{
			var bucket = ScalaLocalVarsService.ReadConfValue(confPath, key);
			if (string.IsNullOrWhiteSpace(bucket)) continue;

			var head = Run(aws, $"s3api head-bucket --bucket {bucket} --region {_region}", servicesCredentials);
			result.checks.Add(new AwsCheck
			{
				name = $"bucket: {label}",
				ok = head.ok,
				// `warning` describes how bad a FAILURE is, not the row's colour: a bucket this developer's realm
				// never touches should not block setup. Setting it unconditionally made PASSING rows render as
				// cautions in `validate`.
				warning = !head.ok,
				detail = bucket,
				awsError = head.ok ? null : Summarize(head.output),
				remediation = head.ok
					? null
					: $"Cannot reach s3://{bucket} with the services role. It must exist and the role needs " +
					  $"s3:ListBucket on it; '{label}' features will fail until then."
			});
		}

		// 6. The scheduler queue. Reported, NOT probed.
		//
		// There is no non-destructive way to verify the access that actually matters. The only SQS operation the
		// product performs is SendMessageBatch (the Loader enqueuing job executions) — testing it would publish a
		// real message to a queue shared with the whole dev environment. The receiving side is the deployed
		// BeamableScheduler.Dispatcher Lambda, which SQS invokes under its OWN execution role, so nothing local
		// reads this queue either.
		//
		// An earlier version called GetQueueAttributes as a cheap read and reported AccessDenied as
		// "scheduled jobs will not dispatch". That was a false alarm: nothing in the codebase calls
		// GetQueueAttributes, so being denied it says nothing about whether scheduling works — and it sent people
		// to ask an AWS administrator for a permission the product never uses.
		var queueUrl = ReadSchedulerQueueUrl(apiDir);
		if (!string.IsNullOrWhiteSpace(queueUrl))
		{
			result.checks.Add(new AwsCheck
			{
				name = "scheduler queue (not verified)",
				ok = true,
				detail = $"{queueUrl} — configured. Local scheduling needs sqs:SendMessageBatch on this queue for " +
				         "the services role; that cannot be probed without publishing to shared dev infrastructure, " +
				         "so it is not checked here."
			});
		}

		return result;
	}


	/// <summary>
	/// A setup guide for the AWS access the local stack needs, grounded in the values this checkout actually
	/// uses: the profile name the backend reads, and the role ARNs / secret id / buckets from the rendered
	/// <c>awsglobal.conf</c> when it is available.
	///
	/// Printed by <c>beam local setup</c> when an AWS check fails, because that is the moment someone needs it —
	/// and because the failure it explains (no credentials) produces a stack that looks completely healthy and
	/// simply cannot log in.
	/// </summary>
	public string BuildSetupGuide(string scalaDir)
	{
		var confPath = scalaDir == null
			? null
			: Path.Combine(scalaDir, "core", "src", "main", "resources", "awsglobal.conf");

		var haveConf = confPath != null && File.Exists(confPath);
		string Conf(string key) => haveConf ? ScalaLocalVarsService.ReadConfValue(confPath, key) : null;

		// The backend reads `aws.default.profile.name`, which is normally absent — and then defaults to "default".
		var profile = Conf("aws.default.profile.name") ?? "default";
		var secretId = Conf(JwtSecretKey) ?? "beamable.jwt.signingKey.local";

		var roles = RoleKeys
			.Select(r => (r.label, arn: Conf(r.key)))
			.Where(r => !string.IsNullOrWhiteSpace(r.arn))
			.ToList();

		var buckets = BucketKeys
			.Select(b => Conf(b.key))
			.Where(b => !string.IsNullOrWhiteSpace(b))
			.ToList();

		var text = new StringBuilder();
		text.AppendLine("AWS setup for the local stack");
		text.AppendLine("=============================");
		text.AppendLine();
		text.AppendLine("Real AWS access is required - there is no LocalStack in this stack. The Scala");
		text.AppendLine("`auth` service reads its JWT signing key from AWS Secrets Manager at runtime,");
		text.AppendLine("so without working credentials every service starts healthy and no one can");
		text.AppendLine("log in.");
		text.AppendLine();
		text.AppendLine("1. Install the AWS CLI v2");
		text.AppendLine("   https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html");
		text.AppendLine();
		text.AppendLine($"2. Configure the profile the backend reads: [{profile}]");
		text.AppendLine($"     aws configure --profile {profile}          # region: {DefaultRegion}");
		text.AppendLine("   Long-lived IAM user keys (AKIA...) are simplest - they do not expire.");
		text.AppendLine("   SSO works too (`aws sso login`), but you must refresh it when it lapses.");
		text.AppendLine("   The name comes from `aws.default.profile.name` in awsglobal.conf;");
		text.AppendLine("   unset means \"default\".");
		text.AppendLine();
		text.AppendLine("3. Get permission to assume the platform roles. This is the step a new");
		text.AppendLine("   developer usually needs an AWS administrator for: your IAM principal has");
		text.AppendLine("   to be in each role's trust policy (sts:AssumeRole).");
		if (roles.Count > 0)
		{
			foreach (var (label, arn) in roles) text.AppendLine($"     - {arn}   ({label})");
		}
		else
		{
			text.AppendLine("     (run `beam local setup --only scala-config` first - the ARNs come from");
			text.AppendLine("      the generated awsglobal.conf, which is not present yet)");
		}

		text.AppendLine();
		text.AppendLine("4. The services role also needs secretsmanager:GetSecretValue on");
		text.AppendLine($"   `{secretId}` in {DefaultRegion}. If the secret does not exist, an");
		text.AppendLine("   administrator has to create it - without it `auth` cannot mint tokens and");
		text.AppendLine("   no login to the local stack will succeed.");
		if (buckets.Count > 0)
		{
			text.AppendLine();
			text.AppendLine("5. Feature-specific (non-blocking): read access to these buckets -");
			text.AppendLine($"   {string.Join(", ", buckets)}");
		}

		text.AppendLine();
		text.AppendLine("Verify:  beam local validate --with-aws");
		text.AppendLine();
		text.AppendLine("Gotchas");
		text.AppendLine("-------");
		text.AppendLine("* AWS_PROFILE overrides the profile above. If it names MFA session");
		text.AppendLine("  credentials (ASIA...), they expire and every call fails with `ExpiredToken`");
		text.AppendLine("  - refresh them, or unset AWS_PROFILE to fall back to the long-lived keys.");
		text.AppendLine("* Never set AWS_PROFILE to an empty string: the CLI then looks for a profile");
		text.AppendLine("  literally named \"\" and fails with `The config profile () could not be found`.");
		text.AppendLine("* A role may be reachable only by CHAINING through the services role rather");
		text.AppendLine("  than directly. The preflight shows that as \"(via the services role)\" - that");
		text.AppendLine("  is fine, not a misconfiguration.");

		return text.ToString();
	}

	/// <summary>
	/// The scheduler's SQS queue URL from BeamableAPI's <c>appsettings.Local.json</c> (the environment
	/// <c>beam local up</c> runs those hosts under). Null when the file or key is absent.
	/// </summary>
	private static string ReadSchedulerQueueUrl(string apiDir)
	{
		if (string.IsNullOrWhiteSpace(apiDir)) return null;

		var path = Path.Combine(apiDir, "BeamableGateway", "appsettings.Local.json");
		if (!File.Exists(path)) return null;

		try
		{
			return (string)JObject.Parse(File.ReadAllText(path)).SelectToken("Scheduler.JobQueue.QueueUrl");
		}
		catch
		{
			return null; // a malformed settings file is not this check's problem to report
		}
	}

	/// <summary>Locates the <c>aws</c> executable on PATH.</summary>
	public static string FindAwsCli()
	{
		var exeName = OperatingSystem.IsWindows() ? "aws.exe" : "aws";
		foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			try
			{
				var candidate = Path.Combine(dir.Trim(), exeName);
				if (File.Exists(candidate)) return candidate;
			}
			catch
			{
				// unusable PATH entry
			}
		}

		return null;
	}

	private readonly record struct CliOutcome(bool ok, string output);

	/// <summary>
	/// Assumes a role, optionally from another role's credentials (chaining). Returns the raw
	/// <c>assume-role</c> response so the temporary credentials can be reused for the resource checks.
	/// </summary>
	private static CliOutcome AssumeRole(string aws, string roleArn, string fromCredentials) =>
		Run(aws, $"sts assume-role --role-arn {roleArn} --role-session-name {SessionName} --output json", fromCredentials);

	/// <summary>
	/// Runs an <c>aws</c> sub-command, optionally with temporary credentials taken from an <c>assume-role</c>
	/// response. When <paramref name="redactOutput"/> is set the output is discarded rather than returned, so a
	/// secret value can never reach a report or a log.
	/// </summary>
	private static CliOutcome Run(string aws, string arguments, string assumeRoleJson, bool redactOutput = false)
	{
		var psi = new ProcessStartInfo
		{
			FileName = aws,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		// Never let a half-configured pager or an inherited output format break the parsing.
		psi.Environment["AWS_PAGER"] = string.Empty;

		if (!string.IsNullOrWhiteSpace(assumeRoleJson))
		{
			var accessKey = TryJsonValue(assumeRoleJson, "AccessKeyId");
			var secretKey = TryJsonValue(assumeRoleJson, "SecretAccessKey");
			var sessionToken = TryJsonValue(assumeRoleJson, "SessionToken");
			if (accessKey != null && secretKey != null)
			{
				psi.Environment["AWS_ACCESS_KEY_ID"] = accessKey;
				psi.Environment["AWS_SECRET_ACCESS_KEY"] = secretKey;
				if (sessionToken != null) psi.Environment["AWS_SESSION_TOKEN"] = sessionToken;
				// REMOVE the profile variables rather than blanking them. An inherited AWS_PROFILE would take
				// precedence over the injected keys, but setting it to "" is worse than leaving it: the CLI then
				// looks for a profile literally named "" and every call dies with "The config profile () could not
				// be found" — which reads as a permissions failure and sends you after the wrong problem.
				psi.Environment.Remove("AWS_PROFILE");
				psi.Environment.Remove("AWS_DEFAULT_PROFILE");
			}
		}

		try
		{
			using var proc = Process.Start(psi);
			if (proc == null) return new CliOutcome(false, "could not start the aws cli");

			var stdout = proc.StandardOutput.ReadToEnd();
			var stderr = proc.StandardError.ReadToEnd();
			proc.WaitForExit();

			if (redactOutput)
				return new CliOutcome(proc.ExitCode == 0, proc.ExitCode == 0 ? null : Summarize(stderr));

			return new CliOutcome(proc.ExitCode == 0, proc.ExitCode == 0 ? stdout : stderr);
		}
		catch (Exception e)
		{
			return new CliOutcome(false, e.Message);
		}
	}

	/// <summary>
	/// Pulls a field out of an <c>assume-role</c> / <c>get-caller-identity</c> response. Credentials are nested
	/// under <c>Credentials</c>, identity fields are at the root, so both shapes are searched.
	/// </summary>
	private static string TryJsonValue(string json, string field)
	{
		if (string.IsNullOrWhiteSpace(json)) return null;

		try
		{
			var parsed = JObject.Parse(json);
			return (string)(parsed.SelectToken($"Credentials.{field}") ?? parsed.SelectToken(field));
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Condenses AWS CLI error output to something that fits in a table cell.</summary>
	private static string Summarize(string output)
	{
		if (string.IsNullOrWhiteSpace(output)) return "(no output)";

		var lines = output.Split('\n')
			.Select(l => l.Trim())
			.Where(l => l.Length > 0)
			.ToList();

		var text = lines.LastOrDefault(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))
		           ?? lines.FirstOrDefault()
		           ?? "(no output)";

		return text.Length > 240 ? text.Substring(0, 240) + "..." : text;
	}
}
