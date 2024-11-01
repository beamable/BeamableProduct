using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Api.Calendars;
using Beamable.Server.Api.Content;
using Beamable.Server.Content;
using microservice;
using microserviceTests.microservice.Util;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog.Events;
using UnityEngine;

namespace microserviceTests.microservice
{

	[Microservice(nameof(InitCompleteTestService), EnableEagerContentLoading = false)]
	public class InitCompleteTestService : Microservice
	{
		private static bool hasInitialized = false;
		[InitializeServices]
		public static async Task Initialize(IServiceInitializer initializer)
		{
			await Task.Delay(2000); // wait for 2 seconds
			hasInitialized = true;
		}

		[ClientCallable]
		public int Add(int a, int b)
		{
			if (!hasInitialized)
			{
				throw new Exception("The service has not initialized yet!");
			}
			return a + b;
		}
	}
	
	[Microservice(nameof(SingletonCache_Success), EnableEagerContentLoading = false)]
	public class SingletonCache_Success : Microservice
	{
		public static readonly ServiceCacheData Cache = new ServiceCacheData();

		public class ServiceCacheData
		{
			public List<int> Cache = new List<int>();

			public async Promise<Unit> GetSomeContentAndCacheIt()
			{
				var initializationPromise = new Promise<Unit>();

				Cache.Add(1);
				initializationPromise.CompleteSuccess(new Unit());
				return await initializationPromise;
			}
		}

		[ConfigureServices]
		public static void ConfigureCacheService(IServiceBuilder serviceBuilder)
		{
			serviceBuilder.AddSingleton(_ => Cache);
		}

		[InitializeServices]
		public static async Promise<Unit> InitializeServices(IServiceInitializer initializer)
		{
			var initializationPromise = new Promise<Unit>();
			var asd = initializer.GetServiceAsCache<ServiceCacheData>();

			asd.Cache.Add(1);
			initializationPromise.CompleteSuccess(new Unit());
			return await initializationPromise;
		}
	}

	[Microservice(nameof(SingletonCache_CacheGuard), EnableEagerContentLoading = false)]
	public class SingletonCache_CacheGuard : Microservice
	{
		public static readonly ServiceCacheData Cache = new ServiceCacheData();

		public class ServiceCacheData
		{
			public List<int> Cache = new List<int>();

			public async Promise<Unit> GetSomeContentAndCacheIt()
			{
				var initializationPromise = new Promise<Unit>();

				Cache.Add(1);
				initializationPromise.CompleteSuccess(new Unit());
				return await initializationPromise;
			}
		}

		[ConfigureServices]
		public static void ConfigureCacheService(IServiceBuilder serviceBuilder)
		{
			serviceBuilder.AddScoped(_ => new ServiceCacheData());
		}

		[InitializeServices]
		public static async Promise<Unit> InitializeServices(IServiceInitializer initializer)
		{
			var initializationPromise = new Promise<Unit>();

			// Since we shutdown the application if any exceptions are thrown during this promise chain, we need to try-catch this to validate that
			// we are indeed throwing a InaccessibleServiceException if the requested service wasn't registered as a singleton service.
			// If we don't do this, the test fails due to we shutting down the running application --- don't do this in production code unless you know what you are
			// doing for your specific use-case.
			try
			{
				_ = initializer.GetServiceAsCache<ServiceCacheData>();
			}
			catch (Exception exception)
			{
				Assert.IsTrue(exception.GetType() == typeof(InaccessibleServiceException));
			}

			initializationPromise.CompleteSuccess(new Unit());
			return await initializationPromise;
		}
	}


	[Microservice(nameof(ExecutionOrder), EnableEagerContentLoading = false)]
	public class ExecutionOrder : Microservice
	{
		public static readonly ExecutionCounter CachedCounter = new ExecutionCounter();

		public class ExecutionCounter
		{
			public static readonly List<int> ConfigureCounter = new List<int>();
			public readonly List<int> InitializeCounter = new List<int>();

			public async Task IncrementCounter(int i)
			{
				BeamableLogger.Log($"Called with {i}");
				await Task.Delay(10);
				BeamableLogger.Log($"Delayed with {i}");
				InitializeCounter.Add(i);
			}
		}

		[ConfigureServices(ExecutionOrder = 2)]
		public static void ConfigureSecond(IServiceBuilder serviceBuilder)
		{
			ExecutionCounter.ConfigureCounter.Add(1);
		}

