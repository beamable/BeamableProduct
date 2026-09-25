using Beamable.Common;
using Beamable.Server;
using microserviceTests.microservice;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace microserviceTests;

public class EasyWebSocketTest : CommonTest
{
	[Test]
	[NonParallelizable]
	public async Task TestSendingLotsOfStuff()
	{
		var port = 8787;
		var uri = $"ws://localhost:{port}";
		
		// need to turn on a websocket server... 
		var server = new WebSocketServer(uri);
		var serverInstances = new List<MessageCounterServer>();
		server.AddWebSocketService<MessageCounterServer>("/sample", sample =>
		{
			serverInstances.Add(sample);
		});
		server.Start();
		
		// fluff time for the server to start.
		await Task.Delay(10);

		var client = EasyWebSocket.Create(uri + "/sample", new TestArgs
		{
			// set the send-chunk-size silly low so that our messages get chunked!
			SendChunkSize = 2
		});

		client.Connect();
		
		// fluff time for the client to connect
		await Task.Delay(10);
		
		var sendMessageCount = 10_000;
		var tasks = new List<Task>();
		// The test server (websocket-sharp) reads frames through nested synchronous continuations, and
		// overflows its stack when tens of thousands of tiny frames are already buffered. Send in short
		// bursts with a pause between them so the server's reader gets to unwind; the client itself no
		// longer paces its writes.
		const int burstSize = 8;
		for (var i = 0; i < sendMessageCount; i++)
		{
			var index = i; // capture i.
			var task = Task.Run(async () =>
			{
				await Task.Delay(index / burstSize); // ~burstSize messages per millisecond tick
				await client.SendMessage("msg " + index);
			});
			tasks.Add(task);
		}

		await Task.WhenAll(tasks);
		await Task.Delay(10);

		await client.Close();

		server.Stop();

		var count = serverInstances.Sum(x => x.messageCount);
		Console.WriteLine("count is " + count);
		Assert.That(count, Is.EqualTo(sendMessageCount), "sent messages do not equal the received count.");
	}

	[Test]
	[NonParallelizable]
	public async Task TestSendFailsFastAfterTheServerCloses()
	{
		var port = 8788;
		var uri = $"ws://localhost:{port}";

		var server = new WebSocketServer(uri);
		server.AddWebSocketService<MessageCounterServer>("/sample");
		server.Start();
		await Task.Delay(10);

		var client = EasyWebSocket.Create(uri + "/sample", new TestArgs());
		var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		client.OnDisconnect((_, __) => disconnected.TrySetResult(true));
		client.Connect();
		await Task.Delay(10);

		// the socket works while it is open...
		await client.SendMessage("hello");

		// ...then the server goes away.
		server.Stop();
		await Task.WhenAny(disconnected.Task, Task.Delay(5000));
		Assert.IsTrue(disconnected.Task.IsCompleted, "the client never noticed that the server closed the connection");

		// A write on the dead socket must fail promptly. Before, it was queued behind a writer loop that
		// had already died, and the caller waited forever.
		var send = client.SendMessage("too late");
		var finished = await Task.WhenAny(send, Task.Delay(5000));
		Assert.AreSame(send, finished, "a send on a dead socket hung instead of failing");
		Assert.IsTrue(send.IsFaulted, "a send on a dead socket must fail");
		Assert.IsInstanceOf<WebsocketNotOpenException>(send.Exception?.InnerException);
	}

	public class MessageCounterServer : WebSocketBehavior
	{
		public int messageCount;
		
		protected override void OnMessage(MessageEventArgs e)
		{
			Interlocked.Increment(ref messageCount);
		}

		protected override void OnError(ErrorEventArgs e)
		{
			BeamableLogger.Log("ERROR : " + e.Message);
		}
	}
	
	
}
