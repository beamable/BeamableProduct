using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace microserviceTests.microservice.Util
{
	/// <summary>
	/// A minimal in-process WebSocket server over a raw TCP listener, so a test controls exactly what the peer does:
	/// stop reading (so the client's sends back up), send a Close frame, or reset the connection.
	/// </summary>
	public sealed class RawWebSocketServer : IDisposable
	{
		private readonly TcpListener _listener;
		private readonly ConcurrentQueue<ServerConnection> _connections = new ConcurrentQueue<ServerConnection>();
		private readonly CancellationTokenSource _stop = new CancellationTokenSource();

		public string Url { get; }

		private readonly Channel<ServerConnection> _accepted = Channel.CreateUnbounded<ServerConnection>();

		/// <summary>The next accepted connection, after the WebSocket handshake, in the order they were accepted.</summary>
		public Task<ServerConnection> NextConnection => _accepted.Reader.ReadAsync().AsTask();

		public RawWebSocketServer(int receiveBufferSize = 4096)
		{
			_listener = new TcpListener(IPAddress.Loopback, 0);
			// A small receive window makes the client's send side fill up quickly once the server stops reading.
			_listener.Server.ReceiveBufferSize = receiveBufferSize;
			_listener.Start();
			Url = $"ws://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/socket";
			_ = AcceptLoop();
		}

		private async Task AcceptLoop()
		{
			while (!_stop.IsCancellationRequested)
			{
				TcpClient client;
				try
				{
					client = await _listener.AcceptTcpClientAsync(_stop.Token);
				}
				catch
				{
					return;
				}

				try
				{
					var connection = await ServerConnection.Handshake(client);
					_connections.Enqueue(connection);
					_accepted.Writer.TryWrite(connection);
				}
				catch (Exception ex)
				{
					_accepted.Writer.TryComplete(ex);
					return;
				}
			}
		}

		public void Dispose()
		{
			_stop.Cancel();
			_listener.Stop();
			while (_connections.TryDequeue(out var c))
			{
				c.Reset();
			}
		}
	}

	public sealed class ServerConnection
	{
		private readonly TcpClient _client;
		public WebSocket Socket { get; }

		private ServerConnection(TcpClient client, WebSocket socket)
		{
			_client = client;
			Socket = socket;
		}

		internal static async Task<ServerConnection> Handshake(TcpClient client)
		{
			var stream = client.GetStream();
			var request = new StringBuilder();
			var buffer = new byte[1];
			while (!request.ToString().EndsWith("\r\n\r\n"))
			{
				if (await stream.ReadAsync(buffer, 0, 1) == 0) throw new InvalidOperationException("closed during handshake");
				request.Append((char)buffer[0]);
			}

			string key = null;
			foreach (var line in request.ToString().Split("\r\n"))
			{
				if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
				{
					key = line.Substring("Sec-WebSocket-Key:".Length).Trim();
				}
			}

			var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
			var response = "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
			               $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
			var bytes = Encoding.ASCII.GetBytes(response);
			await stream.WriteAsync(bytes, 0, bytes.Length);

			var socket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
			{
				IsServer = true,
				KeepAliveInterval = TimeSpan.Zero
			});
			return new ServerConnection(client, socket);
		}

		/// <summary>Read and discard everything the client sends, until the connection ends.</summary>
		public async Task DrainAsync()
		{
			var buffer = new byte[64 * 1024];
			try
			{
				while (Socket.State == WebSocketState.Open)
				{
					await Socket.ReceiveAsync(buffer, CancellationToken.None);
				}
			}
			catch
			{
				// the connection ended.
			}
		}

		public Task SendTextAsync(string text) =>
			Socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

		/// <summary>Sends a Close frame without waiting for the client's reply, so it works while the server is not reading.</summary>
		public Task SendCloseAsync() =>
			Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "test close", CancellationToken.None);

		/// <summary>Resets the TCP connection: the client's next read or write fails.</summary>
		public void Reset()
		{
			try
			{
				_client.Client.LingerState = new LingerOption(true, 0);
				_client.Client.Close();
			}
			catch
			{
				// already closed.
			}
		}
	}
}
