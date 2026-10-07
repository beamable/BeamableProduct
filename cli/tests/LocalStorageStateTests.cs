using System.Collections.Generic;
using cli.Services;
using Docker.DotNet.Models;
using NUnit.Framework;

namespace tests;

/// <summary>
/// `beam project run` decides, from a single Docker container listing, which storages are already running (and hands
/// their connection strings to the services) and which it has to start. These pin that matching.
/// </summary>
[TestFixture]
public class LocalStorageStateTests
{
	static ContainerListResponse Container(string storageId, string state, params (ushort privatePort, ushort publicPort)[] ports)
	{
		var portList = new List<Port>();
		foreach (var (privatePort, publicPort) in ports)
		{
			portList.Add(new Port { PrivatePort = privatePort, PublicPort = publicPort, Type = "tcp" });
		}

		return new ContainerListResponse
		{
			ID = storageId + "-id",
			Names = new List<string> { "/" + BeamoLocalSystem.GetBeamIdAsMongoContainer(storageId) },
			State = state,
			Ports = portList
		};
	}

	[Test]
	public void Running_container_is_running_with_its_mapped_host_port()
	{
		var states = BeamoLocalSystem.MatchStorageContainers(
			new[] { Container("Db", "running", (27017, 49153)) }, new[] { "Db" });

		Assert.That(states["Db"].Status, Is.EqualTo(LocalStorageStatus.Running));
		Assert.That(states["Db"].HostPort, Is.EqualTo("49153"));
		Assert.That(states["Db"].ContainerId, Is.EqualTo("Db-id"));
	}

	[Test]
	public void Missing_container_is_missing()
	{
		var states = BeamoLocalSystem.MatchStorageContainers(new ContainerListResponse[0], new[] { "Db" });

		Assert.That(states["Db"].Status, Is.EqualTo(LocalStorageStatus.Missing));
		Assert.That(states["Db"].HostPort, Is.Null);
	}

	[TestCase("exited")]
	[TestCase("created")]
	[TestCase("removing")]
	public void Non_running_container_is_stopped(string state)
	{
		var states = BeamoLocalSystem.MatchStorageContainers(new[] { Container("Db", state) }, new[] { "Db" });

		Assert.That(states["Db"].Status, Is.EqualTo(LocalStorageStatus.Stopped));
	}

	[Test]
	public void Name_match_is_exact_not_substring()
	{
		// Docker's name filter matches substrings, so the listing for "Db" can include "Db2_mongoDb".
		var states = BeamoLocalSystem.MatchStorageContainers(
			new[] { Container("Db2", "running", (27017, 49153)) }, new[] { "Db", "Db2" });

		Assert.That(states["Db"].Status, Is.EqualTo(LocalStorageStatus.Missing));
		Assert.That(states["Db2"].Status, Is.EqualTo(LocalStorageStatus.Running));
	}

	[Test]
	public void Unmapped_mongo_port_has_no_host_port()
	{
		var states = BeamoLocalSystem.MatchStorageContainers(
			new[] { Container("Db", "running", (27017, 0), (8081, 49160)) }, new[] { "Db" });

		Assert.That(states["Db"].Status, Is.EqualTo(LocalStorageStatus.Running));
		Assert.That(states["Db"].HostPort, Is.Null);
	}

	[Test]
	public void Duplicate_ids_resolve_once()
	{
		var states = BeamoLocalSystem.MatchStorageContainers(
			new[] { Container("Db", "running", (27017, 49153)) }, new[] { "Db", "Db" });

		Assert.That(states.Count, Is.EqualTo(1));
	}

	[Test]
	public void Connection_string_matches_generate_env_format()
	{
		var protocol = new EmbeddedMongoDbLocalProtocol { RootUsername = "beamable", RootPassword = "beamable" };

		Assert.That(BeamoLocalSystem.BuildLocalStorageConnectionString(protocol, "localhost", "49153"),
			Is.EqualTo("mongodb://beamable:beamable@localhost:49153"));
		Assert.That(BeamoLocalSystem.GetStorageConnectionStringVarName("Db"), Is.EqualTo("STORAGE_CONNSTR_Db"));
	}
}
