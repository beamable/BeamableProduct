using CliWrap;
using System.CommandLine;
using System.Text;
using Beamable.Server;

namespace cli;

/// <summary>
/// A custom location for a Java 8 <c>JAVA_HOME</c> (the directory whose <c>bin/java</c> runs the Scala
/// backend). Mirrors <see cref="DockerPathOption"/>: the CLI resolves it automatically from the usual
/// install locations, and it can be overridden via the option or the <c>BEAM_JAVA_HOME</c> env var.
/// The local-stack manifest substitutes the resolved value into the <c>${java}</c> token.
/// </summary>
public class JavaPathOption : Option<string>
{
	public static readonly JavaPathOption Instance = new JavaPathOption();

	private JavaPathOption() : base(
		name: "--java-path",
		description: "a custom location for the Java 8 home used to run the local Scala backend. By default, the CLI " +
		             $"will attempt to resolve Java through its usual install locations. You can also use the {ConfigService.ENV_VAR_JAVA_HOME} " +
		             "environment variable to specify.")
	{
		if (TryGetJavaHome(out var javaHome, out _))
		{
			Description += "\nCurrently, a Java 8 home has been automatically identified.";
			SetDefaultValue(javaHome);
		}
		else if (!string.IsNullOrEmpty(ConfigService.CustomJavaHome))
		{
			SetDefaultValue(ConfigService.CustomJavaHome);
		}
		else
		{
			Description += "\nCurrently, no Java 8 home is available; set this option (or install a JDK 8) to run the Scala backend.";
		}
	}

	/// <summary>
	/// Resolves a Java 8 <c>JAVA_HOME</c> directory. Search order: <c>BEAM_JAVA_HOME</c> → <c>JAVA_HOME</c> →
	/// macOS <c>/usr/libexec/java_home -v 1.8</c> → common Linux/macOS JDK dirs → common Windows dirs.
	/// An explicit override (env var) is trusted even if its version can't be confirmed; auto-discovered
	/// candidates must report Java 8.
	/// </summary>
	public static bool TryGetJavaHome(out string javaHome, out string errorMessage)
	{
		javaHome = null;
		errorMessage = null;

		// An explicit BEAM_JAVA_HOME is trusted as-is (the user chose it for this purpose, any working java).
		if (!string.IsNullOrEmpty(ConfigService.CustomJavaHome)
		    && TryValidateJavaHome(ConfigService.CustomJavaHome, requireJava8: false, out _))
		{
			javaHome = ConfigService.CustomJavaHome;
			return true;
		}

		// A general JAVA_HOME is only used if it actually points at a Java 8 JDK (the Scala backend needs 8).
		var envJavaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
		if (!string.IsNullOrEmpty(envJavaHome) && TryValidateJavaHome(envJavaHome, requireJava8: true, out _))
		{
			javaHome = envJavaHome;
			return true;
		}

		// macOS: ask the system for a Java 8 home directly.
		if (OperatingSystem.IsMacOS() && TryMacJavaHome(out var macHome) && TryValidateJavaHome(macHome, requireJava8: true, out _))
		{
			javaHome = macHome;
			return true;
		}

		// Common install directories, newest-looking first.
		foreach (var candidate in EnumerateCandidateHomes())
		{
			if (TryValidateJavaHome(candidate, requireJava8: true, out _))
			{
				javaHome = candidate;
				return true;
			}
		}

		errorMessage =
			$"Java 8 home not found using common paths. Please install a JDK 8 or specify one with the {ConfigService.ENV_VAR_JAVA_HOME} env var / --java-path option.";
		return false;
	}

	private static IEnumerable<string> EnumerateCandidateHomes()
	{
		var globs = new (string dir, string pattern)[]
		{
			("/usr/lib/jvm", "*8*"),                                        // common Linux
			("/Library/Java/JavaVirtualMachines", "*8*"),                   // macOS JDK bundles (…/Contents/Home)
			("C:\\Program Files\\Eclipse Adoptium", "jdk-8*"),              // Windows Temurin
			("C:\\Program Files\\Java", "jdk1.8*"),                          // Windows Oracle
		};

		foreach (var (dir, pattern) in globs)
		{
			IEnumerable<string> matches;
			try
			{
				matches = Directory.Exists(dir)
					? Directory.EnumerateDirectories(dir, pattern).OrderByDescending(p => p)
					: Array.Empty<string>();
			}
			catch
			{
				matches = Array.Empty<string>();
			}

			foreach (var match in matches)
			{
				// macOS JDK bundles nest the home under Contents/Home.
				var contentsHome = Path.Combine(match, "Contents", "Home");
				yield return Directory.Exists(contentsHome) ? contentsHome : match;
			}
		}
	}

	private static bool TryMacJavaHome(out string home)
	{
		home = null;
		try
		{
			var output = new StringBuilder();
			var res = CliWrap.Cli.Wrap("/usr/libexec/java_home")
				.WithArguments(new[] { "-v", "1.8" })
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync();
			res.Task.Wait();
			if (res.Task.Result.ExitCode != 0) return false;
			home = output.ToString().Trim();
			return !string.IsNullOrEmpty(home);
		}
		catch (Exception ex)
		{
			Log.Verbose($"java_home lookup failed: {ex.Message}");
			return false;
		}
	}

	/// <summary>Validates a <c>JAVA_HOME</c> by running <c>&lt;home&gt;/bin/java -version</c>.</summary>
	public static bool TryValidateJavaHome(string home, bool requireJava8, out string message)
	{
		message = null;
		if (string.IsNullOrWhiteSpace(home))
		{
			message = "no java home given";
			return false;
		}

		var exe = Path.Combine(home, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
		if (!File.Exists(exe))
		{
			message = $"no java executable at {exe}";
			return false;
		}

		Log.Verbose($"testing java candidate=[{exe}]");
		var output = new StringBuilder();
		var command = CliWrap.Cli.Wrap(exe)
			.WithArguments("-version")
			// `java -version` prints to stderr; capture both streams.
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
			.WithValidation(CommandResultValidation.None);

		try
		{
			var res = command.ExecuteAsync();
			res.Task.Wait();
			if (res.Task.Result.ExitCode != 0)
			{
				message = $"path=[{exe}] is not a valid java executable";
				return false;
			}

			var text = output.ToString();
			if (requireJava8 && !(text.Contains("\"1.8") || text.Contains("version \"8")))
			{
				message = $"path=[{exe}] is not Java 8 (got: {text.Split('\n').FirstOrDefault()?.Trim()})";
				return false;
			}

			Log.Verbose($"found java home=[{home}]");
			return true;
		}
		catch (Exception ex)
		{
			message = $"path=[{exe}] could not invoke java: {ex.Message}";
			Log.Verbose(message);
			return false;
		}
	}
}
