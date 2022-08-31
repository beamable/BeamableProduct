using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class StorageObjectTests : CommonTest

	{
		[SetUp]
		[TearDown]
		public void ResetContentInstance()
		{
			ContentApi.Instance = new Promise<IContentApi>();
		}



		[Test]
		[NonParallelizable]
		public async Task Call_StorageObject_GetDatabase()
		{
			TestSocket testSocket = null;

			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithGet()
							.WithRouteContains("beamo/storage/connection"),
						MessageResponder.Success("{\"connectionString\": \"mongodb://mongodb0.example.com:27017\"}"),
						MessageFrequency.Exactly(1));
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);
			var provider = ms.ServiceCollection.BuildServiceProvider();
			var storageObjectConnectionProvider =
				IServiceProviderExtensions.GetService<IStorageObjectConnectionProvider>(provider);
			var simpleStorage = await storageObjectConnectionProvider.GetDatabase<SimpleStorageObject>();
			var simpleStorage2 = await storageObjectConnectionProvider.GetDatabase<SimpleStorageObject>();

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
			Assert.IsTrue(simpleStorage.GetHashCode() == simpleStorage2.GetHashCode());
		}
	}
}
