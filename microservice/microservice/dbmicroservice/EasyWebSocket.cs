
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using UnityEngine;
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
		/// <summary>
		/// Completes when the message was written. Its continuations run asynchronously, so a caller's code never runs
		/// on the send loop and can't hold up later messages.
		/// </summary>
		public TaskCompletionSource<bool> completion;
	}

	public class EasyWebSocket : IConnection

	{
		/// <summary>How long a graceful close, or draining the send queue on shutdown, may take before the socket is aborted.</summary>
		private static readonly TimeSpan GracefulCloseLimit = TimeSpan.FromSeconds(5);

		private readonly IMicroserviceArgs _args;

		private int ReceiveChunkSize => _args.ReceiveChunkSize;

		private int SendChunkSize => _args.SendChunkSize;

		public const int LargeObjectHeapAllocationLimit = 85000;


		private readonly ClientWebSocket _ws;

		private readonly Uri _uri;

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly CancellationToken _cancellationToken;


		private Action<EasyWebSocket> _onConnected;

		private Action<EasyWebSocket, JsonDocument, long, Stopwatch> _onMessage;

		private Action<EasyWebSocket, bool> _onDisconnected;


		private long messageNumber = 0;
		private Channel<WriteItem> _sendChannel;
		private Task _sendMessageTask;

		// Teardown runs once, whichever detector gets there first.
		private int _tornDown;

		// When the frame being written started, as a Stopwatch timestamp; 0 while no frame is being written.
		private long _sendStartedAt;
		private readonly TimeSpan _sendTimeout;
		private readonly bool _enforce;
		private readonly Timer _sendWatchdog;
		private int _reportedBlockedSend;


		public WebSocketState State => _ws.State;


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
			// Ping/pong keep-alive only probes a connection with no inbound traffic, so it catches a dead peer on an idle
			// connection but not a connection whose outbound direction broke while inbound traffic keeps flowing.
			if (args.WebSocketKeepAliveTimeoutSeconds > 0)
			{
				_ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(args.WebSocketKeepAliveTimeoutSeconds);
			}
#endif
			_ws.Options.SetBuffer(SendChunkSize, ReceiveChunkSize);
			_uri = new Uri(uri);

			_cancellationToken = _cancellationTokenSource.Token;

			_sendTimeout = TimeSpan.FromSeconds(Math.Max(0, args.SocketSendTimeoutSeconds));
			_enforce = args.EnforceSocketReliability;

			_sendChannel = Channel.CreateUnbounded<WriteItem>(new UnboundedChannelOptions
			{
				// teardown drains the queue from another thread to fail what was never sent.
				SingleReader = false,
				SingleWriter = false,
				AllowSynchronousContinuations = false
			});

			_sendMessageTask = Task.Run(SendLoop);

			if (_sendTimeout > TimeSpan.Zero)
			{
				var period = TimeSpan.FromMilliseconds(Math.Min(1000, _sendTimeout.TotalMilliseconds / 2));
				_sendWatchdog = new Timer(_ => CheckForBlockedSend(), null, period, period);
			}
		}

		private async Task SendLoop()
		{
			try
			{
				while (await _sendChannel.Reader.WaitToReadAsync())
				{
					var count = 0;
					while (_sendChannel.Reader.TryRead(out var item))
					{
						count++;
						if (count > 10)
						{
							/*
							 * I don't understand why this needs to be here,
							 * but it is still the case that without it, the frames get confused.
							 * The count-by-10 thing is just to wait "sometimes", not all the time.
							 */
							await Task.Delay(1);
							count = 0;
						}

						try
						{
							await Write(item);
							item.stopWatch?.Stop();
							item.completion.TrySetResult(true);
						}
						catch (Exception ex)
						{
							// A connection that can't send is no use: fail this message, and let teardown fail the rest
							// and start a reconnect.
							item.completion.TrySetException(ex);
							Teardown("send_failed", $"a send failed. {ex.GetType().Name}: {ex.Message}", wasClean: false, abort: true, LogLevel.Warning);
						}
					}
				}
			}
			catch (Exception ex)
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogError($"The websocket send loop stopped unexpectedly. {ex.GetType().Name}: {ex.Message}");
				Teardown("send_loop_stopped", "the send loop stopped", wasClean: false, abort: true, LogLevel.Error);
			}
			finally
			{
				FailQueued("the connection closed before this message was sent");
			}
		}

		private async Task Write(WriteItem item)
		{
			if (_ws.State != WebSocketState.Open)
			{
				throw new WebSocketException(WebSocketError.InvalidState, $"Connection is not open. state=[{_ws.State}]");
			}

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

			var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);


			for (var i = 0; i < messagesCount; i++)
			{
				var offset = (SendChunkSize * i);
				var count1 = SendChunkSize;
				var lastMessage = ((i + 1) == messagesCount);

				if ((count1 * (i + 1)) > messageBuffer.Length)
				{
					count1 = messageBuffer.Length - offset;
				}

				Volatile.Write(ref _sendStartedAt, Stopwatch.GetTimestamp());
				try
				{
					await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count1), messageType,
						lastMessage, _cancellationToken);
				}
				finally
				{
					Volatile.Write(ref _sendStartedAt, 0);
				}
			}
		}

		/// <summary>
		/// A frame blocked this long means the peer stopped reading, or the connection is dead without having failed yet.
		/// In enforce mode the connection is torn down, which also unblocks the send; otherwise it is reported once.
		/// </summary>
		private void CheckForBlockedSend()
		{
			var started = Volatile.Read(ref _sendStartedAt);
			if (started == 0) return;
			var blocked = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency);
			if (blocked < _sendTimeout) return;

			if (_enforce)
			{
				Teardown("send_blocked", $"a send was blocked for {blocked.TotalSeconds:0}s", wasClean: false, abort: true, LogLevel.Warning);
				return;
			}

			if (Interlocked.Exchange(ref _reportedBlockedSend, 1) == 1) return;
			SocketReliabilityMetrics.UnhealthyConnections.Add(1,
				new KeyValuePair<string, object>("reason", "send_blocked"),
				new KeyValuePair<string, object>("enforced", false));
			BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"A websocket send has been blocked for {blocked.TotalSeconds:0}s. Not closing the connection, because BEAM_SOCKET_RELIABILITY_ENFORCE is off.");
		}

		/// <summary>
		/// Ends the connection, once. Queued messages fail, the disconnect callbacks run (so the owner reconnects) before
		/// any close handshake, and a connection presumed broken is aborted instead of closed: a close handshake needs the
		/// send direction, which may be the part that's broken.
		/// </summary>
		private void Teardown(string reasonTag, string reason, bool wasClean, bool abort, LogLevel level)
		{
			if (Interlocked.Exchange(ref _tornDown, 1) == 1) return;

			_sendWatchdog?.Dispose();
			SocketReliabilityMetrics.Teardowns.Add(1, new KeyValuePair<string, object>("reason", reasonTag));
			var logger = BeamableZLoggerProvider.LogContext.Value;
			switch (level)
			{
				case LogLevel.Error:
					logger.ZLogError($"Closing the websocket connection: {reason}.");
					break;
				case LogLevel.Warning:
					logger.ZLogWarning($"Closing the websocket connection: {reason}.");
					break;
				default:
					logger.ZLogDebug($"Closing the websocket connection: {reason}.");
					break;
			}

			_sendChannel.Writer.TryComplete();
			FailQueued($"the connection closed before this message was sent: {reason}");

			CallOnDisconnected(wasClean);

			if (abort)
			{
				try
				{
					_ws.Abort();
				}
				catch (Exception)
				{
					// it is already gone.
				}
			}
		}

		private void FailQueued(string reason)
		{
			while (_sendChannel.Reader.TryRead(out var item))
			{
				item.completion.TrySetException(new WebSocketException(WebSocketError.InvalidState, reason));
			}
		}

		/// <summary>Closes with a handshake, but aborts if the handshake doesn't finish in time.</summary>
		private async Task CloseGracefully(WebSocketCloseStatus status, string description)
		{
			using var limit = new CancellationTokenSource(GracefulCloseLimit);
			try
			{
				await _ws.CloseAsync(status, description, limit.Token);
			}
			catch (Exception)
			{
				try
				{
					_ws.Abort();
				}
				catch (Exception)
				{
					// it is already gone.
				}
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
		/// Set the Action to call when the connection has been terminated. It is called at most once.
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
		public Task SendMessage(string message, Stopwatch sw = null)
		{
			return Enqueue(new WriteItem { message = message, stopWatch = sw });
		}

		/// <summary>
		/// Send a binary message to the WebSocket server.
		/// </summary>
		/// <param name="data">The binary data to send</param>
		public Task SendBinaryMessage(byte[] data, Stopwatch sw = null)
		{
			return Enqueue(new WriteItem { binaryData = data, stopWatch = sw });
		}

		private Task Enqueue(WriteItem item)
		{
			item.completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			if (!_sendChannel.Writer.TryWrite(item))
			{
				return Task.FromException(new WebSocketException(WebSocketError.InvalidState, "The connection is closed."));
			}

			return item.completion.Task;
		}

		/// <summary>
		/// Tears the connection down at once, because it is presumed broken. The disconnect callbacks run.
		/// </summary>
		public void Abort(string reason)
		{
			Teardown("aborted", reason, wasClean: false, abort: true, LogLevel.Warning);
		}


		/// <summary>
		/// Terminate the socket in a friendly way, within a time limit.
		/// </summary>
		public async Task Close()
		{
			_sendChannel.Writer.TryComplete(); // tell the channel, "no more!"

			// wait for queued messages to be sent, but a blocked send must not hold up shutdown.
			if (await Task.WhenAny(_sendMessageTask, Task.Delay(GracefulCloseLimit)) != _sendMessageTask)
			{
				Teardown("shutdown", "queued messages were not sent in time to close gracefully", wasClean: true, abort: true, LogLevel.Warning);
				return;
			}

			// and now, close the websocket
			await CloseGracefully(WebSocketCloseStatus.NormalClosure, "shutting down");
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
				Teardown("connect_failed", $"could not connect. {ex.GetType().Name}: {ex.Message}", wasClean: false, abort: true, LogLevel.Debug);
			}


			var _ = Task.Factory.StartNew(StartListen, TaskCreationOptions.LongRunning);
		}

		private async Task StartListen()
		{
			var buffer = new byte[ReceiveChunkSize];

			var tokenLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
			{
				QueueLimit = _args.RateLimitWebsocketMaxQueueSize,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				TokenLimit = _args.RateLimitWebsocketTokens,
				TokensPerPeriod = _args.RateLimitWebsocketTokensPerPeriod,
				AutoReplenishment = true,
				ReplenishmentPeriod = TimeSpan.FromSeconds(_args.RateLimitWebsocketPeriodSeconds)
			});

			var cpu = new CpuTracker(windowSize: 10);

			try
			{
				var readSegment = new ArraySegment<byte>(buffer);

				while (_ws.State == WebSocketState.Open)
				{
					if (_args.RateLimitWebsocket)
					{
						var requiredPermits = 1;
						if (cpu.Readable)
						{
							var interpolated =
								cpu.RollingRatio * cpu.RollingRatio * cpu.RollingRatio; // ease-in-cubic on cpu.
							var arg = Mathf.Lerp((float)_args.RateLimitCPUMultiplierLow,
								(float)_args.RateLimitCPUMultiplierHigh,
								(float)cpu.RollingRatio);
							requiredPermits = (int)(interpolated * _args.RateLimitWebsocketTokens * arg);
							requiredPermits = Math.Clamp(requiredPermits - _args.RateLimitCPUOffset, 1,
								_args.RateLimitWebsocketTokens);
							BeamableZLoggerProvider.LogContext.Value.ZLogTrace(
								$"Acquiring Permits=[{requiredPermits}] cpu=[{cpu.RollingRatio * 100}] arg=[{arg}] interpolated=[{interpolated}]");
						}

						await tokenLimiter.AcquireAsync(requiredPermits);
					}

					WebSocketReceiveResult result;
					var sw = new Stopwatch();
					var readCount = 0;

					MemoryStream stream = new MemoryStream();
					cpu.StartSample();
					var byteCount = 0;
					WebSocketMessageType? receivedMessageType = null;
					do
					{
						var segment = new ArraySegment<byte>(readSegment.Array);
						result = await _ws.ReceiveAsync(segment, _cancellationToken);

						readCount++;
						receivedMessageType ??= result.MessageType;

						if (result.MessageType == WebSocketMessageType.Close)
						{
							// Recover first: the close handshake needs the send direction, which may be blocked.
							Teardown("peer_closed", "the gateway closed the connection", wasClean: true, abort: false, LogLevel.Debug);
							await CloseGracefully(WebSocketCloseStatus.NormalClosure, string.Empty);
						}
						else
						{
							byteCount += result.Count;
							await stream.WriteAsync(readSegment.Array, 0, result.Count);
						}
					} while (!result.EndOfMessage);

					var payloadSize = ReceiveChunkSize * readCount;
					if (payloadSize > LargeObjectHeapAllocationLimit) // LOH allocation
					{
						GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					}

					// Handle binary messages by decompressing them
					if (receivedMessageType == WebSocketMessageType.Binary && byteCount > 0)
					{
						var compressedData = stream.ToArray();
						var decompressed = SocketCompression.Decompress(compressedData);
						var decompressedBytes = Encoding.UTF8.GetBytes(decompressed);
						stream = new MemoryStream(decompressedBytes);
					}
					else
					{
						stream.Seek(0, SeekOrigin.Begin);
					}

					JsonDocument document = null;
					if (byteCount > 0)
					{
						document = await JsonDocument.ParseAsync(stream);
					}
					cpu.EndSample();

					if (document != null)
					{
						EmitMessage(document, sw);
					}
				}
			}

			catch (Exception ex)
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Websocket error=[{ex.GetType().FullName}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
				Teardown("receive_failed", $"receiving failed. {ex.GetType().Name}: {ex.Message}", wasClean: false, abort: true, LogLevel.Debug);
			}

			finally
			{
				_ws.Dispose();
			}
		}


		private void EmitMessage(JsonDocument doc, Stopwatch stopwatch)
		{
			if (_onMessage == null) return;
			var next = Interlocked.Increment(ref messageNumber);
			Task.Factory.StartNew(() => _onMessage(this, doc, next, stopwatch), TaskCreationOptions.PreferFairness);
		}


		private void CallOnDisconnected(bool wasClean)

		{
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

