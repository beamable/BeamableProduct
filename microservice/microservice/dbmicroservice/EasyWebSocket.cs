using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using ZLogger;


namespace Beamable.Server

{
	public class EasyWebSocketProvider : IConnectionProvider

	{
		public IConnection Create(string host, IMicroserviceArgs args)

		{
			var ws = EasyWebSocket.Create(host, args);

			return ws;
		}
	}

	public class WriteItem
	{
		public string message;
		public byte[] binaryData;
		public Stopwatch stopWatch;
		public Promise promise;
	}

	/// <summary>
	/// Thrown when a message cannot be written because the underlying websocket is no longer open.
	/// Callers (see <see cref="SocketRequesterContext.SendMessageSafely"/>) treat this as a signal to
	/// re-resolve the connection and retry, instead of waiting on a write that can never complete.
	/// </summary>
	public class WebsocketNotOpenException : Exception
	{
		public WebSocketState State { get; }

		public WebsocketNotOpenException(WebSocketState state, Exception inner = null)
			: base($"Websocket connection is not open. state=[{state}]", inner)
		{
			State = state;
		}
	}

	public class EasyWebSocket : IConnection

	{
		private readonly IMicroserviceArgs _args;

		private int ReceiveChunkSize => _args.ReceiveChunkSize;

		private int SendChunkSize => _args.SendChunkSize;

		public const int LargeObjectHeapAllocationLimit = 85000;

		/// <summary>
		/// The receive buffer is reused between messages. Once it has grown past this size (a single very
		/// large payload), it is thrown away after the message so the process does not permanently hold on
		/// to a large-object-heap allocation.
		/// </summary>
		private const int MaxRetainedReceiveBufferBytes = 4 * 1024 * 1024;


		private readonly ClientWebSocket _ws;

		private readonly Uri _uri;

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly CancellationToken _cancellationToken;


		private Action<EasyWebSocket> _onConnected;

		private Action<EasyWebSocket, JsonDocument, long, Stopwatch> _onMessage;

		private Action<EasyWebSocket, bool> _onDisconnected;


		private long messageNumber = 0;
		private readonly Channel<WriteItem> _sendChannel;
		private readonly Task _sendMessageTask;

		// default is 0 (false), set to 1 when the disconnect callback has fired, so it only ever fires once per connection.
		private int _disconnectSignaled = 0;


		public WebSocketState State => _ws.State;

		/// <summary>
		/// The number of outbound messages that have been queued but not yet written to the socket.
		/// </summary>
		public int PendingSendCount => _sendChannel.Reader.CanCount ? _sendChannel.Reader.Count : -1;


		protected EasyWebSocket(string uri, IMicroserviceArgs args)
		{
			_args = args;

			_ws = new ClientWebSocket();

			// disable that by default, allow setup from EnviornmentArgs

			if (args.EnableDangerousDeflateOptions)
			{
				_ws.Options.DangerousDeflateOptions =
					new WebSocketDeflateOptions { ServerContextTakeover = true, ClientMaxWindowBits = 15 };
			}

			_ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
#if NET9_0_OR_GREATER
			// Without a pong deadline a half-open TCP connection (gateway node lost without a FIN, or a
			// network partition) is only noticed when the OS gives up on retransmits, which can take many
			// minutes. During that time the service believes it is connected while no traffic can reach
			// it. With a deadline the runtime aborts the socket, which flows into the reconnect logic.
			if (args.WebsocketKeepAliveTimeoutSeconds > 0)
			{
				_ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(args.WebsocketKeepAliveTimeoutSeconds);
			}
#endif
			_ws.Options.SetBuffer(ReceiveChunkSize, SendChunkSize);
			_uri = new Uri(uri);

			_cancellationToken = _cancellationTokenSource.Token;


			_sendChannel = Channel.CreateUnbounded<WriteItem>(new UnboundedChannelOptions
			{
				SingleReader = true,
				SingleWriter = false,
				AllowSynchronousContinuations = false
			});

			_sendMessageTask = Task.Run(SendLoop);
		}

		/// <summary>
		/// The single writer for the socket. <see cref="ClientWebSocket.SendAsync(ArraySegment{byte},WebSocketMessageType,bool,CancellationToken)"/>
		/// must never be invoked concurrently, so every outbound message is funneled through this loop.
		/// <para/>
		/// If the socket stops being writable, the loop stops and every queued (and future) write is failed
		/// immediately instead of being left pending forever. A pending write that never completes would
		/// otherwise pin the request handler that produced it, which in turn holds the shutdown grace period
		/// hostage and leaks the task table.
		/// </summary>
		private async Task SendLoop()
		{
			Exception failure = null;
			try
			{
				while (await _sendChannel.Reader.WaitToReadAsync(_cancellationToken))
				{
					while (_sendChannel.Reader.TryRead(out var item))
					{
						try
						{
							if (_ws.State != WebSocketState.Open)
							{
								throw new WebsocketNotOpenException(_ws.State);
							}

							await WriteItemAsync(item);
							item.stopWatch?.Stop();
							item.promise.CompleteSuccess();
						}
						catch (Exception ex)
						{
							item.promise.CompleteError(ex);
							throw;
						}
					}
				}
			}
			catch (Exception ex)
			{
				failure = ex;
			}
			finally
			{
				FailPendingSends(failure);
			}
		}

