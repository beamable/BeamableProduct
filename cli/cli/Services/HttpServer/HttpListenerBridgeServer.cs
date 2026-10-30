using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;
using System.Net;

namespace cli.Services.HttpServer;

/// <summary>
/// An ASP.NET Core server whose requests come from `server serve`'s <see cref="HttpListener"/>, so an in-process
/// ASP.NET app (the MCP SDK's Streamable HTTP handler) answers on the same port as /execute. The listener keeps
/// owning the connection: it authorizes the request first and closes the response after.
/// </summary>
public sealed class HttpListenerBridgeServer : IServer
{
	private Func<HttpListenerContext, Stream, CancellationToken, Task> _process;

	public IFeatureCollection Features { get; } = new FeatureCollection();

	public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
		where TContext : notnull
	{
		_process = (listenerContext, body, aborted) => ProcessAsync(application, listenerContext, body, aborted);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_process = null;
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_process = null;
	}

	/// <summary>Runs <paramref name="listenerContext"/> through the app. The caller closes the response.</summary>
	/// <param name="body">The request body, when the caller already read it; otherwise the listener's stream.</param>
	/// <param name="aborted">
	/// Signals that the client has given up on the request. HttpListener cannot see a client disconnect until a
	/// write fails, so callers that learn of it another way pass it here; it becomes <c>RequestAborted</c>.
	/// </param>
	public Task HandleAsync(HttpListenerContext listenerContext, Stream body = null, CancellationToken aborted = default) =>
		(_process ?? throw new InvalidOperationException("The bridged app is not running."))(listenerContext, body, aborted);

