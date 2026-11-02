using Newtonsoft.Json.Linq;

namespace cli.Services.LocalStack;

/// <summary>
/// The toolchain the local stack is built and run with: which tools, which versions, and where each
/// platform's archive comes from. <see cref="ToolchainService"/> downloads these into a private directory so
/// <c>beam local run</c> never depends on whatever happens to be installed on the machine.
///
/// Pinning is the whole point. Left to <c>PATH</c>, the stack picks up whatever is there and fails in ways
/// that look like product bugs: Maven running under a JDK 21 from an IDE bundle, a Node major the portal was
/// never built against, or a JDK 8 that is the x64 build running under emulation.
/// </summary>
public static class ToolchainPins
{
	/// <summary>Ids used by <c>--only</c>/<c>--skip</c> and as the sub-directory name under the toolchain dir.</summary>
	public const string Jdk = "jdk8";

	/// <inheritdoc cref="Jdk"/>
	public const string Maven = "maven";

	/// <inheritdoc cref="Jdk"/>
	public const string Dotnet = "dotnet";

	/// <inheritdoc cref="Jdk"/>
	public const string Node = "node";

	/// <inheritdoc cref="Jdk"/>
	public const string Pnpm = "pnpm";

	/// <summary>
	/// The native mobile toolchain, installed only with <c>beam local setup --native</c> (or by a stack that builds
	/// the native libraries): the Android Gradle plugin the native projects use needs JDK 17, and Gradle 8.2 can't
	/// run on anything newer.
	/// </summary>
	public const string Jdk17 = "jdk17";

	/// <inheritdoc cref="Jdk17"/>
	public const string Gradle = "gradle";

	/// <summary>The Android SDK: the command-line tools plus <see cref="AndroidSdkPackages"/>, in its own folder.</summary>
	public const string AndroidSdk = "android-sdk";

	/// <summary>
	/// The Android emulator, a system image and the <see cref="AndroidAvdName"/> device, added to
	/// <see cref="AndroidSdk"/>. Several GB, so only <c>rn-sample:emulator</c> (or <c>--only android-emulator</c>)
	/// installs it.
	/// </summary>
	public const string AndroidEmulator = "android-emulator";

	/// <summary>
	/// Non-tool setup ids, also accepted by <c>--only</c>/<c>--skip</c>: the Docker check, the settings file with
	/// its step catalogue, the agent skill, the generated BeamableBackend config files, the portal's .env.local, and
	/// the AWS preflight. They install nothing into the toolchain dir.
	/// </summary>
	public const string ScalaConfig = "scala-config";

	/// <inheritdoc cref="ScalaConfig"/>
	public const string PortalConfig = "portal-config";

	/// <inheritdoc cref="ScalaConfig"/>
	public const string Aws = "aws";

	/// <inheritdoc cref="ScalaConfig"/>
	public const string Docker = "docker";

	/// <inheritdoc cref="ScalaConfig"/>
	public const string Settings = "settings";

	/// <inheritdoc cref="ScalaConfig"/>
	public const string Skill = "skill";

	/// <summary>
	/// The tool ids, in install order (fast/independent first). <see cref="Pnpm"/> is LAST because it is installed
	/// by the toolchain's own npm, so Node has to be in place before it runs.
	/// </summary>
	public static readonly string[] ToolIds = { Jdk, Maven, Dotnet, Node, Pnpm };

	/// <summary>
	/// The native tool ids, in install order: the SDK's own tools run on <see cref="Jdk17"/>. Not part of
	/// <see cref="ToolIds"/>, so backend and web work never downloads them.
	/// </summary>
	public static readonly string[] NativeToolIds = { Jdk17, Gradle, AndroidSdk };

	/// <summary>Every id <c>--only</c>/<c>--skip</c> accepts, tools plus the non-tool setup steps.</summary>
	public static readonly string[] AllStepIds =
		{ Docker, Jdk, Maven, Dotnet, Node, Pnpm, Jdk17, Gradle, AndroidSdk, AndroidEmulator, Settings, Skill, ScalaConfig, PortalConfig, Aws };

