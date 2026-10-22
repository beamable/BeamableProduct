using Beamable.Server;
using cli.Services.Unity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.IO;

namespace tests.Unity;

public class FederationCommonCopyTests
{
	[TestCase("FederatedMessageRail.cs")]
	[TestCase("FederatedCampaignVirtualOffer.cs")]
	public void CommittedUnityContractMatchesCommonCopy(string fileName)
	{
		BeamableZLoggerProvider.SetLogger(NullLogger.Instance);
		var repository = FindRepository();
		var source = Path.Combine(repository, "cli", "beamable.common", "Runtime", "Federation");
		var unity = UnityProjectUtil.GetUnityInfo(Path.Combine(repository, "client"), "com.beamable");
		var committed = Path.Combine(unity.packageFolder, "Common", "Runtime", "Federation", fileName);
		var output = Path.Combine(Path.GetTempPath(), "beam-federation-copy-tests", Guid.NewGuid().ToString("N"));
		try
		{
			// Exercise the same producer as release-shared-code.
			UnityProjectUtil.CopyProject(source, output);
			Assert.That(File.Exists(committed), Is.True, $"Missing Unity copy: {fileName}");
			Assert.That(File.ReadAllText(committed).ReplaceLineEndings("\n"),
				Is.EqualTo(File.ReadAllText(Path.Combine(output, fileName)).ReplaceLineEndings("\n")),
				$"Refresh {fileName} with release-shared-code.");
		}
		finally
		{
			if (Directory.Exists(output))
			{
				foreach (var file in Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories))
					File.SetAttributes(file, FileAttributes.Normal);
				foreach (var directory in Directory.EnumerateDirectories(output, "*", SearchOption.AllDirectories))
					File.SetAttributes(directory, FileAttributes.Normal);
				File.SetAttributes(output, FileAttributes.Normal);
				Directory.Delete(output, true);
			}
		}
	}

	private static string FindRepository()
	{
		for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
		     directory != null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "cli", "beamable.common", "beamable.common.csproj")))
				return directory.FullName;
		}
		throw new DirectoryNotFoundException("The federation copy test requires the repository checkout.");
	}
}
