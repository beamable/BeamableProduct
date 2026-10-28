using System.IO;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers reading the portal's gitignored <c>.env.local</c>, which decides which backend a plain <c>npm run dev</c>
/// logs in against. Stacks don't use it: they give the portal its API host as a process variable.
///
/// The failure this guards is deliberately specific: with no <c>VITE_API_BASE</c>, the portal's <c>API_BASE</c>
/// falls back to <c>https://api.beamable.com</c>, so a portal served from localhost sends its login to
/// PRODUCTION. The local seed account does not exist there, so login fails while every local service is healthy —
/// a symptom that reads as a broken backend rather than missing config.
/// </summary>
public class LocalStackManagementEnvTests
{
	private const string Host = "http://localhost:8080";

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
	public void PortalConfigIsASelectableStep()
	{
		Assert.That(ToolchainPins.AllStepIds, Contains.Item(ToolchainPins.PortalConfig));
	}
}
