using System;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice
{
	[TestFixture]
	public class TestSetupShutdownTests
	{
		private sealed class PendingShutdownService : IBeamableService
		{
			public readonly TaskCompletionSource<bool> ShutdownCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
			public SocketRequesterContext SocketContext => null;
			public bool HasInitialized => true;
			public IDependencyProvider Provider => null;
			public Task OnShutdown(object sender, EventArgs args) => ShutdownCompleted.Task;
		}

		[Test]
		public async Task OnShutdown_WaitsForServiceShutdown()
		{
			var service = new PendingShutdownService();
			var setup = new TestSetup(null) { Service = service };
			var shutdown = setup.OnShutdown(this, EventArgs.Empty);
			try
			{
				Assert.That(shutdown.IsCompleted, Is.False, "The harness returned before the service finished shutting down.");
			}
			finally
			{
				service.ShutdownCompleted.TrySetResult(true);
			}

			await shutdown;
		}
	}
}
