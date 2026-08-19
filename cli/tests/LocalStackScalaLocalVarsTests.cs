using System.Collections.Generic;
using System.IO;
using System.Linq;
using cli.Commands.LocalStack;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the native replacement for BeamableBackend's <c>bin/set-local-vars</c> — the renderer that produces the
/// three gitignored config files a fresh clone lacks — plus the small HOCON reader the AWS preflight uses to
/// recover role ARNs and bucket names from the rendered output.
///
/// The templates it replaces are pure <c>{{ VAR }}</c> substitution (no Liquid tags, filters or control flow),
/// which is what makes a regex renderer a faithful port rather than an approximation.
/// </summary>
public class LocalStackScalaLocalVarsTests
{
	private static readonly Dictionary<string, string> Vars = new Dictionary<string, string>
	{
		["BEAMABLE_AWS_BUCKET_TRIALS"] = "beamable-cloud-local",
		["BEAMABLE_AWS_BUCKET_COMET"] = "beamable-comet-local",
		["BEAMABLE_DEFAULT_REGION"] = "us-west-2",
	};

	[Test]
	public void SubstitutesPlaceholders()
	{
		var rendered = ScalaLocalVarsService.Render(
			"aws {\n  buckets {\n    trials = \"{{ BEAMABLE_AWS_BUCKET_TRIALS }}\"\n  }\n}", Vars);

		Assert.That(rendered, Does.Contain("trials = \"beamable-cloud-local\""));
		Assert.That(rendered, Does.Not.Contain("{{"));
	}

	[Test]
	public void ToleratesAnyInnerWhitespace()
	{
		var rendered = ScalaLocalVarsService.Render(
			"a={{BEAMABLE_DEFAULT_REGION}} b={{  BEAMABLE_DEFAULT_REGION  }}", Vars);

		Assert.That(rendered, Is.EqualTo("a=us-west-2 b=us-west-2"));
	}

	[Test]
	public void UnknownVariablesRenderEmptyAndAreReported()
	{
		// Matching the Python/Liquid original, an unknown name renders empty rather than failing. But an empty
		// bucket name or role ARN fails much later at runtime in a way that looks nothing like a config problem,
		// so the names have to be surfaced.
		var missing = new HashSet<string>();
		var rendered = ScalaLocalVarsService.Render("x = \"{{ NOT_PROVIDED }}\"", Vars, missing);

		Assert.That(rendered, Is.EqualTo("x = \"\""));
		Assert.That(missing, Is.EquivalentTo(new[] { "NOT_PROVIDED" }));
	}

	[Test]
	public void KnownVariablesAreNotReportedAsMissing()
	{
		var missing = new HashSet<string>();
		ScalaLocalVarsService.Render("x = \"{{ BEAMABLE_DEFAULT_REGION }}\"", Vars, missing);

		Assert.That(missing, Is.Empty);
	}

	[Test]
	public void AllThreeGeneratedConfigFilesAreTracked()
	{
		// The repo README documents only awsglobal.conf, but beamo will not start without its two server.conf
		// files — so all three have to be generated, not just the documented one.
		Assert.That(ScalaLocalVarsService.RelativeConfPaths, Has.Length.EqualTo(3));
		Assert.That(ScalaLocalVarsService.RelativeConfPaths.Any(p => p.EndsWith("awsglobal.conf")), Is.True);
		Assert.That(ScalaLocalVarsService.RelativeConfPaths.Count(p => p.EndsWith("server.conf")), Is.EqualTo(2));
	}

	[Test]
	public void MissingConfigFilesListsWhatIsAbsent()
	{
		var dir = Directory.CreateTempSubdirectory("beam-scala-config-test");
		try
		{
			Assert.That(ScalaLocalVarsService.MissingConfigFiles(dir.FullName),
				Has.Count.EqualTo(ScalaLocalVarsService.RelativeConfPaths.Length));

			// Create one of them; it should drop out of the missing list.
			var first = Path.Combine(dir.FullName, ScalaLocalVarsService.RelativeConfPaths[0]);
			Directory.CreateDirectory(Path.GetDirectoryName(first)!);
			File.WriteAllText(first, "aws {}");

			Assert.That(ScalaLocalVarsService.MissingConfigFiles(dir.FullName),
				Has.Count.EqualTo(ScalaLocalVarsService.RelativeConfPaths.Length - 1));
		}
		finally
		{
			dir.Delete(recursive: true);
		}
	}

