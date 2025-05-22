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
		for (var i = 0; i < sendMessageCount; i++)
		{
			var index = i; // capture i.
			var task = Task.Run(async () =>
			{
				// await Task.Delay(index * 1); // change 1 to a higher number to make test more _likely_ to pass
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