	/// <summary>
	/// Maven version the Scala reactor is built with. 3.9.x is what BeamableBackend's README documents;
	/// the exact patch is pinned so a classpath cache built by one developer matches another's.
	/// </summary>
	public const string MavenVersion = "3.9.9";

	/// <summary>
	/// The .NET SDK the BeamableAPI hosts build with. Their TFM is <c>net10.0</c> and the repo carries no
	/// <c>global.json</c>, so without a pin the build silently follows whichever SDK is newest on the machine.
	/// </summary>
	public const string DotnetVersion = "10.0.100";

	/// <summary>
	/// The Node major the portal expects — its <c>Dockerfile</c> is <c>node:22-alpine</c> and
	/// <c>amplify.yml</c> does <c>nvm use 22</c>. The exact patch is resolved from the dist index at
	/// download time (see <see cref="ResolveNodeVersionAsync"/>) so the pin does not rot.
	/// </summary>
	public const string NodeMajor = "22";

	/// <summary>
	/// The pnpm version the web packages are built with. <c>@beamable/sdk</c> and <c>@beamable/portal-toolkit</c>
	/// both declare <c>"packageManager": "pnpm@10.8.0"</c>, and <c>web: build</c> shells out to <c>pnpm</c>
	/// to build them — so without it that step cannot run at all. BeamableProduct's
	/// README lists pnpm as a prereq; this installs it instead of assuming it.
	///
	/// Keep in step with the <c>packageManager</c> field in <c>web/package.json</c> and
	/// <c>beam-portal-toolkit/package.json</c> — pnpm itself refuses to run a project pinned to a different
	/// package manager, so a mismatch here fails loudly rather than silently.
	/// </summary>
	public const string PnpmVersion = "10.8.0";

	/// <summary>The Java feature version the Scala backend runs under (Scala 2.11 / JDK 8 source+target).</summary>
	public const string JavaFeatureVersion = "8";

	/// <summary>The Gradle the native Android project's wrapper pins (<c>gradle-wrapper.properties</c>).</summary>
	public const string GradleVersion = "8.2";

	/// <summary>
	/// The Android command-line tools build (<c>cmdline-tools;latest</c> 23.0). Google publishes only SHA-1 digests
	/// for these archives, in its SDK repository index; they're pinned here with the build so the download is
	/// checked against what Google listed for it.
	/// </summary>
	public const string AndroidCmdlineToolsBuild = "16111833";

	/// <summary>
	/// The SDK packages the native Android project builds with (<c>compileSdk 34</c>, <c>buildToolsVersion
	/// '34.0.0'</c>, matching Unity 2022.3's SDK), plus <c>platform-tools</c> for <c>adb</c>. On Windows also a CMake whose
	/// ninja handles long paths, for the React Native sample's native build (see <see cref="AndroidCmakeVersion"/>).
	/// </summary>
	public static string[] AndroidSdkPackages => OperatingSystem.IsWindows()
		? new[] { "platform-tools", "platforms;android-34", "build-tools;34.0.0", $"cmake;{AndroidCmakeVersion}" }
		: new[] { "platform-tools", "platforms;android-34", "build-tools;34.0.0" };

	/// <summary>
	/// The CMake the React Native sample builds its native code with on Windows: it ships ninja 1.12, the first that
	/// uses paths over 260 characters. The 3.22 Gradle picks by default fails from a longer checkout path.
	/// </summary>
	public const string AndroidCmakeVersion = "3.31.6";

	/// <summary>The emulator system image <see cref="AndroidEmulator"/> installs, for this machine's architecture.</summary>
	public static string AndroidSystemImage => $"system-images;android-34;google_apis;{(IsArm64 ? "arm64-v8a" : "x86_64")}";

	/// <summary>The emulator device the React Native sample runs on.</summary>
	public const string AndroidAvdName = "beam-rn";