	[Test]
	public void MissingConfigFilesIsEmptyForAnUnknownDirectory()
	{
		// No checkout to inspect is "nothing to report", not "everything is missing" — validate must not claim a
		// gap it cannot see.
		Assert.That(ScalaLocalVarsService.MissingConfigFiles(null), Is.Empty);
		Assert.That(ScalaLocalVarsService.MissingConfigFiles(Path.Combine(Path.GetTempPath(), "no-such-repo")), Is.Empty);
	}

	// ----------------------------------------------------------------------------------
	// The HOCON reader used by the AWS preflight
	// ----------------------------------------------------------------------------------

	[Test]
	public void ReadsANestedConfValueByDottedPath()
	{
		var file = WriteConf(@"
aws {
  buckets {
    trials = ""beamable-cloud-local""
  }

  credentials {
    s3 {
      services {
        role {
          arn = ""arn:aws:iam::677138418699:role/beamable-service-role""
        }
      }
      storage {
        role {
          arn = ""arn:aws:iam::386048776778:role/platform-container-service-assumed""
        }
      }
    }
  }
}

jwtToken {
  secret {
    reference = ""beamable.jwt.signingKey.local""
  }
}
");
		try
		{
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "aws.buckets.trials"),
				Is.EqualTo("beamable-cloud-local"));
			// The two role ARNs are at identical depths under sibling blocks; the reader must not confuse them.
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "aws.credentials.s3.services.role.arn"),
				Is.EqualTo("arn:aws:iam::677138418699:role/beamable-service-role"));
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "aws.credentials.s3.storage.role.arn"),
				Is.EqualTo("arn:aws:iam::386048776778:role/platform-container-service-assumed"));
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "jwtToken.secret.reference"),
				Is.EqualTo("beamable.jwt.signingKey.local"));
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Test]
	public void ReturnsNullForAnAbsentKey()
	{
		var file = WriteConf("aws {\n  buckets {\n    trials = \"x\"\n  }\n}");
		try
		{
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "aws.buckets.nope"), Is.Null);
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "totally.absent"), Is.Null);
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Test]
	public void IgnoresComments()
	{
		var file = WriteConf("aws {\n  # buckets { trials = \"commented\" }\n  buckets {\n    trials = \"real\" // trailing\n  }\n}");
		try
		{
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "aws.buckets.trials"), Is.EqualTo("real"));
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Test]
	public void KeepsDoubleSlashesInsideQuotedValues()
	{
		// A '//' comment strip that ignores quoting would truncate every URL in the file to "https:".
		var file = WriteConf("beamo {\n  wshost = \"wss://localhost:5060/socket\"\n}");
		try
		{
			Assert.That(ScalaLocalVarsService.ReadConfValue(file, "beamo.wshost"),
				Is.EqualTo("wss://localhost:5060/socket"));
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Test]
	public void ReadConfValueIsSafeForAMissingFile()
	{
		Assert.That(ScalaLocalVarsService.ReadConfValue(Path.Combine(Path.GetTempPath(), "nope.conf"), "a.b"), Is.Null);
	}

	// ----------------------------------------------------------------------------------
	// Maven detection, which the toolchain tokens changed
	// ----------------------------------------------------------------------------------

	[Test]
	public void MavenIsRecognisedByFileNameNotWholeString()
	{
		// `${maven}` substitutes to an absolute path, so a whole-string comparison would stop recognising the
		// Scala build step and silently drop the injected `clean` — the thing that keeps cross-module classes
		// from skewing.
		Assert.That(LocalStackUpCommand.IsMvn("mvn"), Is.True);
		Assert.That(LocalStackUpCommand.IsMvn("mvn.cmd"), Is.True);
		Assert.That(LocalStackUpCommand.IsMvn(Path.Combine("/tc", "maven", "3.9.9", "bin", "mvn")), Is.True);
		Assert.That(LocalStackUpCommand.IsMvn(LocalStackTemplate.MavenToken), Is.True);

		Assert.That(LocalStackUpCommand.IsMvn("npm"), Is.False);
		Assert.That(LocalStackUpCommand.IsMvn("docker"), Is.False);
		Assert.That(LocalStackUpCommand.IsMvn(null), Is.False);
		Assert.That(LocalStackUpCommand.IsMvn(""), Is.False);
	}

	private static string WriteConf(string body)
	{
		var file = Path.GetTempFileName();
		File.WriteAllText(file, body);
		return file;
	}
}
