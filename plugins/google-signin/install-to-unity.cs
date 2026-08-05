#!/usr/bin/env -S dotnet run
// Builds the Beamable Google Sign-In Android plugin and installs the artifact into the Unity
// package at client/Packages/com.beamable/Plugins/Android.
//
// A .NET file-based app: no .csproj, no restore, no NuGet packages. Run it directly.
//
//   dotnet run install-to-unity.cs                  build, verify, install
//   dotnet run install-to-unity.cs --verify-only    build and verify, do not install
//   dotnet run install-to-unity.cs -- --help        usage
//
// Note the `--`: `dotnet run` claims some option names for itself, `--help` among them, so anything
// it recognises has to be passed after a `--` separator to reach this script. `--verify-only` is not
// one of those names and works either way.
//
// On macOS/Linux it is also directly executable, thanks to the shebang above:
//
//   chmod +x install-to-unity.cs && ./install-to-unity.cs
//
// WHY AN .aar AND NOT A .jar
// The Unity package consumes googlesignin-release.aar, not a bare .jar. The .aar carries three
// things a .jar cannot: the AndroidManifest.xml that declares GoogleSignInActivity (without it the
// interactive sign-in Activity cannot start), the ProGuard consumer rules that keep the JNI entry
// points alive in minified release builds, and the uses-sdk floor. The filename also matters: Unity
// binds a .meta file to an asset by path, and the committed googlesignin-release.aar.meta holds the
// asset GUID plus the Android-only import settings. So this script always writes exactly that name,
// and never touches the .meta.

using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

// Everything this plugin ships must be Java 8 bytecode: the classes are dexed by the *consuming*
// project's D8, and a D8 from AGP 4.0.x cannot read anything newer. 52 is Java 8.
const int RequiredClassFileMajorVersion = 52;

// The JDK that runs the build - unrelated to the bytecode above, which is always Java 8.
//
// The floor is AGP: 7.4.2 refuses to run on anything older than Java 11, and fails inside Gradle with
// an unhelpful message rather than up front. The ceiling is Gradle: 7.6.4 cannot run on Java 20+ at
// all ("Unsupported class file major version"). 11-17 is also exactly the window Unity's bundled JDKs
// occupy - 11 for 2021.3-2022.3, 17 for Unity 6 - so the Unity-paired JDK always qualifies.
const int MinimumJdkMajorVersion = 11;
const int MaximumJdkMajorVersion = 17;

// The static methods GoogleSignIn.cs / GoogleSignInService.cs call by name through JNI. If R8 or a
// refactor drops one of these, the failure only shows up at runtime on a device.
string[] requiredMethods =
[
    "login", "silentLogin", "signOut", "revokeAccess", "getPluginVersion"
];

