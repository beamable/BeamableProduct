using System.IO;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the portal's gitignored <c>.env.local</c>, which decides which backend the portal logs in against.
///
/// The failure this guards is deliberately specific: with no <c>VITE_API_BASE</c>, the portal's <c>API_BASE</c>
/// falls back to <c>https://api.beamable.com</c>, so a portal served from localhost sends its login to
/// PRODUCTION. The local seed account does not exist there, so login fails while every local service is healthy —
/// a symptom that reads as a broken backend rather than missing config.
/// </summary>
public class LocalStackPortalEnvTests
{
	private const string Host = "http://localhost:8080";

	[Test]
	public void CreatesTheFileWhenItIsMissing()
	{
		// The fresh-clone / copied-folder case: .env.local is gitignored so it is simply not there.
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			var result = new PortalEnvService().Ensure(dir.FullName, Host, force: false);

			Assert.That(result.ok, Is.True, result.error);
			Assert.That(result.action, Is.EqualTo("created"));
			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.EqualTo(Host));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void AppendsTheKeyWhenTheFileExistsWithoutIt()
	{
		// The nastiest variant: the file EXISTS (for some other override) but has no VITE_API_BASE, so an
		// existence check alone would report everything fine while the portal still talks to production.
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			var path = Path.Combine(dir.FullName, PortalEnvService.EnvFileName);
			File.WriteAllText(path, "# mine\nVITE_WINGMAN_URL=http://localhost:4960\n");

			var result = new PortalEnvService().Ensure(dir.FullName, Host, force: false);

			Assert.That(result.action, Is.EqualTo("added-key"));
			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.EqualTo(Host));
			// The developer's other overrides must survive.
			Assert.That(File.ReadAllText(path), Does.Contain("VITE_WINGMAN_URL=http://localhost:4960"));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void LeavesAnAlreadyCorrectValueAlone()
	{
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName),
				$"{PortalEnvService.ApiBaseKey}={Host}\n");

			var result = new PortalEnvService().Ensure(dir.FullName, Host, force: false);

			Assert.That(result.action, Is.EqualTo("kept"));
			Assert.That(result.apiBase, Is.EqualTo(Host));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void DoesNotRetargetADeliberateDifferentHostWithoutForce()
	{
		// Pointing the portal at dev/staging on purpose is legitimate. Setup reports it rather than silently
		// rewriting it out from under the developer.
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName),
				$"{PortalEnvService.ApiBaseKey}=https://dev.api.beamable.com\n");

			var kept = new PortalEnvService().Ensure(dir.FullName, Host, force: false);
			Assert.That(kept.action, Is.EqualTo("kept"));
			Assert.That(kept.apiBase, Is.EqualTo("https://dev.api.beamable.com"));

			var forced = new PortalEnvService().Ensure(dir.FullName, Host, force: true);
			Assert.That(forced.action, Is.EqualTo("rewritten"));
			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.EqualTo(Host));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void ReadApiBaseIgnoresCommentsAndBlankLines()
	{
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName),
				$"# {PortalEnvService.ApiBaseKey}=https://commented-out.example\n\n" +
				$"  {PortalEnvService.ApiBaseKey}=\"{Host}\"  \n");

			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.EqualTo(Host));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void MissingFileOrKeyBothMeanTheProductionFallback()
	{
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.Null, "no file");
			Assert.That(PortalEnvService.PointsAwayFromLocalBackend(dir.FullName, Host), Is.True);

			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName), "VITE_WINGMAN_URL=x\n");
			Assert.That(PortalEnvService.ReadApiBase(dir.FullName), Is.Null, "file without the key");
			Assert.That(PortalEnvService.PointsAwayFromLocalBackend(dir.FullName, Host), Is.True);

			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName),
				$"{PortalEnvService.ApiBaseKey}={Host}\n");
			Assert.That(PortalEnvService.PointsAwayFromLocalBackend(dir.FullName, Host), Is.False);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void TrailingSlashDoesNotCountAsADifferentHost()
	{
		var dir = Directory.CreateTempSubdirectory("beam-portal-env");
		try
		{
			File.WriteAllText(Path.Combine(dir.FullName, PortalEnvService.EnvFileName),
				$"{PortalEnvService.ApiBaseKey}={Host}/\n");

			Assert.That(PortalEnvService.PointsAwayFromLocalBackend(dir.FullName, Host), Is.False);
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void ReportsAnErrorForAMissingPortalDirectory()
	{
		var result = new PortalEnvService().Ensure(Path.Combine(Path.GetTempPath(), "no-such-portal"), Host, false);

		Assert.That(result.ok, Is.False);
		Assert.That(result.error, Does.Contain("Portal checkout not found"));
	}

	[Test]
	public void PortalConfigIsASelectableStep()
	{
		Assert.That(ToolchainPins.AllStepIds, Contains.Item(ToolchainPins.PortalConfig));
	}
}
