﻿#if DB_MICROSERVICE

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text.Json;
using System.Threading.RateLimiting;
using UnityEngine;


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


	public class EasyWebSocket : IConnection

	{
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
			_ws.Options.SetBuffer(SendChunkSize, ReceiveChunkSize);
			_uri = new Uri(uri);

			_cancellationToken = _cancellationTokenSource.Token;
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
		public Task SendMessage(string message, Stopwatch sw = null)
		{
			return SendMessageAsync(message, sw);
		}


		/// <summary>
		/// Terminate the socket in a friendly way.
		/// </summary>
		public async Task Close()
		{
			await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutting down", CancellationToken.None);
		}


		private async Task SendMessageAsync(string message, Stopwatch sw)
		{
			if (_ws.State != WebSocketState.Open)
			{
				throw new Exception($"Connection is not open. state=[{_ws.State}]");
			}

			var messageBuffer = Encoding.UTF8.GetBytes(message);

			var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);


			for (var i = 0; i < messagesCount; i++)
			{
				var offset = (SendChunkSize * i);
				var count = SendChunkSize;
				var lastMessage = ((i + 1) == messagesCount);

				if ((count * (i + 1)) > messageBuffer.Length)
				{
					count = messageBuffer.Length - offset;
				}

				await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text,
					lastMessage, _cancellationToken);
			}

			if (sw != null)
			{
				sw.Stop();
				// Log.Debug($"client message time=[{sw.ElapsedMilliseconds}]");
			}
		}


		private async Task ConnectAsync()

		{
			try

			{
				await _ws.ConnectAsync(_uri, _cancellationToken);

				CallOnConnected();
			}

			catch (Exception)

			{
				CallOnDisconnected(false);
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
							Log.Verbose(
								$"Acquiring Permits=[{requiredPermits}] cpu=[{cpu.RollingRatio * 100}] arg=[{arg}] interpolated=[{interpolated}]");
						}

						await tokenLimiter.AcquireAsync(requiredPermits);
					}

					WebSocketReceiveResult result;
					var sw = new Stopwatch();
					var readCount = 0;

					var streamPool = ObjectPool.Create<MemoryStream>(new DefaultPooledObjectPolicy<MemoryStream>());

					cpu.StartSample();
					var stream = streamPool.Get();
					do
					{
						result = await _ws.ReceiveAsync(readSegment, _cancellationToken);

						readCount++;

						if (result.MessageType == WebSocketMessageType.Close)
						{
							await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
								CancellationToken.None);
							CallOnDisconnected(true);
						}
						else
						{
							await stream.WriteAsync(readSegment.Array, 0, result.Count);
						}
					} while (!result.EndOfMessage);

					var payloadSize = ReceiveChunkSize * readCount;
					if (payloadSize > LargeObjectHeapAllocationLimit) // LOH allocation
					{
						GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					}
					
					cpu.EndSample();

					Task.Run(async () =>
					{
						try
						{
							stream.Seek(0, SeekOrigin.Begin);
							var document = await JsonDocument.ParseAsync(stream);
							EmitMessage(document, sw);
						}
						catch (Exception ex)
						{
							Log.Error("websocket dispatch error. " + ex.Message);
						}
						finally
						{
							streamPool.Return(stream);
						}
					});

				}
			}

			catch (Exception ex)
			{
				Log.Debug($"Websocket error=[{ex.GetType().FullName}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
				CallOnDisconnected(false);
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

#endif