// `args` is provided implicitly by top-level statements, and already excludes the script path.
if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
        Builds plugins/google-signin and installs googlesignin-release.aar into
        client/Packages/com.beamable/Plugins/Android.

          --verify-only   build and run the checks, but do not copy into the Unity package
          --help, -h      show this

        JAVA_HOME and ANDROID_HOME are used when set; otherwise a Unity-bundled JDK and
        Android SDK are located automatically.

        The build needs a JDK between 11 and 17. A JAVA_HOME outside that range is
        reported and skipped in favour of a Unity-bundled JDK.
        """);
    return 0;
}

var verifyOnly = args.Contains("--verify-only");

// ---------------------------------------------------------------------------- paths

// CallerFilePath is the path of this script, which makes every other path relative to it rather
// than to whatever directory the script was invoked from.
static string ThisScript([CallerFilePath] string path = "") => path;

var pluginDir = new DirectoryInfo(Path.GetDirectoryName(ThisScript())!);
var repoRoot = pluginDir.Parent?.Parent
    ?? throw new InvalidOperationException($"Cannot find the repository root above {pluginDir}.");

var unityPluginDir = Path.Combine(repoRoot.FullName,
    "client", "Packages", "com.beamable", "Plugins", "Android");
var aarName = "googlesignin-release.aar";
var builtAar = Path.Combine(pluginDir.FullName, "googlesignin", "build", "outputs", "aar", aarName);
var installedAar = Path.Combine(unityPluginDir, aarName);
var installedMeta = installedAar + ".meta";

Require(File.Exists(Path.Combine(pluginDir.FullName, "settings.gradle")),
    $"{pluginDir} does not look like the Gradle project: settings.gradle is missing.");
Require(Directory.Exists(unityPluginDir),
    $"The Unity plugin directory is missing: {unityPluginDir}");

var moduleBuildGradle = Path.Combine(pluginDir.FullName, "googlesignin", "build.gradle");
var pluginVersion = ReadPluginVersion(moduleBuildGradle);

// Read the toolchain the build actually pins, rather than assuming it. An Android SDK is only
// usable here if it has both of these, and Unity editors differ: the 2022.3 SDK ships build-tools
// 34.0.0 while the Unity 6 SDK ships 36.0.0, so "newest editor wins" picks an SDK that cannot
// build this module without a licensed download.
var requiredPlatform = ReadGradleValue(moduleBuildGradle, @"compileSdk\s+(\d+)");
var requiredBuildTools = ReadGradleValue(moduleBuildGradle, @"buildToolsVersion\s+['""]([^'""]+)['""]");

// Only used to explain the JDK requirement, since these two are what impose it. Read rather than
// hardcoded, so the message cannot drift away from the pins it describes.
var agpVersion = ReadGradleValue(Path.Combine(pluginDir.FullName, "build.gradle"),
    @"com\.android\.tools\.build:gradle:([\d.]+)");
var gradleVersion = ReadGradleValue(
    Path.Combine(pluginDir.FullName, "gradle", "wrapper", "gradle-wrapper.properties"),
    @"gradle-([\d.]+)-bin\.zip");

var jdkRequirement = $"JDK {MinimumJdkMajorVersion} to {MaximumJdkMajorVersion} is required" +
    (string.IsNullOrEmpty(agpVersion) && string.IsNullOrEmpty(gradleVersion)
        ? "."
        : $": AGP {Quote(agpVersion)} does not run on an older JDK, and Gradle {Quote(gradleVersion)} " +
          $"cannot run on a newer one.");

Console.WriteLine($"Beamable Google Sign-In plugin {pluginVersion}");
Console.WriteLine($"  plugin:  {pluginDir.FullName}");
Console.WriteLine($"  install: {(verifyOnly ? "(skipped, --verify-only)" : unityPluginDir)}");

// ---------------------------------------------------------------- toolchain discovery

// SDK first, then the JDK bundled beside it. Pairing them keeps the toolchain coherent and the
// build reproducible: javac 11 and javac 17 both emit valid Java 8 bytecode, but not byte-identical
// class files, so mixing editors would make the committed .aar churn for no source change.
var androidHome = ResolveAndroidHome(requiredPlatform, requiredBuildTools);
var jdk = ResolveJavaHome(androidHome, MinimumJdkMajorVersion, MaximumJdkMajorVersion, jdkRequirement);
var javaHome = jdk.Path;

Console.WriteLine($"  JDK:     {javaHome} (JDK {jdk.Major})");
Console.WriteLine($"  SDK:     {androidHome}");
Console.WriteLine($"           (needs platform android-{requiredPlatform}, build-tools {requiredBuildTools})");
Console.WriteLine();

// ------------------------------------------------------------------------------ build

if (!RunGradle(":googlesignin:assembleRelease"))
{
    return Fail("The Gradle build failed. See the output above.");
}

Require(File.Exists(builtAar), $"Gradle reported success but produced no .aar at {builtAar}");

// ----------------------------------------------------------------------------- verify

Console.WriteLine();
Console.WriteLine("Verifying the .aar");

var problems = VerifyAar(builtAar, javaHome, requiredMethods, RequiredClassFileMajorVersion);

foreach (var (ok, description) in problems)
{
    Console.WriteLine($"  {(ok ? "ok  " : "FAIL")}  {description}");
}

if (problems.Any(p => !p.Ok))
{
    return Fail("The .aar failed verification and was NOT installed.");
}

// ---------------------------------------------------------------------------- install

if (verifyOnly)
{
    Console.WriteLine();
    Console.WriteLine($"Verified. Not installed (--verify-only). Artifact: {builtAar}");
    return 0;
}

// The .meta is Unity's record of this asset's GUID and its Android-only import settings. It is not
// ours to rewrite: copy over the .aar only, and prove afterwards that the .meta is byte-identical.
var metaBefore = File.Exists(installedMeta) ? File.ReadAllBytes(installedMeta) : null;

if (metaBefore is null)
{
    Console.WriteLine();
    Console.WriteLine($"  note  {Path.GetFileName(installedMeta)} does not exist yet; Unity will " +
                      "generate one on its next import. Commit it along with the .aar.");
}

File.Copy(builtAar, installedAar, overwrite: true);

if (metaBefore is not null)
{
    var metaAfter = File.ReadAllBytes(installedMeta);
    Require(metaBefore.AsSpan().SequenceEqual(metaAfter),
        $"{Path.GetFileName(installedMeta)} changed during the copy. The asset GUID may have been " +
        "regenerated, which is a breaking change for every consuming project.");
}

Console.WriteLine();
Console.WriteLine($"Installed {aarName} ({new FileInfo(installedAar).Length:N0} bytes) into the Unity package.");
Console.WriteLine("Commit the .aar together with the plugin source change - nothing rebuilds it automatically.");
return 0;

// ============================================================================ helpers

bool RunGradle(string task)
{
    var onWindows = OperatingSystem.IsWindows();

    // gradlew.bat is a batch file, which cannot be started as a process directly.
    var startInfo = new ProcessStartInfo
    {
        FileName = onWindows ? "cmd.exe" : Path.Combine(pluginDir.FullName, "gradlew"),
        WorkingDirectory = pluginDir.FullName,
        UseShellExecute = false,
    };

    if (onWindows)
    {
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("gradlew.bat");
    }

    startInfo.ArgumentList.Add(task);

    startInfo.Environment["JAVA_HOME"] = javaHome;
    startInfo.Environment["ANDROID_HOME"] = androidHome;
    // Gradle reads ANDROID_SDK_ROOT in some versions and ANDROID_HOME in others; set both.
    startInfo.Environment["ANDROID_SDK_ROOT"] = androidHome;

    Console.WriteLine($"$ ./gradlew {task}");

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start Gradle.");
    process.WaitForExit();

    return process.ExitCode == 0;
}

static List<(bool Ok, string Description)> VerifyAar(
    string aarPath, string javaHome, string[] requiredMethods, int requiredMajorVersion)
{
    var results = new List<(bool, string)>();

    using var aar = ZipFile.OpenRead(aarPath);

    // 1. The consuming app's minSdk must be allowed to be as low as it has always been. AGP writes
    //    uses-sdk into the .aar manifest and the merger enforces library <= app, so raising this
    //    breaks every customer below the new floor.
    var manifestEntry = aar.GetEntry("AndroidManifest.xml");
    var manifest = manifestEntry is null ? "" : new StreamReader(manifestEntry.Open()).ReadToEnd();
    var minSdk = Regex.Match(manifest, @"minSdkVersion=""(\d+)""").Groups[1].Value;
    results.Add((minSdk == "16", $"minSdkVersion is 16 (found: {Quote(minSdk)})"));

    // 2. The package name survived the manifest `package` -> gradle `namespace` move.
    results.Add((manifest.Contains("com.beamable.googlesignin.GoogleSignInActivity"),
        "the manifest declares com.beamable.googlesignin.GoogleSignInActivity"));

    // 3. The ProGuard consumer rules shipped, so a minified consumer keeps the JNI entry points.
    var proguardEntry = aar.GetEntry("proguard.txt");
    var proguard = proguardEntry is null ? "" : new StreamReader(proguardEntry.Open()).ReadToEnd();
    results.Add((proguard.Contains("com.beamable.googlesignin"),
        "proguard.txt keeps com.beamable.googlesignin.**"));

    // 4. Bytecode level and lambda check, read straight out of the nested classes.jar.
    var classesEntry = aar.GetEntry("classes.jar");
    if (classesEntry is null)
    {
        results.Add((false, "classes.jar is present"));
        return results;
    }

    using var classesBuffer = new MemoryStream();
    using (var classesStream = classesEntry.Open())
    {
        classesStream.CopyTo(classesBuffer);
    }

    classesBuffer.Position = 0;
    using var classes = new ZipArchive(classesBuffer, ZipArchiveMode.Read);

    var classEntries = classes.Entries.Where(e => e.FullName.EndsWith(".class")).ToList();
    var wrongVersion = new List<string>();
    var header = new byte[8];

    foreach (var entry in classEntries)
    {
        using var stream = entry.Open();
        if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length)
        {
            wrongVersion.Add($"{entry.Name} (too short to be a class file)");
            continue;
        }

        // 0xCAFEBABE, then u2 minor, then u2 major - both big-endian.
        var major = (header[6] << 8) | header[7];
        if (major != requiredMajorVersion)
        {
            wrongVersion.Add($"{entry.Name} (major {major})");
        }
    }

    results.Add((classEntries.Count > 0 && wrongVersion.Count == 0,
        $"all {classEntries.Count} classes are Java 8 bytecode, major {requiredMajorVersion}" +
        (wrongVersion.Count > 0 ? $" - offenders: {string.Join(", ", wrongVersion)}" : "")));

    // Lambdas compile to invokedynamic at -target 8, which relies on the consumer's desugaring.
    var lambdas = classes.Entries.Where(e => e.FullName.Contains("$$Lambda")).ToList();
    results.Add((lambdas.Count == 0,
        "no lambda classes" + (lambdas.Count > 0 ? $" - found {lambdas.Count}" : "")));

    // Nothing from Unity may be packaged; unity-classes.jar is compileOnly.
    var unityLeak = classes.Entries.Where(e => e.FullName.StartsWith("com/unity3d/")).ToList();
    results.Add((unityLeak.Count == 0,
        "no Unity classes packaged" + (unityLeak.Count > 0 ? $" - found {unityLeak.Count}" : "")));

    // 5. The public surface C# calls by name, via javap on the extracted classes.
    var extractDir = Path.Combine(Path.GetTempPath(), "beamable-googlesignin-verify-" + Guid.NewGuid().ToString("N")[..8]);
    try
    {
        Directory.CreateDirectory(extractDir);
        classesBuffer.Position = 0;
        using (var forExtract = new ZipArchive(classesBuffer, ZipArchiveMode.Read))
        {
            forExtract.ExtractToDirectory(extractDir);
        }

        var javap = Path.Combine(javaHome, "bin", OperatingSystem.IsWindows() ? "javap.exe" : "javap");
        if (!File.Exists(javap))
        {
            results.Add((false, $"javap is available at {javap}"));
        }
        else
        {
            var disassembly = Capture(javap,
                ["-classpath", extractDir, "com.beamable.googlesignin.GoogleSignInActivity"]);

            var missing = requiredMethods
                .Where(m => !disassembly.Contains($" {m}("))
                .ToList();

            results.Add((missing.Count == 0,
                $"GoogleSignInActivity exposes {string.Join(", ", requiredMethods)}" +
                (missing.Count > 0 ? $" - missing: {string.Join(", ", missing)}" : "")));

            // A GMS type in a signature would move a missing-dependency failure from the method
            // body (catchable, reported to C#) to method resolution (kills the player).
            var gmsInSignature = Regex.Matches(disassembly, @"public static.*com\.google\.android\.gms").Count;
            results.Add((gmsInSignature == 0,
                "no Google Play services types in the public signatures"));
        }
    }
    finally
    {
        try { Directory.Delete(extractDir, recursive: true); } catch { /* best effort */ }
    }

    return results;
}

