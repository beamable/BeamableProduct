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
	[TestCase("FederatedVirtualOffer.cs")]
	[TestCase("GlobalSuppressions.cs", "")]
	[TestCase("AssemblyDiscovery.cs", "ReflectionCache")]
	[TestCase("ReflectionCache.cs", "ReflectionCache")]
	public void CommittedUnityContractMatchesCommonCopy(string fileName, string sourceDirectory = "Federation")
	{
		BeamableZLoggerProvider.SetLogger(NullLogger.Instance);
		var repository = FindRepository();
		var source = Path.Combine(repository, "cli", "beamable.common", "Runtime", sourceDirectory);
		var unity = UnityProjectUtil.GetUnityInfo(Path.Combine(repository, "client"), "com.beamable");
		var committed = Path.Combine(unity.packageFolder, "Common", "Runtime", sourceDirectory, fileName);
		var output = Path.Combine(Path.GetTempPath(), "beam-federation-copy-tests", Guid.NewGuid().ToString("N"));
		try
		{
			var input = Path.Combine(output, "source");
			var generated = Path.Combine(output, "generated");
			Directory.CreateDirectory(input);
			Directory.CreateDirectory(generated);
			// Exercise the real producer on the canonical file under test. Copying all of
			// Runtime here also creates unrelated, read-only subdirectories on Linux.
			File.Copy(Path.Combine(source, fileName), Path.Combine(input, fileName));
			UnityProjectUtil.CopyProject(input, generated);
			Assert.That(File.Exists(committed), Is.True, $"Missing Unity copy: {fileName}");
			Assert.That(File.ReadAllText(committed).ReplaceLineEndings("\n"),
				Is.EqualTo(File.ReadAllText(Path.Combine(generated, fileName)).ReplaceLineEndings("\n")),
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