		[ConfigureServices(ExecutionOrder = 1)]
		public static void ConfigureFirst(IServiceBuilder serviceBuilder)
		{
			ExecutionCounter.ConfigureCounter.Add(0);
			serviceBuilder.AddSingleton(_ => CachedCounter);
		}

		[InitializeServices(ExecutionOrder = 2)]
		public static Promise<Unit> InitializeSecond(IServiceInitializer initializer)
		{
			return initializer.GetServiceAsCache<ExecutionCounter>().IncrementCounter(1).ToPromise().ToUnit();
		}

		[InitializeServices(ExecutionOrder = 4)]
		public static async Promise<Unit> InitializeFourth(IServiceInitializer initializer)
		{
			var promise = new Promise<Unit>();
			await initializer.GetServiceAsCache<ExecutionCounter>().IncrementCounter(3);
			promise.CompleteSuccess(new Unit());
			return await promise;
		}

		[InitializeServices(ExecutionOrder = 3)]
		public static async Task InitializeThird(IServiceInitializer initializer)
		{
			await initializer.GetServiceAsCache<ExecutionCounter>().IncrementCounter(2);
		}

		[InitializeServices(ExecutionOrder = 1)]
		public static void InitializeFirst(IServiceInitializer initializer)
		{
			initializer.GetServiceAsCache<ExecutionCounter>().InitializeCounter.Add(0);
			//await initializer.GetServiceAsCache<ExecutionCounter>().IncrementCounter(0);  <<----- Causes non-deterministic behaviour as async void methods are not awaitable when called via reflection.
		}
	}


	public class MicroserviceInitializationTests : CommonTest
	{
		[Test]
		[NonParallelizable]
		public async Task Test_InitializationCompletesBeforeTrafficIsHandled()
		{
			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
	                
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(n => n == 3),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<InitCompleteTestService>(new TestArgs());

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_" + nameof(InitCompleteTestService), nameof(InitCompleteTestService.Add), 1, 1, 1, 2));
			Assert.IsTrue(ms.HasInitialized);

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
		
		[Test]
		[NonParallelizable]
		public async Task Test_SingletonCache_Success()
		{

			var contentResolver = new TestContentResolver(async uri => "{}");
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				socket.WithName("Test Socket");
				socket.AddStandardMessageHandlers();
			}), contentResolver);

			await ms.Start<SingletonCache_Success>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			var provider = ms.Service.Provider;
			var service =
				(SingletonCache_Success.ServiceCacheData)provider.GetService(
					typeof(SingletonCache_Success.ServiceCacheData));
			Assert.IsTrue(service.Cache.Count > 0);
			await ms.OnShutdown(this, null);
		}


		[Test]
		[NonParallelizable]
		public async Task Test_SingletonCache_CacheGuard()
		{
			var contentResolver = new TestContentResolver(async uri => { return "{}"; });
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				socket.WithName("Test Socket");
				socket.AddStandardMessageHandlers();
			}), contentResolver);

			try
			{
				await ms.Start<SingletonCache_CacheGuard>(new TestArgs());
			}
			catch (Exception e)
			{

			}


			// Assert.IsTrue(!ms.HasInitialized); => We don't test this to be true as the exception intentionally causes a process termination and therefore terminates the test ---
			// so we catch the exception and don't test for this. TODO: Find a better way to test this that doesn't involve a bunch of extra complexity to abstract the Environment.Exit call.

			await ms.OnShutdown(this, null);
		}

		[Test]
		[NonParallelizable]
		public async Task Test_SupportedSignaturesAndInitExecutionOrder()
		{

			var contentResolver = new TestContentResolver(async uri => { return "{}"; });
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				socket.WithName("Test Socket");
				socket.AddStandardMessageHandlers();
			}), contentResolver);

			await ms.Start<ExecutionOrder>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			var provider = ms.Service.Provider;
			var service = (ExecutionOrder.ExecutionCounter)provider.GetService(typeof(ExecutionOrder.ExecutionCounter));


			for (int i = 0; i < 2; i++)
			{
				Assert.IsTrue(ExecutionOrder.ExecutionCounter.ConfigureCounter[i] == i);
			}

			for (int i = 0; i < 4; i++)
			{
				Assert.IsTrue(service.InitializeCounter[i] == i);
			}

			await ms.OnShutdown(this, null);
		}

	}
}