static string Capture(string fileName, string[] arguments)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
    process.WaitForExit();
    return output;
}

static string ReadPluginVersion(string moduleBuildGradle) =>
    ReadGradleValue(moduleBuildGradle, @"pluginVersion\s*=\s*['""]([^'""]+)['""]") is { Length: > 0 } version
        ? version
        : "unknown version";

static string ReadGradleValue(string buildGradle, string pattern)
{
    if (!File.Exists(buildGradle))
    {
        return "";
    }

    var match = Regex.Match(File.ReadAllText(buildGradle), pattern);
    return match.Success ? match.Groups[1].Value : "";
}

/// <summary>
/// Find a JDK the build can actually run on, in order of preference, and prove its version.
/// </summary>
/// <remarks>
/// Unlike <c>ResolveAndroidHome</c>, an explicit JAVA_HOME is *not* respected as-is: Gradle will
/// happily download a missing Android SDK component, but it cannot fix a JDK that AGP refuses to run
/// on - it just fails deep in the build with a message that does not mention Java. So every candidate
/// is version-checked here, and one that does not qualify is reported and skipped rather than handed
/// to Gradle. Skipping is loud on purpose: silently overriding an explicit JAVA_HOME would be its own
/// kind of surprise.
/// </remarks>
static (string Path, int Major) ResolveJavaHome(
    string androidHome, int minimumMajor, int maximumMajor, string requirement)
{
    var candidates = new List<(string Path, string Source)>();

    var fromEnvironment = Environment.GetEnvironmentVariable("JAVA_HOME");
    if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
    {
        candidates.Add((fromEnvironment, "JAVA_HOME"));
    }

    // The Unity-bundled JDK means nobody has to install one to build this plugin. Prefer the one
    // sitting next to the Android SDK we chose, so the two come from the same editor install.
    var sibling = Path.Combine(Path.GetDirectoryName(androidHome.TrimEnd(Path.DirectorySeparatorChar)) ?? "",
                               "OpenJDK");
    if (Directory.Exists(sibling))
    {
        candidates.Add((sibling, "the JDK bundled beside the chosen Android SDK"));
    }

    // Any other editor's JDK, newest first. Worth checking even though the list is ordered
    // lexicographically rather than by version: an editor bundling a JDK outside the supported range
    // is now skipped instead of selected and then failing inside Gradle.
    foreach (var other in UnityAndroidPlayerDirectories()
                 .Select(d => Path.Combine(d, "OpenJDK"))
                 .Where(Directory.Exists))
    {
        candidates.Add((other, "a Unity-bundled JDK"));
    }

    var rejected = new List<string>();

    foreach (var (path, source) in candidates.DistinctBy(c => c.Path))
    {
        var major = JdkMajorVersion(path);

        if (major >= minimumMajor && major <= maximumMajor)
        {
            return (path, major.Value);
        }

        var found = major.HasValue ? $"JDK {major}" : "no usable JDK";
        rejected.Add($"{source}: {path} ({found})");

        Console.WriteLine($"  note  skipping {source} - {found} at {path}. This build needs " +
                          $"JDK {minimumMajor}-{maximumMajor}.");
    }

    throw new InvalidOperationException(
        $"No supported JDK found. {requirement}\n" +
        (rejected.Count == 0
            ? "  nothing was found to check."
            : string.Join("\n", rejected.Select(r => $"  rejected {r}"))) +
        $"\nSet JAVA_HOME to a JDK {minimumMajor} or {maximumMajor}, or install a Unity editor with " +
        "Android support (its bundled JDK is used automatically).");
}

