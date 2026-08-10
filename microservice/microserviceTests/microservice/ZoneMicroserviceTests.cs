using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.Inventory;
using NUnit.Framework;

namespace microserviceTests.microservice
{
	/// <summary>
	/// A minimal zone-scoped service. It inherits <see cref="ZoneMicroservice"/> (not
	/// <see cref="Microservice"/>), so it has no realm SDK surface and is implicitly <c>[ZoneScoped]</c>.
	/// </summary>
	[Microservice("zone-test-service", EnableEagerContentLoading = false)]
	public class ZoneTestService : ZoneMicroservice
	{
		// NOTE(janky): client-callable is philosophically wrong for a zone service (no player/realm auth
		// context); the harness rounds-trips ClientCallable today. ServerCallable-only enforcement is future
		// work (analyzer / method scanner).
		[ClientCallable]
		public int Echo(int value) => value;
	}

	public class ZoneMicroserviceTests : CommonTest
	{
		[Test]
		public void ZoneContainer_RejectsRealmScopedService()
		{
			var builder = new ScopedDependencyBuilder(BeamServiceScope.Zone);
			// IMicroserviceInventoryApi is [RealmScoped]; registering it in a zone container must fail on Build.
			builder.AddScoped<IMicroserviceInventoryApi>(_ => null);

			var ex = Assert.Throws<ScopeValidationException>(() => builder.Build());
			StringAssert.Contains(nameof(IMicroserviceInventoryApi), ex.Message);
			StringAssert.Contains("Zone", ex.Message);
		}

		[Test]
		public void ZoneContainer_AcceptsZoneScopedService()
		{
			var builder = new ScopedDependencyBuilder(BeamServiceScope.Zone);
			// ZoneTestService inherits ZoneMicroservice's [ZoneScoped]; valid in a zone container.
			builder.AddScoped(typeof(ZoneTestService));

			Assert.DoesNotThrow(() => builder.Build());
		}

		[Test]
		public void RealmContainer_RejectsZoneScopedService()
		{
			var builder = new ScopedDependencyBuilder(BeamServiceScope.Realm);
			builder.AddScoped(typeof(ZoneTestService));

			var ex = Assert.Throws<ScopeValidationException>(() => builder.Build());
			StringAssert.Contains(nameof(ZoneTestService), ex.Message);
		}

		/// <summary>
		/// Full websocket round-trip of a booted zone service. Marked Explicit: the DI/scope path is proven
		/// by the tests above, but the socket connect/handshake path is not yet zone-hardened (it still makes
		/// realm assumptions once the connection is established), so this crashes the host today. Kept as
		/// scaffolding for when the zone connect path is finished. Run with `--filter` explicitly.
		/// </summary>
		[Test]
		[Explicit("Zone socket connect/handshake path is not yet zone-hardened; groundwork only.")]
		[NonParallelizable]
		public async Task ZoneService_BootsInZoneScope_AndHandlesTraffic()
		{
			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<int>(n => n == 42),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.StartZone<ZoneTestService>();

			testSocket.SendToClient(ClientRequest.ClientCallable(
				"micro_zone-test-service", nameof(ZoneTestService.Echo), 1, 1, 42));

			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
	}
}
