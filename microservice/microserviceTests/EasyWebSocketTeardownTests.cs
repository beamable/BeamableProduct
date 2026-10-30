using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Server;
using microserviceTests.microservice;
using microserviceTests.microservice.Util;
using NUnit.Framework;

namespace microserviceTests
{
	/// <summary>
	/// <see cref="EasyWebSocket"/> against a real peer that misbehaves: stops reading, sends a Close frame while our
	/// sends are blocked, or resets the connection (beamable/BeamableProduct#4834, teardown comment).
	/// </summary>
	[TestFixture]
	[NonParallelizable]
	public class EasyWebSocketTeardownTests : CommonTest
	{
		private sealed class Client
		{
			public EasyWebSocket Socket;
			public ServerConnection Server;
			public int Disconnects;
			public readonly TaskCompletionSource<bool> Disconnected =
				new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			public readonly List<Task> PendingSends = new List<Task>();
		}

		private static async Task<Client> Connect(RawWebSocketServer server, TestArgs args)
		{
			var client = new Client();
			var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			client.Socket = EasyWebSocket.Create(server.Url, args);
			client.Socket.OnConnect(_ => connected.TrySetResult(true));
			client.Socket.OnDisconnect((_, clean) =>
			{
				Interlocked.Increment(ref client.Disconnects);
				client.Disconnected.TrySetResult(clean);
			});
			client.Socket.Connect();
			Assert.That(await Task.WhenAny(connected.Task, Task.Delay(5000)), Is.SameAs(connected.Task), "connect");
			client.Server = await server.NextConnection;
			return client;
		}

		/// <summary>Sends until a send stays blocked, because the server stopped reading and the TCP buffers are full.</summary>
		private static async Task FillUntilSendBlocks(Client client)
		{
			var payload = new string('x', 256 * 1024);
			for (var i = 0; i < 800; i++)
			{
				var send = client.Socket.SendMessage(payload);
				if (await Task.WhenAny(send, Task.Delay(1000)) != send)
				{
					client.PendingSends.Add(send);
					return;
				}
			}

			Assert.Fail("sends never blocked; the peer's buffers never filled");
		}

		private static TestArgs Args(int sendTimeoutSeconds, bool enforce) => new TestArgs
		{
			SendChunkSize = 64 * 1024,
			SocketSendTimeoutSeconds = sendTimeoutSeconds,
			EnforceSocketReliability = enforce
		};

		[Test]
		[TimeoutWithTeardown(60 * 1000)]
		public async Task StuckSend_TimesOut_TearsDownOnce_AndFailsQueuedSends()
		{
			allowErrorLogs = true;
			using var server = new RawWebSocketServer();
			var client = await Connect(server, Args(sendTimeoutSeconds: 2, enforce: true));

			await FillUntilSendBlocks(client);
			for (var i = 0; i < 3; i++)
			{
				client.PendingSends.Add(client.Socket.SendMessage("queued behind the blocked send"));
			}

			Assert.That(await Task.WhenAny(client.Disconnected.Task, Task.Delay(6000)), Is.SameAs(client.Disconnected.Task),
				"a send blocked past the send timeout must tear the connection down");
			await Task.WhenAny(Task.WhenAll(client.PendingSends), Task.Delay(3000));
			Assert.That(client.PendingSends.All(t => t.IsCompleted), Is.True, "every blocked or queued send must finish");
			Assert.That(client.PendingSends.All(t => t.IsFaulted), Is.True, "and none of them was delivered");
			await Task.Delay(1000);
			Assert.That(client.Disconnects, Is.EqualTo(1), "teardown runs once");
		}

		[Test]
		[TimeoutWithTeardown(60 * 1000)]
		public async Task CloseFrameWhileSendIsBlocked_DisconnectsPromptly()
		{
			using var server = new RawWebSocketServer();
			var client = await Connect(server, Args(sendTimeoutSeconds: 0, enforce: false));

			await FillUntilSendBlocks(client);
			await client.Server.SendCloseAsync();

			Assert.That(await Task.WhenAny(client.Disconnected.Task, Task.Delay(3000)), Is.SameAs(client.Disconnected.Task),
				"recovery must not wait for a close handshake that needs the blocked send direction");
		}

