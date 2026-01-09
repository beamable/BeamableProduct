using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

public class AsyncVoidTests : CommonTest
{
	[Test]
	[NonParallelizable]
	public async Task ClientCallablesWithAsyncVoidShouldGoKerblam()
	{
		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers();
		}));
		allowErrorLogs = true;
		Assert.ThrowsAsync<BeamableMicroserviceException>(async () =>
		{
			await ms.Start<InvalidService_AsyncVoids>(new TestArgs());
		});
	}
	
	
	[Test]
	[NonParallelizable]
	public async Task InitializeAndShutdown()
	{

		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers();
		}));

		await ms.Start<ValidService_VariousSigs>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}



	[Microservice("invalid_asyncVoids", UseLegacySerialization = true, EnableEagerContentLoading = false)]
	public class InvalidService_AsyncVoids : Microservice
	{
		[ClientCallable]
		public async void IShouldNotExist()
		{
		   
		}
	}

	[Microservice("valid_asyncs", UseLegacySerialization = true, EnableEagerContentLoading = false)]
	public class ValidService_VariousSigs : Microservice
	{
		[ClientCallable]
		public void Void()
		{
		   
		}
		
		[ClientCallable]
		public Promise Promise()
		{
		   return Beamable.Common.Promise.Success;
		}
		
		[ClientCallable]
		public Promise<int> PromiseGeneric()
		{
			return Promise<int>.Successful(3);
		}
		
		[ClientCallable]
		public Task Task()
		{
			return System.Threading.Tasks.Task.CompletedTask;
		}
		
		
		[ClientCallable]
		public Task<int> TaskGeneric()
		{
			return System.Threading.Tasks.Task.FromResult(3);
		}
	}

}
