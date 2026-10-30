using Beamable.Common.Api;
using Beamable.Server;
using cli.Services;
using cli.Services.LocalStack;
using cli.Utils;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackNativeCommandArgs : CommandArgs
{
	public string action;
	public bool ifChanged;
	public string device;
}

public class LocalStackNativeCommandResult
{
	public string detail;
}

/// <summary>
/// What the native stacks' steps run: <c>native: android</c> and <c>native: ios</c> build the notification libraries
/// into the Unity, Unity.Web, React Native and Unreal packages; <c>rn: …</c> runs the React Native sample against the
/// stack's realm; <c>rn: emulator</c> starts the Android emulator. Hidden: a stack runs them. Every tool comes from the
/// toolchain (<c>beam local setup --native</c>), and the BeamableProduct checkout from
/// <see cref="ProductDirVariable"/>.
/// </summary>
public class LocalStackNativeCommand
	: AtomicCommand<LocalStackNativeCommandArgs, LocalStackNativeCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public override bool IsForInternalUse => true;

	public const string Android = "android", Ios = "ios", Rn = "rn", Emulator = "emulator";
	public const string AnyDevice = "any", EmulatorDevice = "emulator", PhoneDevice = "phone", Browser = "browser";

	/// <summary>The BeamableProduct checkout, which the native steps get from the stack.</summary>
	public const string ProductDirVariable = "BEAM_NATIVE_PRODUCT_DIR";

	/// <summary>Set by <c>beam local run</c> on every step when the stack runs with <c>:build</c>.</summary>
	public const string ForceBuildVariable = "BEAM_LOCAL_FORCE_BUILD";

	/// <summary>What <c>rn: emulator</c> prints once the emulator has booted; the step is ready on it.</summary>
	public const string EmulatorReady = "Android emulator is ready";

	private const string Aar = "beamable-notifications-release.aar";
	private const string NativeAndroid = "beam-native-mobile/NativeSources/Android";

	public override bool AutoLogOutput => false;

	public LocalStackNativeCommand() : base("native", "INTERNAL The native libraries and React Native sample steps of the native stacks")
	{
		IsHidden = true;
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("action", $"What to do: {Android}, {Ios}, {Rn} or {Emulator}"), (args, v) => args.action = v);
		AddOption(new Option<bool>("--if-changed", "With android: build only when the native sources have uncommitted changes (or the stack runs with :build)"),
			(args, v) => args.ifChanged = v);
		AddOption(new Option<string>("--device", () => AnyDevice, $"With rn: where the sample runs: {AnyDevice}, {EmulatorDevice}, {PhoneDevice} or {Browser}"),
			(args, v) => args.device = v);
	}

	public override async Task<LocalStackNativeCommandResult> GetResult(LocalStackNativeCommandArgs args)
	{
		var token = args.Lifecycle.CancellationToken;
		var product = Path.GetFullPath(Environment.GetEnvironmentVariable(ProductDirVariable) ?? string.Empty);
		if (!Directory.Exists(Path.Combine(product, "beam-native-mobile")))
			throw new CliException($"{ProductDirVariable} names no BeamableProduct checkout ('{product}'). The native steps get it from the stack; run them with `beam local run native-libs` or `rn-sample`.");

		var detail = args.action switch
		{
			Android => await BuildAndroid(product, args.ifChanged, token),
			Ios => await BuildIos(product, token),
			Rn => await RunSample(product, args.device, LocalServicesRun(args), token),
			Emulator => await RunEmulator(token),
			_ => throw new CliException($"Unknown action '{args.action}': use {Android}, {Ios}, {Rn} or {Emulator}.")
		};
		return new LocalStackNativeCommandResult { detail = detail };
	}

	// ----------------------------------------------------------------------------------
	// Libraries
	// ----------------------------------------------------------------------------------

	private static async Task<string> BuildAndroid(string product, bool ifChanged, CancellationToken token)
	{
		var forced = Environment.GetEnvironmentVariable(ForceBuildVariable) == "true";
		if (ifChanged && !forced && !HasUncommittedChanges(product, NativeAndroid))
		{
			const string skipped = "The native Android sources match the committed libraries, so they aren't rebuilt (`:build` rebuilds them).";
			Log.Information(skipped);
			return skipped;
		}

		var tools = Tools();
		var project = Path.Combine(product, NativeAndroid, "BeamableNotifications");

		// local.properties' sdk.dir wins over ANDROID_HOME, and a developer's may name another SDK: point it at the
		// toolchain's for the build and put theirs back after.
		var properties = Path.Combine(project, "local.properties");
		var original = File.Exists(properties) ? await File.ReadAllTextAsync(properties, token) : null;
		await File.WriteAllTextAsync(properties, $"sdk.dir={tools.androidSdk.Replace('\\', '/')}\n", token);
		try
		{
			await Run(ToolchainService.ExecutablePath(ToolchainPins.Gradle, tools.gradle),
				$"-p \"{project}\" :notifications:assembleRelease --no-daemon", project, AndroidEnvironment(tools), token);
		}
		finally
		{
			if (original == null) File.Delete(properties);
			else await File.WriteAllTextAsync(properties, original, CancellationToken.None);
		}

		var aar = Path.Combine(project, "notifications", "build", "outputs", "aar", "notifications-release.aar");
		if (!File.Exists(aar))
			throw new CliException($"The Android build produced no {aar}.");

		var mobile = Path.Combine(product, "beam-native-mobile");
		var targets = new[]
		{
			Path.Combine(mobile, "Unity", "Plugins", "Android"),
			Path.Combine(mobile, "Unity.Web", "Plugins", "Android"),
			Path.Combine(mobile, "ReactNative", "android", "libs"),
			Path.Combine(mobile, "Unreal", "ThirdParty", "Android"),
		};
		foreach (var target in targets)
		{
			Directory.CreateDirectory(target);
			File.Copy(aar, Path.Combine(target, Aar), overwrite: true);
			Log.Information($"{Aar} -> {target}");
		}

		BustGradleTransforms();
		return $"Built {Aar} into {targets.Length} packages.";
	}

	/// <summary>
	/// Gradle keeps an exploded copy of a loose .aar in its user home and doesn't re-explode it when the file is
	/// replaced under the same name, so the React Native app kept the old library's bytecode. Removing those transforms
	/// makes the next app build use the new one.
	/// </summary>
	private static void BustGradleTransforms()
	{
		var caches = Path.Combine(Environment.GetEnvironmentVariable("GRADLE_USER_HOME")
		                          ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gradle"), "caches");
		if (!Directory.Exists(caches)) return;

		foreach (var dir in Directory.EnumerateDirectories(caches, Path.GetFileNameWithoutExtension(Aar), SearchOption.AllDirectories).ToList())
		{
			// .../transforms/<hash>/transformed/beamable-notifications-release: the whole <hash> transform goes.
			var transform = Directory.GetParent(dir)?.Parent?.FullName;
			if (transform == null || !Directory.Exists(transform)) continue;
			try
			{
				Directory.Delete(transform, recursive: true);
				Log.Information($"Removed the stale Gradle transform {transform}");
			}
			catch (Exception e)
			{
				Log.Warning($"Couldn't remove the Gradle transform {transform}: {e.Message}");
			}
		}
	}

	private static async Task<string> BuildIos(string product, CancellationToken token)
	{
		var xcode = OperatingSystem.IsMacOS() ? StartProcessUtil.Run("xcodebuild", "-showsdks").WaitForResult() : null;
		if (xcode == null || xcode.exit != 0 || !xcode.stdout.Contains("iphoneos"))
		{
			const string skipped = "The iOS libraries build only on macOS with Xcode (not just its command-line tools); skipped.";
			Log.Information(skipped);
			return skipped;
		}

		var mobile = Path.Combine(product, "beam-native-mobile");
		var ios = Path.Combine(mobile, "NativeSources", "iOS", "BeamableNotifications");
		var build = Path.Combine(ios, "build");
		const string framework = "BeamableNotifications.xcframework";

		await Run("bash", $"\"{Path.Combine(ios, "scripts", "build-xcframework.sh")}\"", ios, new Dictionary<string, string>(), token);
		if (!Directory.Exists(Path.Combine(build, framework)))
			throw new CliException($"The iOS build produced no {Path.Combine(build, framework)}.");

		foreach (var target in new[]
		         {
			         Path.Combine(mobile, "Unity", "Plugins", "iOS"),
			         Path.Combine(mobile, "Unity.Web", "Plugins", "iOS"),
			         Path.Combine(mobile, "ReactNative", "ios"),
		         })
		{
			var copy = Path.Combine(target, framework);
			if (Directory.Exists(copy)) Directory.Delete(copy, recursive: true);
			CopyDirectory(Path.Combine(build, framework), copy);
			Log.Information($"{framework} -> {target}");
		}

		// Unreal needs a dynamic framework. Its script empties build/, so it runs after the static copies are out.
		await Run("bash", $"\"{Path.Combine(ios, "scripts", "build-xcframework-dynamic.sh")}\"", ios, new Dictionary<string, string>(), token);
		const string embedded = "BeamableNotifications.embeddedframework.zip";
		var unreal = Path.Combine(mobile, "Unreal", "ThirdParty");
		Directory.CreateDirectory(unreal);
		File.Copy(Path.Combine(build, embedded), Path.Combine(unreal, embedded), overwrite: true);
		Log.Information($"{embedded} -> {unreal}");
		return "Built the iOS frameworks into the Unity, Unity.Web, React Native and Unreal packages.";
	}

	// ----------------------------------------------------------------------------------
	// React Native sample
	// ----------------------------------------------------------------------------------

	/// <summary>True when the running stack has microservices of this workspace, started with <c>beam project run</c>.</summary>
	private static bool LocalServicesRun(LocalStackNativeCommandArgs args)
	{
		var state = LocalStackRunStateIO.Load(LocalStackCommand.ResolveRunStatePath(args.ConfigService, null));
		return state?.steps.Any(s => s.name.StartsWith(LocalStackTemplate.MicroservicePrefix, StringComparison.OrdinalIgnoreCase)) == true;
	}

	private static async Task<string> RunSample(string product, string device, bool localServices, CancellationToken token)
	{
		var host = Environment.GetEnvironmentVariable(ConfigService.EnvironmentVariableFor(ConfigService.CFG_JSON_FIELD_HOST));
		var cid = Environment.GetEnvironmentVariable(ConfigService.EnvironmentVariableFor(ConfigService.CFG_JSON_FIELD_CID));
		var pid = Environment.GetEnvironmentVariable(ConfigService.EnvironmentVariableFor(ConfigService.CFG_JSON_FIELD_PID));
		if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(pid))
			throw new CliException("The sample runs against the stack's realm, and the stack has no login to it yet.");

		// In a browser the sample needs only Node; on a device the whole Android toolchain.
		var tools = device == Browser ? ToolchainService.TryReadInstalled() ?? new LocalStackToolchain() : Tools();
		var sample = Path.Combine(product, "beam-native-mobile", "Samples", "ReactNative");
		var env = device == Browser ? NodeEnvironment(tools) : AndroidEnvironment(tools);
		// The sample's Metro config watches its file: packages from here (see its metro.config.js).
		env["BEAM_REPO_ROOT"] = product;
		await EnsureWebSdk(product, env, token);
		if (!SampleLinksUsable(sample, product))
			await Run(Npm(tools), "ci --no-audit --no-fund", sample, env, token);

		if (device == Browser)
		{
			WriteRealm(sample, host, cid, pid, RoutingKey(localServices));
			await Run(Npm(tools), "run web", sample, env, token);
			return "The sample ran in the browser.";
		}

		var serial = await WaitForDevice(tools, device, token);
		var onEmulator = serial.StartsWith("emulator-", StringComparison.Ordinal);
		var appHost = DeviceHost(host, onEmulator);
		WriteRealm(sample, appHost, cid, pid, RoutingKey(localServices));
		Log.Information($"Building the sample for {serial}, against {appHost} (cid {cid}, realm {pid}).");

		env["ANDROID_SERIAL"] = serial;
		WriteSampleLocalProperties(sample, tools);
		// --clean regenerates the native project: one generated from another path or realm builds the wrong app.
		// --no-bundler: a release build carries its JS, and Expo otherwise leaves Metro on 8081 after the step.
		await Run(Npm(tools), "run android:local:release -- --clean --no-bundler", sample, env, token);
		return $"The sample is installed on {serial}, against {appHost}.";
	}

	/// <summary>
	/// The sample's Android build gets the toolchain's SDK and, on Windows, its CMake: the one Gradle picks on its own
	/// ships ninja 1.10, which can't use paths over 260 characters, and the sample's native build nests that deep from
	/// any checkout path longer than about 30 characters ("build.ninja still dirty after 100 tries"). The sample's
	/// <c>--clean</c> keeps this file when it regenerates the native project.
	/// </summary>
	private static void WriteSampleLocalProperties(string sample, LocalStackToolchain tools)
	{
		var lines = new List<string> { $"sdk.dir={tools.androidSdk.Replace('\\', '/')}" };
		var cmake = Path.Combine(tools.androidSdk, "cmake", ToolchainPins.AndroidCmakeVersion);
		if (OperatingSystem.IsWindows() && Directory.Exists(cmake))
			lines.Add($"cmake.dir={cmake.Replace('\\', '/')}");

		var android = Path.Combine(sample, "android");
		Directory.CreateDirectory(android);
		File.WriteAllLines(Path.Combine(android, "local.properties"), lines);
	}

	/// <summary>
	/// The sample's installed packages link its file: dependencies (the SDK, the notifications package) by absolute
	/// path. Links made from another drive or folder resolve outside what Metro and Gradle see, so they're reinstalled.
	/// </summary>
	private static bool SampleLinksUsable(string sample, string product)
	{
		var sdk = new DirectoryInfo(Path.Combine(sample, "node_modules", "@beamable", "sdk"));
		if (!sdk.Exists) return false;
		var target = sdk.LinkTarget?.Replace(@"\\?\", string.Empty);
		return target == null || target.StartsWith(product.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// The backend's address as the device sees it: on the emulator this machine is 10.0.2.2, on a phone it's the LAN
	/// address. Other hosts (dev, staging, a LAN host already) stay as they are.
	/// </summary>
	public static string DeviceHost(string host, bool onEmulator)
	{
		if (!Uri.TryCreate(host, UriKind.Absolute, out var uri) || !(uri.IsLoopback || uri.Host == "localhost"))
			return host;

		var address = onEmulator ? "10.0.2.2" : LocalStackTargets.LanAddress()
			?? throw new CliException("No LAN address found for the phone to reach this machine's backend. Use `rn-sample:emulator`, or a stack against dev.");
		return new UriBuilder(uri) { Host = address }.Uri.ToString().TrimEnd('/');
	}

	/// <summary>
	/// Microservices started with <c>beam project run</c> bind behind this machine's routing key and answer only callers
	/// that send it; deployed ones bind without one, so then the sample must send none.
	/// </summary>
	private static string RoutingKey(bool localServices) =>
		localServices ? ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine() : null;

	/// <summary>
	/// The sample reads its realm from env.local (gitignored) when it names one, ahead of its committed
	/// <c>.beamable/config.beam.json</c>; see the sample's <c>app.config.js</c>. <c>BEAM_ROUTING_KEY</c> is written
	/// when <paramref name="routingKey"/> is set and removed otherwise. Other lines of the file are kept.
	/// </summary>
	public static void WriteRealm(string sample, string host, string cid, string pid, string routingKey)
	{
		var file = Path.Combine(sample, "env.local");
		var values = new Dictionary<string, string>
		{
			["BEAM_HOST"] = host, ["BEAM_CID"] = cid, ["BEAM_PID"] = pid, ["VITE_API_BASE"] = host, ["BEAM_ROUTING_KEY"] = routingKey
		};
		var lines = (File.Exists(file) ? File.ReadAllLines(file) : Array.Empty<string>())
			.Where(line => !values.Keys.Any(key => line.TrimStart().StartsWith(key + "=", StringComparison.Ordinal)))
			.Concat(values.Where(v => v.Value != null).Select(v => $"{v.Key}={v.Value}"));
		File.WriteAllLines(file, lines);
	}

	/// <summary>The sample links <c>@beamable/sdk</c> to this checkout's <c>web/</c>, whose <c>dist</c> isn't committed.</summary>
	private static async Task EnsureWebSdk(string product, Dictionary<string, string> env, CancellationToken token)
	{
		var web = Path.Combine(product, "web");
		var dist = Path.Combine(web, "dist");
		var newestSource = Directory.EnumerateFiles(Path.Combine(web, "src"), "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty().Max();
		var built = Directory.Exists(dist)
			? Directory.EnumerateFiles(dist, "*", SearchOption.AllDirectories).Select(File.GetLastWriteTimeUtc).DefaultIfEmpty().Max()
			: DateTime.MinValue;
		if (built >= newestSource) return;

		var pnpm = ToolchainService.TryReadInstalled()?.pnpm;
		var exe = pnpm == null ? "pnpm" : ToolchainService.ExecutablePath(ToolchainPins.Pnpm, pnpm);
		Log.Information("Building the Web SDK the sample links to (web/dist) ...");
		if (!Directory.Exists(Path.Combine(web, "node_modules")))
			await Run(exe, "install --frozen-lockfile", web, env, token);
		await Run(exe, "build", web, env, token);
	}

	/// <summary>The device to install on: a phone, the emulator, or (any) the only one connected. Waits for one to show up.</summary>
	private static async Task<string> WaitForDevice(LocalStackToolchain tools, string device, CancellationToken token)
	{
		var adb = Adb(tools);
		var deadline = DateTime.UtcNow.AddMinutes(5);
		var told = false;
		while (true)
		{
			var devices = StartProcessUtil.Run(adb, "devices").WaitForResult().stdout
				.Split('\n').Skip(1).Select(l => l.Trim().Split('\t')).Where(p => p.Length == 2 && p[1] == "device").Select(p => p[0]).ToList();
			var candidates = device switch
			{
				EmulatorDevice => devices.Where(d => d.StartsWith("emulator-", StringComparison.Ordinal)).ToList(),
				PhoneDevice => devices.Where(d => !d.StartsWith("emulator-", StringComparison.Ordinal)).ToList(),
				_ => devices
			};
			if (candidates.Count == 1) return candidates[0];
			if (candidates.Count > 1)
				throw new CliException($"Several Android devices are connected ({string.Join(", ", candidates)}). Disconnect the others, or use `rn-sample:emulator` / `rn-sample:device`.");
			if (DateTime.UtcNow > deadline)
				throw new CliException("No Android device showed up in 5 minutes.");

			if (!told)
			{
				Log.Information(device == EmulatorDevice
					? "Waiting for the emulator to come up ..."
					: "Waiting for an Android device: connect a phone with USB debugging on (Settings > Developer options), or pair it over Wi-Fi with `adb pair`.");
				told = true;
			}

			await Task.Delay(TimeSpan.FromSeconds(3), token);
		}
	}

	// ----------------------------------------------------------------------------------
	// Emulator
	// ----------------------------------------------------------------------------------

	private static async Task<string> RunEmulator(CancellationToken token)
	{
		var tools = Tools();
		var env = AndroidEnvironment(tools);
		var exe = ToolchainService.ExecutablePath(ToolchainPins.AndroidEmulator, tools.androidSdk);
		if (!File.Exists(exe))
			throw new CliException("The Android emulator isn't installed: `beam local setup --only android-emulator` installs it.");

		var emulator = StartProcessUtil.Run(exe, $"-avd {ToolchainPins.AndroidAvdName} -no-snapshot-save -no-boot-anim",
			environmentVariables: env, onStdout: Console.WriteLine, onStderr: Console.WriteLine);

		var adb = Adb(tools);
		var deadline = DateTime.UtcNow.AddMinutes(5);
		while (!emulator.Process.HasExited)
		{
			var booted = StartProcessUtil.Run(adb, "-e shell getprop sys.boot_completed").WaitForResult().stdout.Trim();
			if (booted == "1")
			{
				Console.WriteLine($"{EmulatorReady}: {ToolchainPins.AndroidAvdName}");
				break;
			}

			if (DateTime.UtcNow > deadline)
				throw new CliException("The emulator didn't finish booting in 5 minutes. Hardware acceleration may be off: see https://developer.android.com/studio/run/emulator-acceleration.");
			await Task.Delay(TimeSpan.FromSeconds(3), token);
		}

		// The step stays up as long as the emulator does; stopping the stack closes it.
		await emulator.Process.WaitForExitAsync(token);
		if (emulator.Process.ExitCode != 0)
			throw new CliException($"The emulator exited with code {emulator.Process.ExitCode}.");
		return "The emulator closed.";
	}

	// ----------------------------------------------------------------------------------

	private static LocalStackToolchain Tools()
	{
		var tools = ToolchainService.TryReadInstalled();
		if (tools?.jdk17 == null || tools.gradle == null || tools.androidSdk == null)
			throw new CliException("The native toolchain isn't installed: `beam local setup --native` installs JDK 17, Gradle and the Android SDK.");
		return tools;
	}

	private static Dictionary<string, string> AndroidEnvironment(LocalStackToolchain tools)
	{
		var env = ToolchainService.AndroidEnvironment(tools.androidSdk, tools.jdk17,
			ToolchainService.AndroidAvdHome(tools.dir ?? ToolchainService.ResolveDir(null)));
		// JDK 17 ahead of the stack's JDK 8, and adb and the toolchain's Node for the sample's own scripts.
		var path = new[]
			{
				ToolchainService.BinDir(ToolchainPins.Jdk17, tools.jdk17),
				Path.Combine(tools.androidSdk, "platform-tools"),
				tools.node == null ? null : ToolchainService.BinDir(ToolchainPins.Node, tools.node),
				// The sample's --clean builds the Web SDK with pnpm.
				tools.pnpm == null ? null : ToolchainService.BinDir(ToolchainPins.Pnpm, tools.pnpm),
			}
			.Where(p => p != null).Append(Environment.GetEnvironmentVariable("PATH"));
		env["PATH"] = string.Join(Path.PathSeparator, path);
		return env;
	}

	private static Dictionary<string, string> NodeEnvironment(LocalStackToolchain tools) => new()
	{
		["PATH"] = string.Join(Path.PathSeparator, new[] { tools.node == null ? null : ToolchainService.BinDir(ToolchainPins.Node, tools.node), Environment.GetEnvironmentVariable("PATH") }.Where(p => p != null)),
	};

	private static string Adb(LocalStackToolchain tools) =>
		Path.Combine(tools.androidSdk, "platform-tools", OperatingSystem.IsWindows() ? "adb.exe" : "adb");

	private static string Npm(LocalStackToolchain tools) =>
		tools.node == null ? "npm" : Path.Combine(ToolchainService.BinDir(ToolchainPins.Node, tools.node), OperatingSystem.IsWindows() ? "npm.cmd" : "npm");

	/// <summary>True when <paramref name="folder"/> (relative to the checkout) has uncommitted changes other than docs.</summary>
	private static bool HasUncommittedChanges(string product, string folder)
	{
		var status = StartProcessUtil.Run("git", $"status --porcelain -- \"{folder}\"", workingDirectoryPath: product).WaitForResult();
		return status.exit != 0 || status.stdout.Split('\n').Any(line =>
			!string.IsNullOrWhiteSpace(line) && !line.TrimEnd().TrimEnd('"').EndsWith(".md", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>Runs a tool to completion with its output in the step's log, failing the step on a non-zero exit.</summary>
	private static async Task Run(string exe, string arguments, string workingDirectory, Dictionary<string, string> env, CancellationToken token)
	{
		Log.Information($"> {Path.GetFileName(exe)} {arguments}");
		// The tool's own output goes to the step's log as is (Log would read braces in it as a message template).
		var process = StartProcessUtil.Run(exe, arguments, environmentVariables: env, workingDirectoryPath: workingDirectory,
			onStdout: Console.WriteLine, onStderr: Console.WriteLine);
		await using var _ = token.Register(() =>
		{
			try { process.Process.Kill(entireProcessTree: true); } catch { /* already gone */ }
		});
		await process.Process.WaitForExitAsync(token);
		if (process.Process.ExitCode != 0)
			throw new CliException($"{Path.GetFileName(exe)} {arguments} failed with exit code {process.Process.ExitCode}.");
	}

	private static void CopyDirectory(string source, string destination)
	{
		Directory.CreateDirectory(destination);
		foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
			Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, dir)));
		foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
			File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
	}
}
