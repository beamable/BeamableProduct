using Beamable.Common;
using Beamable.Server;
using cli.Utils;
using System.Text.RegularExpressions;

namespace cli.Portal;

public class PortalExtensionCheckCommandArgs : CommandArgs
{

}

public class PortalExtensionCheckCommand : AppCommand<PortalExtensionCheckCommandArgs>
{
	public PortalExtensionCheckCommand() : base("check", "Verifies that all dependencies required for a Portal Extension app exist")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PortalExtensionCheckCommandArgs args)
	{

		Check("node", "-v", min: new PackageVersion(22, 0, 0),
			hint: "Install Node.js 22+ (includes npm & npx): https://nodejs.org/");

		// Should we need to test for both cases?
		CheckEither(
			primary: () => Check("vite", "--version",
				hint: "Install locally: npm i -D vite (recommended) or globally: npm i -g vite"),
			fallback: () => Check("npx", "--yes vite --version",
				hint: "Running via npx requires internet the first time; consider adding vite to devDependencies.")
		);

		return Task.CompletedTask;
	}


	private bool Check(string fileName, string args, PackageVersion? min = null, string? hint = null)
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

			if (min != null)
			{
				if (!TryParseVersion(verText, out var ver))
				{
					Log.Warning($"{fileName}: couldn’t parse version from '{text}'.");
					return true;
				}
				if (ver < min)
				{
					Log.Error($"{fileName} version {ver} is less than required {min}.", hint);
					return false;
				}
			}

			return true;
		}
		catch (Exception ex)
		{
			Log.Error($"{fileName} check failed: {ex.Message}", hint);
			return false;
		}
	}

	private bool TryParseVersion(string version, out PackageVersion v)
	{
		if (PackageVersion.TryFromSemanticVersionString(version, out v))
		{
			return true;
		}

		v = new PackageVersion(0, 0, 0);
		return false;
	}

	private bool CheckEither(Func<bool> primary, Func<bool> fallback)
	{
		if (primary()) return true;
		Console.WriteLine("→ Trying fallback...");
		return fallback();
	}
}