/// <summary>
/// The major version of the JDK installed at <paramref name="javaHome"/>, or null if that is not a
/// usable JDK.
/// </summary>
/// <remarks>
/// The `release` file is part of every JDK 9+ layout, so the common case costs no process launch.
/// The fallback exists for JDK 8, which has no `release` file on some builds and which is precisely
/// the version this check has to catch.
/// </remarks>
static int? JdkMajorVersion(string javaHome)
{
    var releaseFile = Path.Combine(javaHome, "release");
    if (File.Exists(releaseFile))
    {
        var declared = Regex.Match(File.ReadAllText(releaseFile), @"JAVA_VERSION=""([^""]+)""");
        if (declared.Success && TryParseJavaMajor(declared.Groups[1].Value, out var fromRelease))
        {
            return fromRelease;
        }
    }

    var java = Path.Combine(javaHome, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
    if (!File.Exists(java))
    {
        // Not a JDK at all - an empty directory, or a JRE-only layout that cannot compile anything.
        return null;
    }

    // `java -version` writes its banner to stderr on JDK 8 and to stdout on newer JDKs; Capture
    // concatenates both, so one regex covers either.
    var banner = Capture(java, ["-version"]);
    var reported = Regex.Match(banner, @"version\s+""([^""]+)""");

    return reported.Success && TryParseJavaMajor(reported.Groups[1].Value, out var fromBanner)
        ? fromBanner
        : null;
}

/// <summary>
/// Parse a Java version string into its major version: "1.8.0_402" is 8, "11.0.20" is 11, "17" is 17,
/// "21.0.2+13" is 21.
/// </summary>
static bool TryParseJavaMajor(string version, out int major)
{
    // The legacy "1.x" scheme puts the major version second; everything since Java 9 puts it first.
    var match = Regex.Match(version, @"^(?:1\.(\d+)|(\d+))");
    var digits = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

    return int.TryParse(digits, out major);
}

static string ResolveAndroidHome(string requiredPlatform, string requiredBuildTools)
{
    foreach (var name in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
        {
            // An explicit choice is respected as-is; Gradle will download what it lacks.
            return fromEnvironment;
        }
    }

    var candidates = UnityAndroidPlayerDirectories()
        .Select(d => Path.Combine(d, "SDK"))
        .Concat(DefaultAndroidSdkLocations())
        .Where(Directory.Exists)
        .ToList();

    if (candidates.Count == 0)
    {
        throw new InvalidOperationException(
            "No Android SDK found. Set ANDROID_HOME, or install a Unity editor with Android support.");
    }

    bool HasPlatform(string sdk) => string.IsNullOrEmpty(requiredPlatform)
        || Directory.Exists(Path.Combine(sdk, "platforms", $"android-{requiredPlatform}"));

    bool HasBuildTools(string sdk) => string.IsNullOrEmpty(requiredBuildTools)
        || Directory.Exists(Path.Combine(sdk, "build-tools", requiredBuildTools));

    // Anything missing the pinned build-tools makes Gradle try to download them, which fails on an
    // unlicensed SDK - so an SDK that has both is strongly preferred over a merely newer one.
    return candidates.FirstOrDefault(sdk => HasPlatform(sdk) && HasBuildTools(sdk))
        ?? candidates.FirstOrDefault(HasPlatform)
        ?? candidates[0];
}

static IEnumerable<string> UnityAndroidPlayerDirectories()
{
    var hubRoots = new List<string>();

    if (OperatingSystem.IsMacOS())
    {
        hubRoots.Add("/Applications/Unity/Hub/Editor");
    }
    else if (OperatingSystem.IsWindows())
    {
        hubRoots.Add(@"C:\Program Files\Unity\Hub\Editor");
        var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrEmpty(programFiles))
        {
            hubRoots.Add(Path.Combine(programFiles, "Unity", "Hub", "Editor"));
        }
    }
    else
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        hubRoots.Add(Path.Combine(home, "Unity", "Hub", "Editor"));
    }

    foreach (var hubRoot in hubRoots.Where(Directory.Exists))
    {
        // Newest editor first, so a fresher toolchain wins.
        foreach (var editor in Directory.GetDirectories(hubRoot).OrderDescending())
        {
            var playbackEngines = OperatingSystem.IsMacOS()
                ? Path.Combine(editor, "PlaybackEngines", "AndroidPlayer")
                : Path.Combine(editor, "Editor", "Data", "PlaybackEngines", "AndroidPlayer");

            if (Directory.Exists(playbackEngines))
            {
                yield return playbackEngines;
            }
        }
    }
}

static IEnumerable<string> DefaultAndroidSdkLocations()
{
    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    if (OperatingSystem.IsMacOS())
    {
        yield return Path.Combine(home, "Library", "Android", "sdk");
    }
    else if (OperatingSystem.IsWindows())
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(localAppData, "Android", "Sdk");
    }
    else
    {
        yield return Path.Combine(home, "Android", "Sdk");
    }
}

static string Quote(string value) => string.IsNullOrEmpty(value) ? "none" : value;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static int Fail(string message)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"error: {message}");
    return 1;
}
