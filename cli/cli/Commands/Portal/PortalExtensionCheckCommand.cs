using Beamable.Common;
using Beamable.Server;
using cli.Utils;

namespace cli.Portal;

public class PortalExtensionCheckCommandArgs : CommandArgs
{

}

public class PortalExtensionCheckCommand : AppCommand<PortalExtensionCheckCommandArgs>, ISkipManifest
{
	public PortalExtensionCheckCommand() : base("check", "Verifies that all dependencies required for a Portal Extension app exist")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PortalExtensionCheckCommandArgs args)
	{

		Log.Information("Checking if all dependencies for a Portal Extension App exist");
		if (CheckPortalExtensionsDependencies())
		{
			Log.Information("All dependencies for running a Portal Extension were found");
		}
		else
		{
			Log.Information("There are missing dependencies, please check the errors to know how to proceed");
		}

		return Task.CompletedTask;
	}

	public static bool CheckPortalExtensionsDependencies()
	{
		var nodeCheck = CheckDependency("node", "-v", minVersion: new PackageVersion(22, 0, 0),
			hint: "Install Node.js 22+ (includes npm & npx): https://nodejs.org/");

		var viteCheck = CheckDependency("vite", "--version",
			hint: "Install locally: npm i -D vite (recommended) or globally: npm i -g vite");

		if (!viteCheck)
		{
			viteCheck = CheckDependency("npx", "--yes vite --version",
				hint: "Running via npx requires internet the first time; consider adding vite to devDependencies.");
			if (viteCheck)
			{
				Log.Information("Found Vite through npx installation");
			}
		}

		return nodeCheck && viteCheck;
	}


	private static bool CheckDependency(string fileName, string args, PackageVersion? minVersion = null, string? hint = null)
	{
		try
		{
			var result = StartProcessUtil.Run(fileName, args);
			if (result.exit != 0)
			{
				Log.Error($"{fileName} not found or returned non-zero exit code.\n{result.stderr}".Trim(), hint);
				return false;
			}

			string text = (result.stdout.Length > 0 ? result.stdout : result.stderr).Trim();
			string verText = text.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? text.Substring(1) : text; //Node puts a "v" in front of the version

			if (minVersion != null)
			{
				if (!TryParseVersion(verText, out var version))
				{
					Log.Error($"{fileName}: Could not parse version from '{verText}'.");
					return true;
				}

				if (version < minVersion)
				{
					Log.Error($"{fileName} version {version} is less than required {minVersion}.", hint);
					return false;
				}
			}

			return true;
		}
		catch (Exception ex)
		{
			Log.Error($"{fileName} check failed: {ex.Message}\n {hint}");
			return false;
		}
	}

	private static bool TryParseVersion(string version, out PackageVersion v)
	{
		if (PackageVersion.TryFromSemanticVersionString(version, out v))
		{
			return true;
		}

		v = new PackageVersion(0, 0, 0);
		return false;
	}
}