	private static async Task ProcessAsync<TContext>(IHttpApplication<TContext> application, HttpListenerContext listenerContext,
		Stream body, CancellationToken aborted)
		where TContext : notnull
	{
		var req = listenerContext.Request;
		var headers = new HeaderDictionary();
		foreach (var name in req.Headers.AllKeys)
		{
			if (name != null)
			{
				headers[name] = req.Headers.GetValues(name);
			}
		}

		var response = new ListenerResponseFeature(listenerContext.Response, aborted);
		var features = new FeatureCollection();
		features.Set<IHttpRequestFeature>(new HttpRequestFeature
		{
			Protocol = $"HTTP/{req.ProtocolVersion}",
			Scheme = req.Url!.Scheme,
			Method = req.HttpMethod,
			PathBase = "",
			Path = Uri.UnescapeDataString(req.Url.AbsolutePath),
			QueryString = req.Url.Query,
			RawTarget = req.RawUrl ?? req.Url.PathAndQuery,
			Headers = headers,
			Body = body ?? req.InputStream
		});
		features.Set<IHttpResponseFeature>(response);
		features.Set<IHttpResponseBodyFeature>(response);
		features.Set<IHttpRequestLifetimeFeature>(response);
		features.Set<IHttpConnectionFeature>(new HttpConnectionFeature
		{
			ConnectionId = req.RequestTraceIdentifier.ToString(),
			LocalIpAddress = req.LocalEndPoint.Address,
			LocalPort = req.LocalEndPoint.Port,
			RemoteIpAddress = req.RemoteEndPoint.Address,
			RemotePort = req.RemoteEndPoint.Port
		});

		var context = application.CreateContext(features);
		Exception error = null;
		try
		{
			await application.ProcessRequestAsync(context);
		}
		catch (Exception ex)
		{
			error = ex;
			if (!response.HasStarted)
			{
				response.StatusCode = StatusCodes.Status500InternalServerError;
			}
		}

		try
		{
			await response.CompleteAsync();
		}
		catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException)
		{
			// The client is gone; nothing left to send.
		}
		finally
		{
			await response.FireOnCompletedAsync();
			application.DisposeContext(context, error);
		}
	}

	/// <summary>
	/// Maps ASP.NET's response onto <see cref="HttpListenerResponse"/>. Status and headers can change until the
	/// first body byte; then they are copied over and the body streams through (chunked, so SSE flushes go out).
	/// </summary>
	private sealed class ListenerResponseFeature : IHttpResponseFeature, IHttpResponseBodyFeature, IHttpRequestLifetimeFeature
	{
		private readonly HttpListenerResponse _response;
		private readonly List<(Func<object, Task> callback, object state)> _onStarting = new();
		private readonly List<(Func<object, Task> callback, object state)> _onCompleted = new();
		private readonly CancellationTokenSource _aborted;
		private readonly BodyStream _body;
		private PipeWriter _writer;

		public ListenerResponseFeature(HttpListenerResponse response, CancellationToken aborted)
		{
			_response = response;
			_aborted = CancellationTokenSource.CreateLinkedTokenSource(aborted);
			_body = new BodyStream(this);
		}

		public int StatusCode { get; set; } = StatusCodes.Status200OK;
		public string ReasonPhrase { get; set; }
		public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
		public bool HasStarted { get; private set; }

		[Obsolete("Use IHttpResponseBodyFeature.Stream")]
		public Stream Body
		{
			get => _body;
			set => throw new NotSupportedException();
		}

		public Stream Stream => _body;
		public PipeWriter Writer => _writer ??= PipeWriter.Create(_body, new StreamPipeWriterOptions(leaveOpen: true));

		public CancellationToken RequestAborted
		{
			get => _aborted.Token;
			set => throw new NotSupportedException();
		}

		public void Abort() => _aborted.Cancel();

		public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));
		public void OnCompleted(Func<object, Task> callback, object state) => _onCompleted.Add((callback, state));

		public void DisableBuffering()
		{
			// Every write goes straight to the listener already.
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			if (HasStarted)
			{
				return;
			}
			HasStarted = true;

			// ASP.NET runs OnStarting callbacks in reverse registration order.
			for (var i = _onStarting.Count - 1; i >= 0; i--)
			{
				await _onStarting[i].callback(_onStarting[i].state);
			}

			_response.StatusCode = StatusCode;
			if (!string.IsNullOrEmpty(ReasonPhrase))
			{
				_response.StatusDescription = ReasonPhrase;
			}

			foreach (var (name, values) in Headers)
			{
				if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
				{
					_response.ContentType = values.ToString();
				}
				else if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
				{
					_response.ContentLength64 = long.Parse(values.ToString());
				}
				else if (!string.Equals(name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
				{
					foreach (var value in values)
					{
						_response.AppendHeader(name, value);
					}
				}
			}

			if (!Headers.ContainsKey("Content-Length"))
			{
				_response.SendChunked = true;
			}
		}

		public async Task CompleteAsync()
		{
			await StartAsync();
			if (_writer != null)
			{
				await _writer.FlushAsync();
			}
			await _response.OutputStream.FlushAsync();
		}

		public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default) =>
			throw new NotSupportedException();

		public async Task FireOnCompletedAsync()
		{
			foreach (var (callback, state) in _onCompleted)
			{
				try
				{
					await callback(state);
				}
				catch (Exception ex)
				{
					Beamable.Server.Log.Debug($"OnCompleted callback failed: {ex.Message}");
				}
			}
		}

		private sealed class BodyStream : Stream
		{
			private readonly ListenerResponseFeature _owner;

			public BodyStream(ListenerResponseFeature owner) => _owner = owner;

			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => throw new NotSupportedException();
			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			public override void Flush() => FlushAsync().GetAwaiter().GetResult();

			public override async Task FlushAsync(CancellationToken cancellationToken)
			{
				await _owner.StartAsync(cancellationToken);
				await Guard(() => _owner._response.OutputStream.FlushAsync(cancellationToken));
			}

			public override void Write(byte[] buffer, int offset, int count) =>
				WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
				WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

			public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
			{
				await _owner.StartAsync(cancellationToken);
				await Guard(() => _owner._response.OutputStream.WriteAsync(buffer, cancellationToken).AsTask());
			}

			/// <summary>A failed write means the client disconnected, which is how a handler learns to stop.</summary>
			private async Task Guard(Func<Task> write)
			{
				try
				{
					await write();
				}
				catch (Exception ex) when (ex is HttpListenerException or IOException or ObjectDisposedException)
				{
					_owner.Abort();
					throw new OperationCanceledException("The client disconnected.", ex, _owner.RequestAborted);
				}
			}

			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
		}
	}
}
