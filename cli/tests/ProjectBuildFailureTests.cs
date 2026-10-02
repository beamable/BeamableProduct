using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using cli;
using cli.Services;
using CliWrap;
using NUnit.Framework;

namespace tests;

public class ProjectBuildFailureTests
{
	private string _directory = null!;
	private string ErrorPath => Path.Combine(_directory, "compiler.sarif");

	[SetUp]
	public void SetUp()
	{
		_directory = Path.Combine(Path.GetTempPath(), "beam-build-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_directory);
	}

	[TearDown]
	public void TearDown() => Directory.Delete(_directory, true);

	private Command BuildCommand() => CliWrap.Cli.Wrap("dotnet").WithWorkingDirectory(_directory)
		.WithArguments(new[] { "build", "Probe.csproj", "--nologo", "-v:minimal", $"-p:ErrorLog={ErrorPath}%2Cversion=2" });

	private void WriteProject(string target = "", string source = "public class Valid { }")
	{
		File.WriteAllText(Path.Combine(_directory, "Probe.csproj"), $"""
			<Project Sdk="Microsoft.NET.Sdk">
			<PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableNETAnalyzers>false</EnableNETAnalyzers></PropertyGroup>
			{target}
			</Project>
			""");
		File.WriteAllText(Path.Combine(_directory, "Valid.cs"), source);
	}

	[TestCase("AfterTargets", "Build")]
	[TestCase("BeforeTargets", "CoreCompile")]
	public void MsbuildFailure_EmitsFailedReportBeforeThrowing(string position, string target)
	{
		WriteProject($"<Target Name=\"FailBuild\" {position}=\"{target}\"><Error Text=\"TUPLE_SCHEMA_FAILURE\" /></Target>");
		ProjectErrorReport? report = null;
		var exception = Assert.ThrowsAsync<CliException>(async () => await ProjectService.ExecuteBuild(BuildCommand(), ErrorPath, r => report = r));
		Assert.That(exception!.NonZeroOrOneExitCode, Is.EqualTo(2));
		Assert.That(report, Is.Not.Null, "Unity must receive a report before the command fails");
		Assert.That(report!.isSuccess, Is.False);
		Assert.That(string.Join("\n", report.errors.Select(e => e.formattedMessage)), Does.Contain("TUPLE_SCHEMA_FAILURE"));
		if (position == "AfterTargets")
			Assert.That(ProjectService.ReadErrorReport(ErrorPath).errors, Is.Empty);
		else
			Assert.That(File.Exists(ErrorPath), Is.False);
	}

	[Test]
	public void MalformedSarif_DoesNotHideProcessFailure()
	{
		File.WriteAllText(ErrorPath, "not valid JSON");
		var report = ProjectService.ReadBuildErrorReport(ErrorPath, 1, "ACTIONABLE_BUILD_FAILURE");
		Assert.That(report.isSuccess, Is.False);
		Assert.That(report.errors.Single().formattedMessage, Does.Contain("ACTIONABLE_BUILD_FAILURE"));
	}

	[Test]
	public async Task CompilerFailure_PreservesLocation_AndDoesNotLeakIntoNextBuild()
	{
		WriteProject(source: "public class Invalid { UnknownType value; }");
		ProjectErrorReport? report = null;
		Assert.ThrowsAsync<CliException>(async () => await ProjectService.ExecuteBuild(BuildCommand(), ErrorPath, r => report = r));
		Assert.That(report!.errors.Any(e => e.line == 1 && e.formattedMessage.Contains("CS0246")), Is.True);

		// The next failure happens before compilation; the old compiler report must not hide it.
		WriteProject("<Target Name=\"FailEarly\" BeforeTargets=\"CoreCompile\"><Error Text=\"PRE_COMPILE_FAILURE\" /></Target>");
		Assert.ThrowsAsync<CliException>(async () => await ProjectService.ExecuteBuild(BuildCommand(), ErrorPath, r => report = r));
		var message = string.Join("\n", report!.errors.Select(e => e.formattedMessage));
		Assert.That(message, Does.Contain("PRE_COMPILE_FAILURE").And.Not.Contain("CS0246"));

		WriteProject();
		await ProjectService.ExecuteBuild(BuildCommand(), ErrorPath, r => report = r);
		Assert.That(report!.isSuccess, Is.True);
		// An unchanged incremental build need not run the compiler or create another SARIF file.
		await ProjectService.ExecuteBuild(BuildCommand(), ErrorPath, r => report = r);
		Assert.That(report!.isSuccess, Is.True);
		Assert.That(report.errors, Is.Empty);
	}
}