	/// <summary>A resolved, verifiable download: a URL plus the SHA-256 the bytes must hash to.</summary>
	public class Download
	{
		public string url;
		public string fileName;

		/// <summary>Lower-case hex SHA-256. Null when the source publishes no checksum, which skips verification.</summary>
		public string sha256;

		/// <summary>Lower-case hex SHA-512, used where that is the only digest published (Maven).</summary>
		public string sha512;

		/// <summary>Lower-case hex SHA-1, used where that is the only digest published (the Android command-line tools).</summary>
		public string sha1;

		/// <summary>The version string recorded in <c>toolchain.json</c> and shown by <c>local validate</c>.</summary>
		public string version;
	}

	// ----------------------------------------------------------------------------------
	// Platform identity
	// ----------------------------------------------------------------------------------

	/// <summary>True on 64-bit ARM (Apple Silicon, arm64 Linux/Windows).</summary>
	public static bool IsArm64 =>
		System.Runtime.InteropServices.RuntimeInformation.OSArchitecture
			is System.Runtime.InteropServices.Architecture.Arm64;

	/// <summary>A short <c>os-arch</c> label used in error messages and cache keys.</summary>
	public static string PlatformLabel =>
		(OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux")
		+ "-" + (IsArm64 ? "arm64" : "x64");

	/// <summary>True when the current platform's archives are zip files rather than tarballs (Windows).</summary>
	public static bool UsesZip => OperatingSystem.IsWindows();

	// ----------------------------------------------------------------------------------
	// JDK 8
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// Resolves the JDK 8 download for this platform.
	///
	/// Everywhere except Apple Silicon this is Eclipse Temurin, via the Adoptium assets API (which publishes a
	/// SHA-256 alongside the link). On macOS arm64 it is Azul Zulu instead, because <b>Adoptium ships no
	/// aarch64 macOS build of JDK 8 at all</b> — its API 404s for that combination. Falling back to the x64
	/// build there "works", but every Scala service then runs under Rosetta translation, which is both slow and
	/// a difference from CI nobody expects to be there.
	/// </summary>
	public static async Task<Download> ResolveJdkAsync(HttpClient http, CancellationToken token)
	{
		if (OperatingSystem.IsMacOS() && IsArm64)
			return await ResolveZuluMacArm64Async(http, token);

		return await ResolveTemurinAsync(http, JavaFeatureVersion, token);
	}

	/// <summary>The newest Eclipse Temurin JDK 17 for this platform (Adoptium builds 17 for Apple Silicon too).</summary>
	public static Task<Download> ResolveJdk17Async(HttpClient http, CancellationToken token) =>
		ResolveTemurinAsync(http, "17", token);

	private static async Task<Download> ResolveTemurinAsync(HttpClient http, string feature, CancellationToken token)
	{
		var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "mac" : "linux";
		var arch = IsArm64 ? "aarch64" : "x64";
		var url = $"https://api.adoptium.net/v3/assets/latest/{feature}/hotspot" +
		          $"?os={os}&architecture={arch}&image_type=jdk";

		var json = await GetStringAsync(http, url, token);
		var asset = JArray.Parse(json).FirstOrDefault()
			?? throw new CliException(
				$"Adoptium has no JDK {feature} build for {PlatformLabel}. " +
				$"Install a JDK {feature} by hand and pass --java-path (or set BEAM_JAVA_HOME).");

		var package = asset["binary"]?["package"]
			?? throw new CliException($"Adoptium returned no package for JDK {feature} on {PlatformLabel}.");

		return new Download
		{
			url = (string)package["link"],
			fileName = (string)package["name"],
			sha256 = (string)package["checksum"],
			version = (string)asset["release_name"] ?? feature
		};
	}

	/// <summary>
	/// The Azul Zulu 8 macOS aarch64 build, resolved through Azul's metadata API. Two calls: the package list
	/// (newest GA) then that package's detail, which is where the SHA-256 lives.
	/// </summary>
	private static async Task<Download> ResolveZuluMacArm64Async(HttpClient http, CancellationToken token)
	{
		const string listUrl =
			"https://api.azul.com/metadata/v1/zulu/packages/?java_version=8&os=macos&arch=aarch64" +
			"&archive_type=tar.gz&java_package_type=jdk&javafx_bundled=false&latest=true&release_status=ga";

		var list = JArray.Parse(await GetStringAsync(http, listUrl, token));
		var package = list.FirstOrDefault()
			?? throw new CliException(
				"Azul published no GA Zulu 8 build for macOS aarch64, and Adoptium has none either. " +
				"Install a JDK 8 by hand and pass --java-path (or set BEAM_JAVA_HOME).");

		var uuid = (string)package["package_uuid"];
		var detail = JObject.Parse(await GetStringAsync(http,
			$"https://api.azul.com/metadata/v1/zulu/packages/{uuid}", token));

		var javaVersion = string.Join(".", (detail["java_version"] ?? package["java_version"])?
			.Select(v => (string)v) ?? Array.Empty<string>());

		return new Download
		{
			url = (string)(detail["download_url"] ?? package["download_url"]),
			fileName = (string)(detail["name"] ?? package["name"]),
			sha256 = (string)detail["sha256_hash"],
			version = string.IsNullOrEmpty(javaVersion) ? "8" : javaVersion
		};
	}

	// ----------------------------------------------------------------------------------
	// Maven
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// The pinned Maven binary distribution. Platform-independent (it is a jar launcher plus scripts), and
	/// Apache publishes only a <c>.sha512</c> beside it, so that is the digest used.
	/// </summary>
	public static async Task<Download> ResolveMavenAsync(HttpClient http, CancellationToken token)
	{
		var fileName = $"apache-maven-{MavenVersion}-bin.{(UsesZip ? "zip" : "tar.gz")}";
		var url = $"https://archive.apache.org/dist/maven/maven-3/{MavenVersion}/binaries/{fileName}";

		// The digest file is a bare hex string (sometimes with a trailing filename); take the first token.
		var sha = (await GetStringAsync(http, url + ".sha512", token) ?? string.Empty)
			.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.FirstOrDefault();

		return new Download { url = url, fileName = fileName, sha512 = sha?.ToLowerInvariant(), version = MavenVersion };
	}

	// ----------------------------------------------------------------------------------
	// Gradle and the Android command-line tools
	// ----------------------------------------------------------------------------------

	/// <summary>The pinned Gradle binary distribution: one zip for every platform, with a <c>.sha256</c> beside it.</summary>
	public static async Task<Download> ResolveGradleAsync(HttpClient http, CancellationToken token)
	{
		var fileName = $"gradle-{GradleVersion}-bin.zip";
		var url = $"https://services.gradle.org/distributions/{fileName}";
		var sha = (await GetStringAsync(http, url + ".sha256", token) ?? string.Empty).Trim();
		return new Download { url = url, fileName = fileName, sha256 = sha.ToLowerInvariant(), version = GradleVersion };
	}

	/// <summary>The pinned Android command-line tools for this platform, checked against Google's listed SHA-1.</summary>
	public static Download ResolveAndroidCmdlineTools()
	{
		var (platform, sha1) =
			OperatingSystem.IsWindows() ? ("win", "57d04f2d75eb8e8fffc5000a987e5de4b5a63e9d")
			: OperatingSystem.IsMacOS() && IsArm64 ? ("mac_arm64", "ad03dc49bfacfd52c110b14104ea548b8a07e830")
			: OperatingSystem.IsMacOS() ? ("mac_x86_64", "112cf9618794a997ff273537d55bee02c22abffe")
			: ("linux", "e025545c62a8e64c7559119566a569fb1dec5f60");
		var fileName = $"commandlinetools-{platform}-{AndroidCmdlineToolsBuild}_latest.zip";
		return new Download
		{
			url = $"https://dl.google.com/android/repository/{fileName}",
			fileName = fileName,
			sha1 = sha1,
			version = AndroidCmdlineToolsBuild
		};
	}

	// ----------------------------------------------------------------------------------
	// Node
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// The newest LTS release of <see cref="NodeMajor"/>, resolved from the dist index, with its SHA-256 taken
	/// from that release's <c>SHASUMS256.txt</c>. Resolved rather than hard-pinned so the patch level tracks
	/// upstream security releases while the major stays exactly what the portal is built against.
	/// </summary>
	public static async Task<Download> ResolveNodeAsync(HttpClient http, CancellationToken token)
	{
		var version = await ResolveNodeVersionAsync(http, token);
		var fileName = $"node-{version}-{NodePlatformSuffix()}.{(UsesZip ? "zip" : "tar.gz")}";
		var url = $"https://nodejs.org/dist/{version}/{fileName}";

		var shasums = await GetStringAsync(http, $"https://nodejs.org/dist/{version}/SHASUMS256.txt", token);
		var sha = shasums?
			.Split('\n')
			.Select(line => line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
			.FirstOrDefault(parts => parts.Length == 2 && parts[1] == fileName)?[0];

		return new Download { url = url, fileName = fileName, sha256 = sha?.ToLowerInvariant(), version = version };
	}

	/// <summary>The newest LTS <c>vMAJOR.x.y</c> tag for <see cref="NodeMajor"/> from the dist index.</summary>
	private static async Task<string> ResolveNodeVersionAsync(HttpClient http, CancellationToken token)
	{
		var index = JArray.Parse(await GetStringAsync(http, "https://nodejs.org/dist/index.json", token));
		// The index is newest-first. `lts` is the codename string for an LTS line and `false` otherwise.
		var release = index.FirstOrDefault(r =>
			((string)r["version"])?.StartsWith($"v{NodeMajor}.", StringComparison.Ordinal) == true
			&& r["lts"]?.Type != JTokenType.Boolean);

		return (string)release?["version"]
			?? throw new CliException($"nodejs.org lists no LTS release for Node {NodeMajor}.");
	}

	/// <summary>The <c>os-arch</c> fragment nodejs.org names its archives with.</summary>
	public static string NodePlatformSuffix()
	{
		if (OperatingSystem.IsWindows()) return IsArm64 ? "win-arm64" : "win-x64";
		if (OperatingSystem.IsMacOS()) return IsArm64 ? "darwin-arm64" : "darwin-x64";
		return IsArm64 ? "linux-arm64" : "linux-x64";
	}

	// ----------------------------------------------------------------------------------
	// .NET SDK
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// The official install script for the .NET SDK. Unlike the others this is not a plain archive: the script
	/// resolves the platform-correct build itself, so <see cref="ToolchainService"/> runs it with
	/// <c>--install-dir</c> pointed into the toolchain rather than downloading and extracting a tarball.
	/// </summary>
	public static string DotnetInstallScriptUrl =>
		OperatingSystem.IsWindows() ? "https://dot.net/v1/dotnet-install.ps1" : "https://dot.net/v1/dotnet-install.sh";

	/// <inheritdoc cref="DotnetInstallScriptUrl"/>
	public static string DotnetInstallScriptName => OperatingSystem.IsWindows() ? "dotnet-install.ps1" : "dotnet-install.sh";

	// ----------------------------------------------------------------------------------

	/// <summary>
	/// GETs a URL as text, turning a failure into a <see cref="CliException"/> that names the URL — a bare
	/// <c>HttpRequestException</c> from deep inside resolution reads as "setup broke" rather than "this
	/// upstream index was unreachable".
	/// </summary>
	private static async Task<string> GetStringAsync(HttpClient http, string url, CancellationToken token)
	{
		try
		{
			using var res = await http.GetAsync(url, token);
			if (!res.IsSuccessStatusCode)
				throw new CliException($"GET {url} returned {(int)res.StatusCode} {res.ReasonPhrase}.");

			return await res.Content.ReadAsStringAsync(token);
		}
		catch (CliException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new CliException($"Could not reach {url}: {e.Message}");
		}
	}
}