		private async Task WriteItemAsync(WriteItem item)
		{
			byte[] messageBuffer;
			WebSocketMessageType messageType;

			if (item.binaryData != null)
			{
				messageBuffer = item.binaryData;
				messageType = WebSocketMessageType.Binary;
			}
			else
			{
				messageBuffer = Encoding.UTF8.GetBytes(item.message);
				messageType = WebSocketMessageType.Text;
			}

			var chunkSize = Math.Max(1, SendChunkSize);
			var messagesCount = Math.Max(1, (int)Math.Ceiling((double)messageBuffer.Length / chunkSize));

			for (var i = 0; i < messagesCount; i++)
			{
				var offset = chunkSize * i;
				var count = Math.Min(chunkSize, messageBuffer.Length - offset);
				var lastMessage = (i + 1) == messagesCount;

				await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), messageType,
					lastMessage, _cancellationToken);
			}
		}

		/// <summary>
		/// Stops accepting writes and fails everything that is still queued.
		/// Safe to call multiple times.
		/// </summary>
		private void FailPendingSends(Exception reason)
		{
			reason ??= new WebsocketNotOpenException(_ws.State);
			_sendChannel.Writer.TryComplete(reason);
			while (_sendChannel.Reader.TryRead(out var item))
			{
				item.promise.CompleteError(reason);
			}
		}


		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="uri">The URI of the WebSocket server.</param>
		/// <returns></returns>
		public static EasyWebSocket Create(string uri, IMicroserviceArgs args)
		{
			return new EasyWebSocket(uri, args);
		}


		/// <summary>
		/// Connects to the WebSocket server.
		/// </summary>
		/// <returns></returns>
		public IConnection Connect()

		{
			var connectAsync = ConnectAsync();
			connectAsync.Wait(_cancellationToken);
			return this;
		}


		/// <summary>
		/// Set the Action to call when the connection has been established.
		/// </summary>
		/// <param name="onConnect">The Action to call.</param>
		/// <returns></returns>
		public IConnection OnConnect(Action<IConnection> onConnect)

		{
			_onConnected += onConnect;

			return this;
		}


		/// <summary>
		/// Set the Action to call when the connection has been terminated.
		/// </summary>
		/// <param name="onDisconnect">The Action to call</param>
		/// <returns></returns>
		public IConnection OnDisconnect(Action<IConnection, bool> onDisconnect)

		{
			_onDisconnected += onDisconnect;

			return this;
		}


		public IConnection OnMessage(Action<IConnection, JsonDocument, long> onMessage) =>
			OnMessage((c, msg, id, _) => onMessage(c, msg, id));

		/// <summary>
		/// Set the Action to call when a messages has been received.
		/// </summary>
		/// <param name="onMessage">The Action to call.</param>
		/// <returns></returns>
		public IConnection OnMessage(Action<IConnection, JsonDocument, long, Stopwatch> onMessage)
		{
			_onMessage = onMessage;
			return this;
		}


		/// <summary>
		/// Send a message to the WebSocket server.
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <exception cref="WebsocketNotOpenException">when the socket is closed, or closes before the message is written</exception>
		public Task SendMessage(string message, Stopwatch sw = null)
		{
			return Enqueue(new WriteItem { message = message, stopWatch = sw, promise = new Promise() });
		}

		/// <summary>
		/// Send a binary message to the WebSocket server.
		/// </summary>
		/// <param name="data">The binary data to send</param>
		/// <exception cref="WebsocketNotOpenException">when the socket is closed, or closes before the message is written</exception>
		public Task SendBinaryMessage(byte[] data, Stopwatch sw = null)
		{
			return Enqueue(new WriteItem { binaryData = data, stopWatch = sw, promise = new Promise() });
		}

		private async Task Enqueue(WriteItem item)
		{
			try
			{
				await _sendChannel.Writer.WriteAsync(item);
			}
			catch (ChannelClosedException ex)
			{
				throw new WebsocketNotOpenException(_ws.State, ex.InnerException ?? ex);
			}

			await item.promise;
		}


		/// <summary>
		/// Terminate the socket in a friendly way.
		/// </summary>
		public async Task Close()
		{
			_sendChannel.Writer.TryComplete(); // tell the channel, "no more!"
			try
			{
				await _sendMessageTask; // wait for all sending messages to send
			}
			catch
			{
				// the loop reports its own failures through the queued promises.
			}

			// and now, close the websocket
			if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
			{
				try
				{
					await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutting down", CancellationToken.None);
				}
				catch (Exception ex)
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Websocket close failed. type=[{ex.GetType().Name}] message=[{ex.Message}]");
				}
			}
		}


		private async Task ConnectAsync()

		{
			try

			{
				await _ws.ConnectAsync(_uri, _cancellationToken);

				CallOnConnected();
			}

			catch (Exception ex)

			{
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Websocket connect failed. uri=[{_uri.Host}] type=[{ex.GetType().Name}] message=[{ex.Message}]");
				FailPendingSends(new WebsocketNotOpenException(_ws.State, ex));
				CallOnDisconnected(false);
				return;
			}


			var _ = Task.Factory.StartNew(StartListen, TaskCreationOptions.LongRunning);
		}

		private RateLimiter CreateRateLimiter()
		{
			if (!_args.RateLimitWebsocket) return null;

			// A plain token bucket. This is only a safety valve against a runaway flood of inbound frames;
			// it deliberately does NOT slow down reads when the process is busy. Slowing the read loop never
			// reduces the amount of work the service has to do -- it only delays that work past the gateway's
			// response deadline, which turns an overloaded-but-recovering service into a wall of timeouts.
			// Overload is handled explicitly by the request-level load shedding in BeamableMicroService.
			return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
			{
				QueueLimit = _args.RateLimitWebsocketMaxQueueSize,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				TokenLimit = _args.RateLimitWebsocketTokens,
				TokensPerPeriod = _args.RateLimitWebsocketTokensPerPeriod,
				AutoReplenishment = true,
				ReplenishmentPeriod = TimeSpan.FromSeconds(_args.RateLimitWebsocketPeriodSeconds)
			});
		}

		private async Task StartListen()
		{
			var buffer = new byte[ReceiveChunkSize];
			var stream = new MemoryStream();
			var tokenLimiter = CreateRateLimiter();
			var wasClean = false;

			try
			{
				while (_ws.State == WebSocketState.Open)
				{
					if (tokenLimiter != null)
					{
						using var lease = await tokenLimiter.AcquireAsync(1, _cancellationToken);
					}

					var sw = new Stopwatch();
					var byteCount = 0;
					WebSocketMessageType? receivedMessageType = null;
					ValueWebSocketReceiveResult result;

					// the buffer from the previous message is reused; drop it if it grew very large.
					if (stream.Capacity > MaxRetainedReceiveBufferBytes)
					{
						stream = new MemoryStream();
					}
					else
					{
						stream.SetLength(0);
					}

					do
					{
						result = await _ws.ReceiveAsync(new Memory<byte>(buffer), _cancellationToken);
						if (!sw.IsRunning)
						{
							sw.Start();
						}

						receivedMessageType ??= result.MessageType;

						if (result.MessageType == WebSocketMessageType.Close)
						{
							wasClean = true;
							try
							{
								await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
									CancellationToken.None);
							}
							catch (Exception ex)
							{
								BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Websocket close handshake failed. type=[{ex.GetType().Name}] message=[{ex.Message}]");
							}
						}
						else
						{
							byteCount += result.Count;
							stream.Write(buffer, 0, result.Count);
						}
					} while (!result.EndOfMessage);

					if (wasClean)
					{
						break;
					}

					if (byteCount == 0)
					{
						continue;
					}

					JsonDocument document;
					if (receivedMessageType == WebSocketMessageType.Binary)
					{
						// binary frames are compressed json. The decompressed bytes are owned by the
						// document (no copy), which is fine because they are a fresh allocation.
						var decompressed = SocketCompression.DecompressToBytes(stream.GetBuffer(), (int)stream.Length);
						document = JsonDocument.Parse(new ReadOnlyMemory<byte>(decompressed));
					}
					else
					{
						// JsonDocument.Parse(Stream) copies the bytes into its own buffer, so the
						// receive stream can be reused for the next message.
						stream.Position = 0;
						document = JsonDocument.Parse(stream);
					}

					EmitMessage(document, sw);
				}
			}

			catch (Exception ex)
			{
				var level = _cancellationToken.IsCancellationRequested ? Microsoft.Extensions.Logging.LogLevel.Debug : Microsoft.Extensions.Logging.LogLevel.Warning;
				BeamableZLoggerProvider.LogContext.Value.ZLog(level, $"Websocket receive loop ended. error=[{ex.GetType().FullName}] message=[{ex.Message}] state=[{_ws.State}]");
			}

			finally
			{
				// nothing can be written anymore; release anything still waiting to be sent.
				FailPendingSends(new WebsocketNotOpenException(_ws.State));
				tokenLimiter?.Dispose();
				CallOnDisconnected(wasClean);
				_ws.Dispose();
			}
		}


		private void EmitMessage(JsonDocument doc, Stopwatch stopwatch)
		{
			if (_onMessage == null)
			{
				doc.Dispose();
				return;
			}
			var next = Interlocked.Increment(ref messageNumber);
			Task.Factory.StartNew(() => _onMessage(this, doc, next, stopwatch), TaskCreationOptions.PreferFairness);
		}


		private void CallOnDisconnected(bool wasClean)

		{
			// only ever signal the disconnect once, no matter how many code paths notice it.
			if (Interlocked.CompareExchange(ref _disconnectSignaled, 1, 0) != 0) return;

			if (_onDisconnected != null)

				RunInTask(() => _onDisconnected(this, wasClean));
		}


		private void CallOnConnected()

		{
			if (_onConnected != null)

				RunInTask(() => _onConnected(this));
		}


		private static void RunInTask(Action action)
		{
			Task.Run(action);
		}
	}
}
