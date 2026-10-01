using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using cli;
using cli.Commands.Project;
using cli.Utils;
using NUnit.Framework;

namespace tests;

/// <summary>
/// `beam project run` decides the scope and secret of every service it starts. A realm secret only authenticates
/// its realm and a zone secret only its zone (the gateway picks the secret by the pid's scope), so a SECRET override
/// is valid for one scope only, and must be rejected up front rather than leaving services looping on a 403.
/// </summary>
[TestFixture]
public class RunScopeIdentityTests
{
	const string Cid = "123";
	const string Pid = "DE_456";
	const string Zid = "ZONE_789";
	const string Host = "wss://example.test/socket";
	const string RealmSecret = "realm-secret";
	const string ZoneSecret = "zone-secret";

	static Dictionary<string, ServiceScopeIdentity> Build(string secretOverride, string realmSecret, string zoneSecret,
		params (string, bool)[] services) =>
		RunScopeIdentity.BuildIdentities(services, Cid, Pid, Zid, Host, secretOverride, realmSecret, zoneSecret);

	[Test]
	public void Realm_service_without_override_uses_realm_pid_and_no_secret_or_zid()
	{
		var identity = Build(null, null, null, ("Realm", false))["Realm"];

		Assert.That(identity.Pid, Is.EqualTo(Pid));
		Assert.That(identity.Zid, Is.Null);
		Assert.That(identity.Secret, Is.Null);
	}

	[Test]
	public void Zone_service_without_override_uses_zid_in_the_pid_slot_and_the_zone_secret()
	{
		var identity = Build(null, null, ZoneSecret, ("Zone", true))["Zone"];

		Assert.That(identity.Pid, Is.EqualTo(Zid));
		Assert.That(identity.Zid, Is.EqualTo(Zid));
		Assert.That(identity.Secret, Is.EqualTo(ZoneSecret));
	}

	[Test]
	public void Mixed_run_without_override_keeps_each_scope_separate()
	{
		var identities = Build(null, null, ZoneSecret, ("Realm", false), ("Zone", true));

		Assert.That(identities["Realm"].Pid, Is.EqualTo(Pid));
		Assert.That(identities["Realm"].Secret, Is.Null, "the zone secret must never reach a realm service");
		Assert.That(identities["Zone"].Secret, Is.EqualTo(ZoneSecret));
	}

	[Test]
	public void Realm_override_matching_the_realm_secret_is_used()
	{
		var identity = Build(RealmSecret, RealmSecret, null, ("Realm", false))["Realm"];

		Assert.That(identity.Secret, Is.EqualTo(RealmSecret));
	}

	[Test]
	public void Zone_override_matching_the_zone_secret_is_used()
	{
		var identity = Build(ZoneSecret, null, ZoneSecret, ("Zone", true))["Zone"];

		Assert.That(identity.Secret, Is.EqualTo(ZoneSecret));
	}

	[Test]
	public void Zone_secret_override_is_rejected_for_realm_services()
	{
		var ex = Assert.Throws<CliException>(() => Build(ZoneSecret, RealmSecret, null, ("Realm", false)));

		Assert.That(ex.Message, Does.Contain($"not valid for realm [{Pid}]"));
		Assert.That(ex.Message, Does.Contain("Realm"));
	}

	[Test]
	public void Realm_secret_override_is_rejected_for_zone_services()
	{
		var ex = Assert.Throws<CliException>(() => Build(RealmSecret, null, ZoneSecret, ("Zone", true)));

		Assert.That(ex.Message, Does.Contain($"not valid for zone [{Zid}]"));
	}

	[Test]
	public void Any_override_is_rejected_when_running_both_scopes()
	{
		var ex = Assert.Throws<CliException>(() =>
			Build(RealmSecret, RealmSecret, ZoneSecret, ("Realm", false), ("Zone", true)));

		Assert.That(ex.Message, Does.Contain("realm-scoped and zone-scoped services together"));
	}

	[Test]
	public void Process_environment_removes_zid_and_secret_when_not_set()
	{
		var env = Build(null, null, null, ("Realm", false))["Realm"].ToProcessEnvironment();

		Assert.That(env["PID"], Is.EqualTo(Pid));
		Assert.That(env["CID"], Is.EqualTo(Cid));
		Assert.That(env["HOST"], Is.EqualTo(Host));
		Assert.That(env.ContainsKey("ZID") && env["ZID"] == null, "ZID must be explicitly removed");
		Assert.That(env.ContainsKey("SECRET") && env["SECRET"] == null, "SECRET must be explicitly removed");
	}

	[Test]
	public void Null_environment_value_removes_an_inherited_variable_from_the_child()
	{
		const string name = "BEAM_TEST_INHERITED_VAR";
		Environment.SetEnvironmentVariable(name, "leaked");
		try
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var handle = StartProcessUtil.Run(isWindows ? "cmd.exe" : "sh", isWindows ? "/c set" : "-c env",
				environmentVariables: new Dictionary<string, string> { [name] = null, ["BEAM_TEST_SET_VAR"] = "kept" });
			var result = handle.WaitForResult();

			Assert.That(result.stdout, Does.Not.Contain(name));
			Assert.That(result.stdout, Does.Contain("BEAM_TEST_SET_VAR=kept"));
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}
}
