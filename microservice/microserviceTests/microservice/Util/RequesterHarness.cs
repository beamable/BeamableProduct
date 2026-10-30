using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;

namespace microserviceTests.microservice.Util
{
	/// <summary>
	/// A socket requester context, requester and auth daemon wired to a <see cref="TestSocket"/>, without a whole
	/// microservice around them. Replies the test socket produces are routed back through the context the same way
	/// <c>BeamableMicroService</c> routes platform messages.
	/// </summary>
	public class RequesterHarness : IDisposable
	{
		public TestArgs Args { get; }
		public TestSocket Socket { get; }
		public SocketRequesterContext Context { get; }
		public MicroserviceRequester Requester { get; }
		public MicroserviceRequester AuthRequester { get; }
		public MicroserviceAuthenticationDaemon Daemon => Context.Daemon;

		private readonly CancellationTokenSource _daemonCancel = new CancellationTokenSource();

		public RequesterHarness(Action<TestSocket> configureSocket, TestArgs args = null, bool authenticated = true)
		{
			Args = args ?? new TestArgs();
			TestSocket socket = null;
			var provider = new TestSocketProvider(s =>
			{
				socket = s;
				s.SetAuthentication(authenticated);
				configureSocket?.Invoke(s);
			});
			var connection = provider.Create("test", Args);
			Socket = socket;
			Context = new SocketRequesterContext(() => Promise<IConnection>.Successful(connection));
			var reqCtx = new RequestContext(Args.CustomerID, Args.ProjectName, 1, 200, 1, "path", "GET", "");
			Requester = new MicroserviceRequester(Args, reqCtx, Context, true, new NoopActivityProvider());
			AuthRequester = new MicroserviceRequester(Args, null, Context, false, new NoopActivityProvider());
			(_, Context.Daemon) = MicroserviceAuthenticationDaemon.Start(Args, AuthRequester, _daemonCancel);
			Socket.Connect();
			Socket.OnMessage((_, data, _) =>
			{
				if (!data.TryBuildRequestContext(Args, out var rc)) return;
				// replies are handled off the sending thread, as EasyWebSocket does.
				Task.Run(() => Context.HandleMessage(null, rc));
			});
		}

		/// <summary>Total outbound requests the auth daemon has counted, and how many of them it saw finish.</summary>
		public (long sent, long processed) Accounting
		{
			get
			{
				var type = typeof(MicroserviceAuthenticationDaemon);
				var flags = BindingFlags.Instance | BindingFlags.NonPublic;
				var sent = (long)type.GetField("_OutgoingRequestCounter", flags)!.GetValue(Daemon)!;
				var processed = (long)type.GetField("_OutgoingRequestProcessedCounter", flags)!.GetValue(Daemon)!;
				return (sent, processed);
			}
		}

		public void Dispose()
		{
			Daemon?.KillAuthThread();
			_daemonCancel.Cancel();
		}
	}

	public static class PromiseTestExtensions
	{
		/// <summary>
		/// Waits for the promise to finish, up to <paramref name="limit"/>. Returns the error it failed with, or null
		/// on success. Throws <see cref="TimeoutException"/> when the promise is still pending at the limit, which is
		/// the failure these tests exist to catch.
		/// </summary>
		public static async Task<Exception> FinishWithin<T>(this Promise<T> promise, TimeSpan limit, string what)
		{
			var tcs = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
			promise.Then(_ => tcs.TrySetResult(null));
			promise.Error(ex => tcs.TrySetResult(ex));
			var finished = await Task.WhenAny(tcs.Task, Task.Delay(limit));
			if (finished != tcs.Task)
			{
				throw new TimeoutException($"{what} was still pending after {limit.TotalSeconds}s");
			}

			return await tcs.Task;
		}
	}
}