		[Test]
		[TimeoutWithTeardown(60 * 1000)]
		public async Task SendAfterConnectionReset_FailsInsteadOfHanging()
		{
			using var server = new RawWebSocketServer();
			var client = await Connect(server, Args(sendTimeoutSeconds: 30, enforce: false));
			_ = client.Server.DrainAsync();

			client.Server.Reset();
			Assert.That(await Task.WhenAny(client.Disconnected.Task, Task.Delay(3000)), Is.SameAs(client.Disconnected.Task));

			var send = client.Socket.SendMessage("after the reset");
			Assert.That(await Task.WhenAny(send, Task.Delay(2000)), Is.SameAs(send), "a send on a dead connection must fail, not wait forever");
			Assert.That(send.IsFaulted, Is.True);
		}

		[Test]
		[TimeoutWithTeardown(60 * 1000)]
		public async Task Close_FinishesInBoundedTime_WhenSendIsBlocked()
		{
			allowErrorLogs = true;
			using var server = new RawWebSocketServer();
			var client = await Connect(server, Args(sendTimeoutSeconds: 0, enforce: false));

			await FillUntilSendBlocks(client);
			var close = client.Socket.Close();

			Assert.That(await Task.WhenAny(close, Task.Delay(15000)), Is.SameAs(close), "shutdown must not hang on a blocked send");
		}

		[Test]
		[TimeoutWithTeardown(60 * 1000)]
		public async Task SendCompletion_DoesNotRunCallerCodeOnTheSendLoop()
		{
			using var server = new RawWebSocketServer();
			var client = await Connect(server, Args(sendTimeoutSeconds: 30, enforce: false));
			_ = client.Server.DrainAsync();

			// A caller whose code after `await SendMessage` blocks must not stall the next caller's send. An await
			// continuation runs wherever the awaited task completes, so this checks where the send completes it.
			// Repeated, because a send that finishes before the await is registered continues on the caller's thread.
			for (var i = 0; i < 5; i++)
			{
				var release = new ManualResetEventSlim(false);
				async Task SendThenBlock()
				{
					await client.Socket.SendMessage("first");
					release.Wait(5000);
				}

				var first = Task.Run(SendThenBlock);
				await Task.Delay(100);
				var second = client.Socket.SendMessage("second");
				var finished = await Task.WhenAny(second, Task.Delay(2000));
				release.Set();
				await first;

				Assert.That(finished, Is.SameAs(second), "a caller's blocked continuation stalled every later send");
			}
		}
	}

	/// <summary>
	/// Pins down how .NET's own keep-alive behaves, because the fix depends on it (dotnet/runtime
	/// ManagedWebSocket.KeepAlive.cs: inbound data postpones the next ping).
	/// </summary>
	[TestFixture]
	[NonParallelizable]
	public class ClientWebSocketKeepAliveCharacterizationTests
	{
		private static async Task<(ClientWebSocket client, ServerConnection server)> Connect(RawWebSocketServer server)
		{
			var client = new ClientWebSocket();
			client.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(500);
			client.Options.KeepAliveTimeout = TimeSpan.FromMilliseconds(500);
			await client.ConnectAsync(new Uri(server.Url), CancellationToken.None);
			var connection = await server.NextConnection;
			_ = ReadAll(client);
			return (client, connection);
		}

		private static async Task ReadAll(ClientWebSocket client)
		{
			var buffer = new byte[1024];
			try
			{
				while (client.State == WebSocketState.Open)
				{
					await client.ReceiveAsync(buffer, CancellationToken.None);
				}
			}
			catch
			{
				// aborted.
			}
		}

		[Test]
		[Timeout(30 * 1000)]
		public async Task KeepAliveTimeout_DoesNotFire_WhileInboundTrafficFlows_EvenIfPeerNeverAnswersPings()
		{
			using var server = new RawWebSocketServer();
			var (client, connection) = await Connect(server);

			// The server never reads, so it never answers a ping; but it keeps sending, as the gateway keeps routing.
			var until = DateTime.UtcNow.AddSeconds(4);
			while (DateTime.UtcNow < until)
			{
				await connection.SendTextAsync("{}");
				await Task.Delay(100);
			}

			Assert.That(client.State, Is.EqualTo(WebSocketState.Open),
				"inbound data postpones every ping, so a dead outbound direction goes undetected");
			client.Abort();
		}

		[Test]
		[Timeout(30 * 1000)]
		public async Task KeepAliveTimeout_Fires_OnAnIdleConnectionWhosePeerNeverAnswers()
		{
			using var server = new RawWebSocketServer();
			var (client, _) = await Connect(server);

			await Task.Delay(4000);

			Assert.That(client.State, Is.EqualTo(WebSocketState.Aborted));
		}
	}
}
