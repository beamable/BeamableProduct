using System.Linq;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// The session name an assume-role probe sends.
///
/// This looks like trivia and is not. Some role trust policies condition on <c>sts:RoleSessionName</c>, so
/// probing them under any other name returns AccessDenied while the backend — which always assumes under
/// <c>platform-service-assume-role</c> (BeamableAPI's <c>AmazonAccountManager.SessionName</c>) — succeeds.
///
/// That produced a confidently wrong diagnosis: the preflight reported that this developer could not assume
/// the <c>beamable-local-analytics-*</c> roles and that an administrator had to grant access, while an
/// analytics loader was running on the same machine writing to exactly those buckets. Nothing about the
/// AccessDenied text distinguishes "no trust relationship" from "wrong session name", so the only defence is
/// to send what production sends before believing the failure.
///
/// The probe itself shells out to the AWS CLI, so what is pinned here is the part that decides the question
/// being asked: the candidate list and the argument string built from it.
/// </summary>
[TestFixture]
public class AwsPreflightSessionNameTests
{
	[Test]
	public void Probes_the_backend_session_name()
	{
		Assert.That(AwsPreflightService.SessionNameCandidates,
			Does.Contain(AwsPreflightService.PlatformServiceSessionName),
			"a role whose trust policy conditions on the session name is only reachable under this name");
	}

	[Test]
	public void Uses_the_exact_session_name_the_aws_policy_allows()
	{
		// Hard-coded rather than referenced: this string has to match an ALLOWED value in an AWS trust policy,
		// so a rename is a breaking infrastructure change and should fail here rather than at runtime, in an
		// error message that reads like a permissions problem.
		Assert.That(AwsPreflightService.PlatformServiceSessionName, Is.EqualTo("platform-service-assume-role"));
	}

	[Test]
	public void Tries_the_preflight_own_name_first()
	{
		// So CloudTrail attributes the probe to the preflight whenever that works, and the backend's shared
		// session name is only borrowed when a policy actually requires it.
		Assert.That(AwsPreflightService.SessionNameCandidates.First(),
			Is.Not.EqualTo(AwsPreflightService.PlatformServiceSessionName));
		Assert.That(AwsPreflightService.SessionNameCandidates, Is.Unique);
	}

	[Test]
	public void Sends_the_session_name_it_was_given()
	{
		// The defect was entirely in which name reached the CLI, so the argument string is worth pinning.
		var args = AwsPreflightService.AssumeRoleArgs(
			"arn:aws:iam::393371603939:role/beamable-local-analytics-writer-assume-role",
			AwsPreflightService.PlatformServiceSessionName);

		Assert.That(args, Does.Contain("--role-session-name platform-service-assume-role"));
		Assert.That(args, Does.Contain("--role-arn arn:aws:iam::393371603939:role/beamable-local-analytics-writer-assume-role"));
	}
}
