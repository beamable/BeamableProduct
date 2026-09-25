using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice
{
	[Microservice("ResilienceTestService", EnableEagerContentLoading = false)]
	public class ResilienceTestService : Microservice
	{
		public const string QualifiedName = "micro_ResilienceTestService";

		/// <summary>completes when <see cref="WaitForGate"/> has started executing</summary>
		public static TaskCompletionSource<bool> Entered = NewSource();

		/// <summary>released by the test to let <see cref="WaitForGate"/> finish</summary>
		public static TaskCompletionSource<bool> Gate = NewSource();

		public static void Reset()
		{
			Entered = NewSource();
			Gate = NewSource();
		}

		private static TaskCompletionSource<bool> NewSource() =>
			new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		[ClientCallable]
		public int Add(int a, int b)
		{
			return a + b;
		}

		[ClientCallable]
		public async Task<int> WaitForGate()
		{
			Entered.TrySetResult(true);
			await Gate.Task;
			return 1;
		}
	}

	/// <summary>
	/// Covers the overload and connection-loss behaviour of the runtime: a busy or draining instance must
	/// answer quickly so the gateway can route around it, a request the gateway has already given up on must
	/// not be worked on, and a lost connection must not leave handlers waiting forever.
	/// </summary>
	[TestFixture]
	public class ResilienceTests : CommonTest
	{
		private static async Task WaitFor(Task task, string what, int timeoutMs = 10_000)
		{
			var finished = await Task.WhenAny(task, Task.Delay(timeoutMs));
			Assert.AreSame(task, finished, $"timed out waiting for {what}");
			await task;
		}

		private static WebsocketRequest CallableWithDeadline(string methodName, int reqId, long deadlineEpochMs, params object[] args)
		{
			var req = ClientRequest.ClientCallable(ResilienceTestService.QualifiedName, methodName, reqId, 1, args);
			req.headers = new Dictionary<string, string>
			{
				[BeamableMicroService.GATEWAY_DEADLINE_HEADER] = deadlineEpochMs.ToString()
			};
			return req;
		}

		[Test]
		[NonParallelizable]
		public async Task DrainingInstance_Answers503_InsteadOfDroppingTheRequest()
		{
			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(503)
							.WithBody<WebsocketErrorResponse>(body => body.error == "draining" && body.status == 503),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce(),
						desc: "503 while draining"
					);
			}));

			await ms.Start<ResilienceTestService>(new TestArgs());
			var service = (BeamableMicroService)ms.Service;

			// this is the state the instance is in between DELETE gateway/provider and the socket closing.
			service.RefuseNewClientMessages = true;

			testSocket.SendToClient(ClientRequest.ClientCallable(ResilienceTestService.QualifiedName, nameof(ResilienceTestService.Add), 1, 1, 1, 2));

			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled(), "the draining instance did not answer with a 503");
		}

		[Test]
		[NonParallelizable]
		public async Task RequestPastItsGatewayDeadline_IsDroppedWithoutAResponse()
		{
			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				// no handler for request 1: if the service answered, the socket would record a NoHandlerException.
				socket.AddStandardMessageHandlers();
			}));

			await ms.Start<ResilienceTestService>(new TestArgs());

			var tenSecondsAgo = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 10_000;
			testSocket.SendToClient(CallableWithDeadline(nameof(ResilienceTestService.Add), 1, tenSecondsAgo, 1, 2));

			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled(), "a stale request produced a response");
			AssertBadLogCountContains("Dropping request", 1);
		}

		[Test]
		[NonParallelizable]
		public async Task RequestWithinItsGatewayDeadline_IsHandledNormally()
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
						MessageFrequency.OnlyOnce(),
						desc: "answer within deadline"
					);
			}));

			await ms.Start<ResilienceTestService>(new TestArgs());

			var inTenSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 10_000;
			testSocket.SendToClient(CallableWithDeadline(nameof(ResilienceTestService.Add), 1, inTenSeconds, 1, 2));

			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled(), "a request within its deadline was not answered");
			AssertBadLogCountContains("Dropping request", 0);
		}

		[Test]
		[NonParallelizable]
		public async Task RequestsBeyondTheConcurrencyLimit_AreShedWith503()
		{
			ResilienceTestService.Reset();
			var shedSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(n => n == 1),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce(),
						desc: "the request that held the only slot"
					)
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(2)
							.WithStatus(503)
							.WithBody<WebsocketErrorResponse>(body => body.error == "serviceBusy" && body.status == 503),
						MessageResponder.Custom(_ =>
						{
							shedSeen.TrySetResult(true);
							return null;
						}),
						MessageFrequency.OnlyOnce(),
						desc: "the request that was shed"
					);
			}));

			await ms.Start<ResilienceTestService>(new TestArgs { MaxConcurrentRequests = 1 });

			// occupy the only slot...
			testSocket.SendToClient(ClientRequest.ClientCallable(ResilienceTestService.QualifiedName, nameof(ResilienceTestService.WaitForGate), 1, 1));
			await WaitFor(ResilienceTestService.Entered.Task, "the first request to start");
			Assert.AreEqual(1, BeamableMicroService.InflightClientRequests);

			// ...so the next request is shed right away, instead of queueing behind it.
			testSocket.SendToClient(ClientRequest.ClientCallable(ResilienceTestService.QualifiedName, nameof(ResilienceTestService.Add), 2, 1, 1, 2));
			await WaitFor(shedSeen.Task, "the second request to be shed");
			Assert.AreEqual(1, BeamableMicroService.InflightClientRequests, "a shed request must not hold a slot");

			ResilienceTestService.Gate.TrySetResult(true);

			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
			Assert.AreEqual(0, BeamableMicroService.InflightClientRequests);
		}
	}

	[TestFixture]
	public class SocketRequesterContextTests : CommonTest
	{
		private static SocketRequesterContext CreateContext(int requestTimeoutSeconds)
		{
			var socket = new TestSocket();
			return new SocketRequesterContext(() => Promise<IConnection>.Successful(socket), requestTimeoutSeconds);
		}

		private static (Promise<object> promise, Task<Exception> failure) AddPendingRequest(SocketRequesterContext ctx, string path)
		{
			var promise = ctx.AddListener<object>(new WebsocketRequest { method = "get", path = path }, path, null, BeamActivity.Noop);
			var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
			// register the errback synchronously, so the failure can never be observed as "uncaught".
			promise.Error(ex => failure.TrySetResult(ex));
			return (promise, failure.Task);
		}

		private static async Task<Exception> WaitForFailure(Task<Exception> failure, int timeoutMs = 10_000)
		{
			var finished = await Task.WhenAny(failure, Task.Delay(timeoutMs));
			Assert.AreSame(failure, finished, "the pending request was never failed");
			return await failure;
		}

		[Test]
		public async Task FailAllPendingRequests_FailsEveryWaitingRequest()
		{
			var ctx = CreateContext(requestTimeoutSeconds: 0);
			var (_, first) = AddPendingRequest(ctx, "basic/first");
			var (_, second) = AddPendingRequest(ctx, "basic/second");
			Assert.AreEqual(2, ctx.PendingRequestCount);

			Assert.AreEqual(2, ctx.FailAllPendingRequests("connection lost"));

			foreach (var failure in new[] { first, second })
			{
				var ex = await WaitForFailure(failure);
				Assert.IsInstanceOf<WebsocketRequesterException>(ex);
				Assert.AreEqual(0, ((WebsocketRequesterException)ex).Status, "a lost connection is reported as the existing 'noconnection' status 0");
			}

			Assert.AreEqual(0, ctx.PendingRequestCount);
			Assert.AreEqual(0, ctx.FailAllPendingRequests("nothing left"), "failing twice must be harmless");
		}

		[Test]
		public async Task PendingRequest_TimesOut_WhenNoResponseArrives()
		{
			var ctx = CreateContext(requestTimeoutSeconds: 1);
			var (_, failure) = AddPendingRequest(ctx, "basic/never-answered");
			Assert.AreEqual(1, ctx.PendingRequestCount);

			var ex = await WaitForFailure(failure);
			Assert.IsInstanceOf<WebsocketRequesterException>(ex);
			Assert.AreEqual(504, ((WebsocketRequesterException)ex).Status);
			Assert.AreEqual(0, ctx.PendingRequestCount);
		}

		[Test]
		public void PendingRequest_WithoutTimeout_StaysPending()
		{
			var ctx = CreateContext(requestTimeoutSeconds: 0);
			var (_, failure) = AddPendingRequest(ctx, "basic/patient");
			Thread.Sleep(1500);
			Assert.IsFalse(failure.IsCompleted, "no timeout was configured, so nothing should fail the request");
			Assert.AreEqual(1, ctx.PendingRequestCount);
			ctx.FailAllPendingRequests("test cleanup");
		}
	}
}
