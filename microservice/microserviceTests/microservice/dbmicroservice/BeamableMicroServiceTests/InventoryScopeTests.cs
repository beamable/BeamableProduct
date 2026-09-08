using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common.Api.Inventory;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	/// <summary>
	/// Regression tests for https://github.com/beamable/BeamableProduct/issues/4833.
	///
	/// <see cref="Beamable.Server.Api.Inventory.MicroserviceInventoryApi.GetCurrent"/> must omit the scope query
	/// parameter when the scope is empty. Sending it as an empty value (`?scope=`) produces a request the gateway
	/// never answers, and the promise hangs forever.
	/// </summary>
	[TestFixture]
	public class InventoryScopeTests : CommonTest
	{
		private const string ServiceName = "inventoryscopeservice";
		private const string ServiceRoute = "micro_" + ServiceName;
		private const int PlayerId = 1;

		// The autogen InventoryView the platform would return. Only currencies are needed to prove the round trip.
		private const string InventoryJson =
			"{\"currencies\":[{\"id\":\"currency.gems\",\"amount\":5}],\"items\":[],\"scope\":\"currency\"}";

		private const string ExpectedClientPayload = "currency.gems=5";

		[Microservice(ServiceName, EnableEagerContentLoading = false)]
		public class InventoryScopeMicroservice : Microservice
		{
			[ClientCallable]
			public async Task<string> GetDefaultScope()
			{
				var view = await Services.Inventory.GetCurrent();
				return Describe(view);
			}

			[ClientCallable]
			public async Task<string> GetEmptyScope()
			{
				var view = await Services.Inventory.GetCurrent("");
				return Describe(view);
			}

			[ClientCallable]
			public async Task<string> GetNullScope()
			{
				var view = await Services.Inventory.GetCurrent(null);
				return Describe(view);
			}

			[ClientCallable]
			public async Task<string> GetScoped(string scope)
			{
				var view = await Services.Inventory.GetCurrent(scope);
				return Describe(view);
			}

			private static string Describe(InventoryView view)
			{
				return string.Join(",", view.currencies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
			}
		}

		/// <summary>
		/// Builds a service whose socket answers exactly one inventory GET that satisfies <paramref name="routeMatcher"/>,
		/// and expects exactly one 200 reply to the client. Every inventory route the service sends is recorded in the
		/// returned list so a failing test can show what actually went over the wire.
		/// </summary>
		private static (TestSetup setup, Func<TestSocket> socket, List<string> routes) Build(TestSocketMessageMatcher routeMatcher)
		{
			TestSocket testSocket = null;
			var routes = new List<string>();
			var setup = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithGet()
							.WithRouteContains("object/inventory/")
							.And(req =>
							{
								lock (routes)
								{
									if (!routes.Contains(req.path)) routes.Add(req.path);
								}

								return true;
							})
							.And(routeMatcher),
						MessageResponder.Success(InventoryJson),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload(ExpectedClientPayload),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce());
			}));

			return (setup, () => testSocket, routes);
		}

		[Test]
		[NonParallelizable]
		[TestCase(nameof(InventoryScopeMicroservice.GetDefaultScope))]
		[TestCase(nameof(InventoryScopeMicroservice.GetEmptyScope))]
		[TestCase(nameof(InventoryScopeMicroservice.GetNullScope))]
		public async Task GetCurrent_WithoutScope_OmitsScopeQueryParameter(string callableName)
		{
			var (ms, socket, routes) = Build(req => req.path.EndsWith($"object/inventory/{PlayerId}/"));

			await ms.Start<InventoryScopeMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			socket().SendToClient(ClientRequest.ClientCallable(ServiceRoute, callableName, 1, PlayerId));

			await Task.Delay(50);
			await ms.OnShutdown(this, null);

			Assert.That(routes, Is.Not.Empty, "the service never called the inventory endpoint");
			Assert.That(routes, Has.All.Not.Contains("scope"),
				"an empty scope must be omitted, never sent as `?scope=`. Routes seen: " + string.Join(" | ", routes));
			Assert.IsTrue(socket().AllMocksCalled(), "the inventory reply did not make it back to the client");
		}

		[Test]
		[NonParallelizable]
		public async Task GetCurrent_WithScope_SendsScopeQueryParameter()
		{
			const string scope = "currency";
			var (ms, socket, routes) = Build(req => req.path.EndsWith($"object/inventory/{PlayerId}/?scope={scope}"));

			await ms.Start<InventoryScopeMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			socket().SendToClient(ClientRequest.ClientCallable(ServiceRoute, nameof(InventoryScopeMicroservice.GetScoped), 1, PlayerId, scope));

			await Task.Delay(50);
			await ms.OnShutdown(this, null);

			Assert.That(routes, Has.Some.Contains($"?scope={scope}"),
				"a non-empty scope must still be sent. Routes seen: " + string.Join(" | ", routes));
			Assert.IsTrue(socket().AllMocksCalled(), "the inventory reply did not make it back to the client");
		}
	}
}
