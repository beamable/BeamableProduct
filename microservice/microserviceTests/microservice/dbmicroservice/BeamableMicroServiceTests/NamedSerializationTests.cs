using System.Threading.Tasks;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class NamedSerializationTests : CommonTest
	{
		[Test]
		[NonParallelizable]
		public async Task Call_NamedInts_Success()
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

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(
				ClientRequest.ClientCallableNamed("micro_named", "Add", 1, 1, new { a = 1, b = 2 }));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_NamedInts_MissingParameter_Errors()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(400),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallableNamed("micro_named", "Add", 1, 1, new
			{
				a = 1, c = 2 // c does not exist
			}));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			allowErrorLogs = true;
			AssertMissingParameterError();
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_RenamedParameter()
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
							.WithPayload<bool>(x => x),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(
				ClientRequest.ClientCallableNamed("micro_named", "IsTrue", 1, 1, new { notX = true }));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}


		[Test]
		[NonParallelizable]
		public async Task Call_ArrayWithValues()
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
							.WithPayload<int>(x => x == 6),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallableNamed("micro_named", "Sum", 1, 1,
				new { arr = new int[] { 1, 2, 3 } }));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_ArrayWithNull()
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
							.WithPayload<int>(x => x == 0),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallableNamed("micro_named", "Sum", 1, 1, new IntArrayBody()));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		class IntArrayBody
		{
			public int[] arr;
		}

		[Test]
		[NonParallelizable]
		public async Task Call_ComplexDoodad()
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
							.WithPayload<int>(x => x == 10),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<NamedSerializationMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallableNamed("micro_named", "ComplexInput", 1, 1,
				new { xy = new { X = 1, Y = 2 }, doodad = new { X = 3, Foo = "abc", Recurse = new { X = 4 } } }));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
	}
}
