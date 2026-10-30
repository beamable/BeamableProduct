using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.WebSockets;
using Beamable.Common.Dependencies;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Beamable.Server
{
	public class WebsocketRequest
	{
		public long id;
		public string method;
		public string path;
		public object body;
		public long? from;
		public string[] scopes;
		public Dictionary<string, string> headers;
	}

	public class WebsocketReply
	{
		public long id;
		public long status;
		public object body;
	}

	public interface IPlatformSubscription
	{
		Task Resolve(IUserScope scope);
	}
	public class PlatformSubscription<T> : IPlatformSubscription
	{
		public string EventName;
		public Action Unsubscribe;
		public Func<IUserScope, T, Task> OnEvent;

		public async Task Resolve(IUserScope scope)
		{
			var ctx = scope.Context;
			var data = JsonConvert.DeserializeObject<T>(ctx.Body, UnitySerializationSettings.Instance);
			if (OnEvent == null) return;
			await OnEvent.Invoke(scope, data);
		}
	}


	public class WebsocketRequesterException : RequesterException
	{
		public WebsocketRequesterException(string method, string uri, long responseCode, string responsePayload) : base(Constants.Requester.ERROR_PREFIX_WEBSOCKET_RES, method,
			uri, responseCode, responsePayload)
		{

		}
	}

	public class UnauthenticatedException : WebsocketRequesterException
	{
		public WebsocketErrorResponse Error { get; }

		public UnauthenticatedException(WebsocketErrorResponse error, string method, string uri, long responseCode, string responsePayload) : base(method, uri, responseCode, responsePayload)
		{
			Error = error;
		}
	}

	/// <summary>
	/// An outbound request got no reply before its deadline. The status is 504, like a gateway timeout.
	/// </summary>
	public class RequestTimeoutException : WebsocketRequesterException
	{
		public long RequestId { get; }
		public TimeSpan Elapsed { get; }

		public RequestTimeoutException(string method, string uri, long requestId, TimeSpan elapsed)
			: base(method, uri, 504, JsonConvert.SerializeObject(new
			{
				status = 504,
				service = "beam-microservice",
				error = "RequestTimeout",
				message = $"No reply after {elapsed.TotalSeconds:0.0}s. request id=[{requestId}]"
			}))
		{
			RequestId = requestId;
			Elapsed = elapsed;
		}
	}

	public interface IWebsocketResponseListener
	{
		long Id { get; set; }
		string Path { get; set; }
		string Method { get; set; }
		BeamActivity Activity { get; set; }
		void Resolve(RequestContext ctx);

		/// <summary>Ends the request with an error. Only the path that claimed the listener may call this.</summary>
		void Fail(Exception ex);
		/// <summary><see cref="Stopwatch.GetTimestamp"/> when the request was registered.</summary>
		long StartedAt { get; set; }
		/// <summary><see cref="Stopwatch.GetTimestamp"/> after which the request is overdue, or null for no deadline.</summary>
		long? Deadline { get; set; }
		/// <summary>When false, passing the deadline only logs; the caller keeps waiting.</summary>
		bool DeadlineEnforced { get; set; }
		/// <summary>The connection the request was written to; null until the send finished.</summary>
		IConnection SentOn { get; set; }
		/// <summary><see cref="Stopwatch.GetTimestamp"/> when the send finished.</summary>
		long SentAt { get; set; }
		/// <summary>Whether an overdue, unenforced request was already reported.</summary>
		bool OverdueReported { get; set; }
	}
	public class WebsocketResponseListener<T> : IWebsocketResponseListener
	{
		public Promise<T> OnDone;

		public long Id { get; set; }
		public string Path { get; set; }
		public string Uri { get; set; }
		public string Method { get; set; }
		public BeamActivity Activity { get; set; }
		public Func<string, T> Parser { get; set; }
		public long StartedAt { get; set; }
		public long? Deadline { get; set; }
		public bool DeadlineEnforced { get; set; }
		public IConnection SentOn { get; set; }
		public long SentAt { get; set; }
		public bool OverdueReported { get; set; }

		public void Fail(Exception ex) => OnDone.CompleteError(ex);

		public void Resolve(RequestContext ctx)
		{
			if (ctx.Status == 0)
			{
				OnDone.CompleteError(new WebsocketRequesterException(Method, Uri, ctx.Status, "noconnection"));
			}
			else if (ctx.Status == 403)
			{
				var error = JsonConvert.DeserializeObject<WebsocketErrorResponse>(ctx.Body, UnitySerializationSettings.Instance);
				OnDone.CompleteError(new UnauthenticatedException(error, Method, Uri, ctx.Status, ctx.Body));
			}
			else if (ctx.Status != 200)
			{
				OnDone.CompleteError(new WebsocketRequesterException(Method, Uri, ctx.Status, ctx.Body));
			}
			else
			{
				// parse out the data from ctx.

				if (Parser == null)
				{
					T DefaultParser(string json)
					{
						return JsonConvert.DeserializeObject<T>(json, UnitySerializationSettings.Instance);
					}
					Parser = DefaultParser;
				}

				try
				{
					var result = Parser(ctx.Body);
					ctx.ActivityContext?.StopAndDispose(ActivityStatusCode.Ok);
					OnDone.CompleteSuccess(result);

				}
				catch (Exception ex)
				{
					ctx.ActivityContext?.StopAndDispose(ex);
					OnDone.CompleteError(ex);
				}
			}
		}
	}

	/// <summary>
	/// Deadline and connection health settings for one <see cref="SocketRequesterContext"/>.
	/// </summary>
	public class SocketReliabilityOptions
	{
		/// <summary>How long a sent request may go unanswered, with no other reply arriving, before the connection is judged broken. Zero turns the check off.</summary>
		public TimeSpan StallTimeout { get; set; }

		/// <summary>When false, a connection judged broken is only reported, not replaced.</summary>
		public bool Enforce { get; set; }

		public static SocketReliabilityOptions From(IMicroserviceArgs args) => new SocketReliabilityOptions
		{
			StallTimeout = TimeSpan.FromSeconds(Math.Max(0, args.SocketStallTimeoutSeconds)),
			Enforce = args.EnforceSocketReliability
		};
	}

	public class SocketRequesterContext
	{
		private const int EndedRequestMemory = 4096;
		private const int SeenEventMemory = 1024;
		private static readonly TimeSpan SweepInterval = TimeSpan.FromMilliseconds(250);

		public MicroserviceAuthenticationDaemon Daemon { get; set; }
		private readonly Func<Promise<IConnection>> _socketGetter;
		public Promise<IConnection> Socket => _socketGetter();

		public IActivityProvider ActivityProvider { get; set; }

		/// <summary>A name for logs and metrics, such as the connection id.</summary>
		public string Name { get; set; }

		/// <summary>Deadline and connection health settings. By default stall detection is off and nothing is enforced.</summary>
		public SocketReliabilityOptions Reliability { get; set; } = new SocketReliabilityOptions();

		/// <summary>
		/// Raised, in enforce mode, when a health check judges this context's connection broken. The owner should replace
		/// the connection. Raised at most once per connection, off the calling thread.
		/// </summary>
		public event Action<IConnection, string> ConnectionUnhealthy;

		private ConcurrentDictionary<long, IWebsocketResponseListener> _pendingMessages = new ConcurrentDictionary<long, IWebsocketResponseListener>();
		private ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>> _subscriptions = new ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>>();
		private long _lastRequestId = 0;

		// Requests that ended without a reply (deadline, failed send, lost connection), so a reply that still arrives is
		// recognized as late instead of unknown. Bounded: the oldest ids are forgotten first.
		private readonly ConcurrentDictionary<long, byte> _endedWithoutReply = new ConcurrentDictionary<long, byte>();
		private readonly ConcurrentQueue<long> _endedWithoutReplyOrder = new ConcurrentQueue<long>();

		// Event message ids already dispatched, so an event delivered on more than one connection runs once.
		private readonly ConcurrentDictionary<string, byte> _seenEvents = new ConcurrentDictionary<string, byte>();
		private readonly ConcurrentQueue<string> _seenEventsOrder = new ConcurrentQueue<string>();

		private int _sweeperRunning;
		private long _lastReplyAt;
		private IConnection _reportedConnection;

		// The oldest request that ended without a reply since the last reply arrived, for stall detection.
		private readonly object _stallLock = new object();
		private long _unansweredSince;
		private IConnection _unansweredOn;

		// default is false, set 1 for true.
		private int _hasSocketClosedFlag = 1; // https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool
		public bool HasSocketClosed
		{
			get => (Interlocked.CompareExchange(ref _hasSocketClosedFlag, 1, 1) == 1);
			private set
			{
				if (value) Interlocked.CompareExchange(ref _hasSocketClosedFlag, 1, 0);
				else Interlocked.CompareExchange(ref _hasSocketClosedFlag, 0, 1);
			}
		}

		public SocketRequesterContext(Func<Promise<IConnection>> socketGetter)
		{
			_socketGetter = () =>
			{
				if (!HasSocketClosed)
				{
					throw new Exception("socket has closed");
				}
				return socketGetter();
			};
			SocketReliabilityMetrics.Track(this);
		}

		/// <summary>The number of outbound requests waiting for a reply.</summary>
		public int PendingRequestCount => _pendingMessages.Count;

		/// <summary>How long the oldest outbound request has been waiting for its reply.</summary>
		public TimeSpan OldestPendingRequestAge
		{
			get
			{
				long oldest = 0;
				foreach (var pair in _pendingMessages)
				{
					if (oldest == 0 || pair.Value.StartedAt < oldest) oldest = pair.Value.StartedAt;
				}

				return oldest == 0 ? TimeSpan.Zero : Elapsed(oldest, Stopwatch.GetTimestamp());
			}
		}

		/// <summary>
		/// Whether this context's connection looks usable: connected, open, authenticated, and not judged broken.
		/// </summary>
		public bool IsHealthy
		{
			get
			{
				var connection = CurrentConnection;
				return connection != null
				       && connection.State == WebSocketState.Open
				       && !ReferenceEquals(Volatile.Read(ref _reportedConnection), connection)
				       && (Daemon?.NoPendingOrInProgressAuth ?? false);
			}
		}

		/// <summary>The connection in use now, or null while there is none (connecting, reconnecting, or closed).</summary>
		public IConnection CurrentConnection
		{
			get
			{
				if (!HasSocketClosed) return null;
				try
				{
					var promise = _socketGetter();
					return promise != null && promise.IsCompleted && !promise.IsFailed ? promise.GetResult() : null;
				}
				catch
				{
					return null;
				}
			}
		}


		public async Task WaitForAuthorization(TimeSpan timeout=default, string message=null)
		{
			var startTime = DateTime.UtcNow;
			if (timeout <= default(TimeSpan))
			{
				timeout = TimeSpan.FromSeconds(10);
			}

			if (Daemon.AuthorizationCounter <= 0)
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogTrace($"Waiting for authorization, but auth is already done. message=[{message}]");
				return;
			}

			var enteringCount = Daemon.AuthorizationCounter;
			while (Daemon.AuthorizationCounter > 0)
			{
				var totalWaitedTime = DateTime.UtcNow - startTime;
				if (totalWaitedTime > timeout)
				{
					var exitCount = Daemon.AuthorizationCounter;
					throw new TimeoutException($"waited for authorization for too long. enter-count=[{enteringCount}] exit-count=[{exitCount}] Waited for [{totalWaitedTime}] started=[{startTime}] message=[{message}]");
				}
				await Task.Delay(100);
			}
			BeamableZLoggerProvider.LogContext.Value.ZLogTrace($"Leaving wait for send. message=[{message}]");
		}

		public async Promise SendMessageSafely(string message, bool awaitAuthorization=true, int retryCount=10, Stopwatch sw=null, bool disableCompression=false)
		{
			await SendWithRetries(message, awaitAuthorization, retryCount, sw, disableCompression, null, null);
		}

		/// <summary>
		/// Sends a request whose listener is already registered. It stops retrying once the request has ended (for
		/// example, at its deadline), records the connection that carried it, and ends the request if every attempt fails.
		/// </summary>
		internal Promise<IConnection> SendRequest(long requestId, string message, bool awaitAuthorization, bool disableCompression)
		{
			return Send().ToPromise();

			async Task<IConnection> Send()
			{
				try
				{
					return await SendWithRetries(message, awaitAuthorization, 10, null, disableCompression,
						() => _pendingMessages.ContainsKey(requestId),
						connection =>
						{
							if (_pendingMessages.TryGetValue(requestId, out var listener))
							{
								listener.SentAt = Stopwatch.GetTimestamp();
								listener.SentOn = connection;
							}
						});
				}
				catch (RequestEndedException)
				{
					throw;
				}
				catch (Exception ex)
				{
					FailIfPending(requestId, ex);
					throw;
				}
			}
		}

		/// <summary>
		/// Sends a reply to a request that arrived on <paramref name="origin"/>. A reply only goes back on that connection:
		/// the gateway matches it to the session that sent the request, and the same id on another session belongs to a
		/// different request. A reply doesn't wait for re-authentication, because the gateway doesn't check auth for
		/// replies.
		/// </summary>
		public async Promise SendReply(IConnection origin, string message, Stopwatch sw = null, bool disableCompression = false)
		{
			if (origin == null)
			{
				await SendMessageSafely(message, awaitAuthorization: false, sw: sw, disableCompression: disableCompression);
				return;
			}

			var failures = new List<Exception>();
			for (var attempt = 0; attempt < 3; attempt++)
			{
				try
				{
					await WriteTo(origin, message, sw, disableCompression);
					return;
				}
				catch (Exception ex)
				{
					failures.Add(ex);
					if (origin.State != WebSocketState.Open) break;
					await Task.Delay(250);
				}
			}

			// The request was lost with its connection; the caller sees the gateway's timeout. This follows a disconnect.
			var finalEx = new SocketClosedException(failures);
			BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Dropped a reply because the connection its request arrived on closed. first-failure=[{failures[0].Message}]");
			throw finalEx;
		}

		private async Task<IConnection> SendWithRetries(string message, bool awaitAuthorization, int retryCount, Stopwatch sw,
			bool disableCompression, Func<bool> stillWanted, Action<IConnection> onWriting)
		{
			var failures = new List<Exception>();
			for (var retry = 0; retry < retryCount; retry++)
			{
				if (stillWanted != null && !stillWanted()) throw new RequestEndedException();
				try
				{
					var connection = await Socket;
					if (awaitAuthorization)
					{
						await WaitForAuthorization( message: message);
					}

					// the request may have ended while this waited for the socket or for authorization.
					if (stillWanted != null && !stillWanted()) throw new RequestEndedException();
					onWriting?.Invoke(connection);
					await WriteTo(connection, message, sw, disableCompression);
					return connection;
				}
				catch (RequestEndedException)
				{
					throw;
				}
				catch (Exception ex)
				{
					failures.Add(ex);
					await Task.Delay(
						250); // wait awhile before trying again; so that its likely authorization has finished.
				}
			}
			// all attempts have failed : (
			var finalEx = new SocketClosedException(failures);
			BeamableZLoggerProvider.LogContext.Value.ZLogError($"Exception {finalEx.GetType().Name}: {finalEx.Message} - {finalEx.Source} \n {finalEx.StackTrace}");

			for (var i = 0 ; i < failures.Count; i ++)
			{
				var failure = failures[i];
				BeamableZLoggerProvider.LogContext.Value.ZLogError($"  Failure {i} {finalEx.GetType().Name}: {failure.Message} - {failure.Source} \n {failure.StackTrace}");
			}

			throw finalEx;
		}

		private async Task WriteTo(IConnection connection, string message, Stopwatch sw, bool disableCompression)
		{
			// authorization needs to be complete if this is any message _other_ than auth related
			// Use compression if negotiated and message is large enough
			var codec = Daemon?.NegotiatedCodec;
			if (codec != null && !disableCompression && SocketCompression.ShouldCompress(message))
			{
				var compressed = SocketCompression.Compress(message, codec);
				await connection.SendBinaryMessage(compressed, sw);
			}
			else
			{
				await connection.SendMessage(message, sw);
			}
		}

		/// <summary>Thrown inside the send path when the request it was sending has already ended.</summary>
		private class RequestEndedException : Exception
		{
			public RequestEndedException() : base("The request ended before it was sent.") { }
		}

		public void HandleCloseConnection()
		{
			HasSocketClosed = false;

		}

		private long GetNextRequestId()
		{
			lock (this)
			{
				return Interlocked.Decrement(ref _lastRequestId);
			}
		}

		public PlatformSubscription<T> Subscribe<T>(string eventName, Action<T> callback)
		{
			return Subscribe<T>(eventName, (_, data) =>
			{
				callback?.Invoke(data);
				return Task.CompletedTask;
			});
		}

		public PlatformSubscription<T> Subscribe<T>(string eventName, Func<T, Task> callback)
		{
			return Subscribe<T>(eventName, async (_, data) =>
			{
				var res = callback?.Invoke(data);
				if (res != null)
				{
					await res;
				}
			});
		}

		public PlatformSubscription<T> Subscribe<T>(string eventName, Action<IUserScope, T> callback)
		{
			return Subscribe<T>(eventName, (scope, data) =>
			{
				callback?.Invoke(scope, data);
				return Task.CompletedTask;
			});
		}

		public PlatformSubscription<T> Subscribe<T>(string eventName, Func<IUserScope, T, Task> callback)
		{
			var subscription = new PlatformSubscription<T>
			{
				EventName = eventName,
				OnEvent = callback
			};

			_subscriptions.TryAdd(eventName, new SynchronizedCollection<IPlatformSubscription>());

			var subscriptionList = _subscriptions[eventName];
			subscriptionList.Add(subscription);
			var unsub = new Action(() =>
			{
				subscriptionList.Remove(subscription);
			});
			subscription.Unsubscribe = unsub;

			return subscription;
		}

		private bool TryGetEventSubscriptions(string eventName, out SynchronizedCollection<IPlatformSubscription> subscriptions)
		{
			return _subscriptions.TryGetValue(eventName, out subscriptions);
		}

		public bool IsPlatformMessage(RequestContext ctx)
		{
			return string.IsNullOrEmpty(ctx.Path) || ctx.Path.StartsWith("event/");
		}

		public async Task HandleMessage(IDependencyProvider provider, MicroserviceRequestContext ctx, BeamActivity parentActivity=null)
		{
			if (parentActivity == null)
			{
				parentActivity = BeamActivity.Noop;
			}

			if (ctx.IsEvent)
			{
				var eventName = ctx.Path.Substring("event/".Length);
				parentActivity.SetDisplay($"On{eventName}");

				if (IsRepeatedEvent(ctx))
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Skipping event {eventName} id=[{ctx.Id}]; it was already handled.");
					return;
				}

				if (TryGetEventSubscriptions(eventName, out var subscriptions))
				{
					var fork =
						provider == null
							? new DependencyBuilder()
								.AddSingleton<RequestContext>(ctx)
								.AddSingleton<IBeamableRequester>(p => null)
								.AddSingleton<IBeamableServices>(p => null)
								.Build()
							: provider.Fork(b =>
							{
								b.RemoveIfExists<RequestContext>();
								b.RemoveIfExists<MicroserviceRequestContext>();
								b.AddScoped<RequestContext>(ctx);
								b.AddScoped<MicroserviceRequestContext>(ctx);
							});

					await using IUserScope scope = new UserRequestDataHandler(fork);
					var startCount = subscriptions.Count; // take the count at the moment the event is processed. If something else subscribes at the same frame, they're too late.
					for (var i = 0; i < startCount; i++) // TODO: there is still a bug with multi-threaded access; if an item is removed/ unsub
					{
						await subscriptions[i].Resolve(scope);
					}
				}

				//</color> <color=blue>BaseGet [{"id":1,"method":"post","path":"event/content.manifest","body":{"categories":["tournaments","announcements","listings","items","stores","sagamap","skus","currency","leaderboards","emails","game_types"]}}

			} else if (TryClaim(ctx.Id, out var listener))
			{
				// this is a response to some pending request...
				NoteReply();
				parentActivity.SetDisplay($"Response ({ctx.Id})");
				listener.Resolve(ctx);
			}
			else
			{
				NoteReply(); // a late reply still shows the connection carries traffic both ways.
				if (_endedWithoutReply.TryRemove(ctx.Id, out _))
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Ignoring a late reply to request {ctx.Id}, which already ended.");
				}
				else
				{
					BeamableLogger.LogError("There was no listener for request {id}", ctx.Id);
				}
			}
		}

		/// <summary>
		/// True when an event with the same gateway message id was already handled by this context. Every connection
		/// receives realm events, so the same event can reach this context more than once during a failover.
		/// </summary>
		private bool IsRepeatedEvent(RequestContext ctx)
		{
			if (ctx.Headers == null || !ctx.Headers.TryGetValue("msgId", out var messageId) || string.IsNullOrEmpty(messageId))
			{
				return false;
			}

			if (!_seenEvents.TryAdd(messageId, 0)) return true;
			_seenEventsOrder.Enqueue(messageId);
			while (_seenEventsOrder.Count > SeenEventMemory && _seenEventsOrder.TryDequeue(out var oldest))
			{
				_seenEvents.TryRemove(oldest, out _);
			}

			return false;
		}


		public Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser, BeamActivity parentActivity)
		{
			return AddListener(req, uri, parser, parentActivity, null, false);
		}

		/// <summary>
		/// Registers a listener for the reply to <paramref name="req"/> and assigns its id.
		/// </summary>
		/// <param name="deadline"><see cref="Stopwatch.GetTimestamp"/> after which the request is overdue, or null for none.</param>
		/// <param name="enforceDeadline">When true, an overdue request fails with <see cref="RequestTimeoutException"/>; otherwise it is only reported.</param>
		public Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser, BeamActivity parentActivity, long? deadline, bool enforceDeadline)
		{
			var requestId = GetNextRequestId();
			if (_pendingMessages.ContainsKey(requestId))
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"The request {requestId} was already taken");
				return AddListener(req, uri, parser, parentActivity, deadline, enforceDeadline); // try again.
			}

			var promise = new Promise<T>();

			req.id = requestId;
			var listener = new WebsocketResponseListener<T>
			{
				Id = requestId,
				OnDone = promise,
				Parser = parser,
				Uri = uri,
				Method = req.method,
				Path = req.path,
				Activity = parentActivity,
				StartedAt = Stopwatch.GetTimestamp(),
				Deadline = deadline,
				DeadlineEnforced = enforceDeadline
			};

			if (!_pendingMessages.TryAdd(requestId, listener))
			{
				promise.CompleteError(new Exception("request Id has already been taken in socket context. id=" +requestId));
			}
			else if (deadline.HasValue || Reliability.StallTimeout > TimeSpan.Zero)
			{
				EnsureSweeper();
			}
			return promise;
		}

		public bool TryGetListener(long id, out IWebsocketResponseListener listener)
		{
			return _pendingMessages.TryGetValue(id, out listener);
		}

		/// <summary>
		/// Takes ownership of a pending request so it can be ended. Every path that ends a request (reply, deadline, failed
		/// send, lost connection, shutdown) claims it first; only the one that wins may complete it.
		/// </summary>
		public bool TryClaim(long id, out IWebsocketResponseListener listener)
		{
			return _pendingMessages.TryRemove(id, out listener);
		}

		public void Remove(long id)
		{
			if (!_pendingMessages.TryRemove(id, out _))
			{
				throw new Exception("request Id could not be removed from the socket context. id=" +id);
			}
		}

		/// <summary>Ends a request with <paramref name="ex"/> if nothing else has ended it yet.</summary>
		public bool FailIfPending(long id, Exception ex)
		{
			if (!TryClaim(id, out var listener)) return false;
			EndWithoutReply(listener, ex);
			return true;
		}

		/// <summary>
		/// Ends every request that was sent on <paramref name="connection"/>; the gateway can't answer them on any other
		/// connection. Requests not sent yet are left to go out on the replacement.
		/// </summary>
		public int FailPendingFor(IConnection connection, Exception ex)
		{
			var failed = 0;
			foreach (var pair in _pendingMessages)
			{
				if (!ReferenceEquals(pair.Value.SentOn, connection)) continue;
				if (!TryClaim(pair.Key, out var listener)) continue;
				failed++;
				EndWithoutReply(listener, ex);
			}

			lock (_stallLock)
			{
				if (ReferenceEquals(_unansweredOn, connection))
				{
					_unansweredSince = 0;
					_unansweredOn = null;
				}
			}

			if (failed > 0)
			{
				SocketReliabilityMetrics.ConnectionLost.Add(failed);
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Failed {failed} request(s) that were sent on connection {Name} before it closed.");
			}
			return failed;
		}

		/// <summary>Ends every pending request, for shutdown.</summary>
		public int FailAllPending(Exception ex)
		{
			var failed = 0;
			foreach (var pair in _pendingMessages)
			{
				if (!TryClaim(pair.Key, out var listener)) continue;
				failed++;
				EndWithoutReply(listener, ex);
			}
			return failed;
		}

		private void EndWithoutReply(IWebsocketResponseListener listener, Exception ex)
		{
			_endedWithoutReply.TryAdd(listener.Id, 0);
			_endedWithoutReplyOrder.Enqueue(listener.Id);
			while (_endedWithoutReplyOrder.Count > EndedRequestMemory && _endedWithoutReplyOrder.TryDequeue(out var oldest))
			{
				_endedWithoutReply.TryRemove(oldest, out _);
			}

			if (listener.SentOn != null)
			{
				lock (_stallLock)
				{
					if (_unansweredSince == 0 || listener.SentAt < _unansweredSince)
					{
						_unansweredSince = listener.SentAt;
						_unansweredOn = listener.SentOn;
					}
				}
			}

			listener.Fail(ex);
		}

		private void NoteReply()
		{
			Volatile.Write(ref _lastReplyAt, Stopwatch.GetTimestamp());
			lock (_stallLock)
			{
				_unansweredSince = 0;
				_unansweredOn = null;
			}
		}

		/// <summary>
		/// Reports that this context's connection looks broken. In enforce mode the owner is asked to replace it through
		/// <see cref="ConnectionUnhealthy"/>; otherwise it is only logged. Each connection is reported once.
		/// </summary>
		/// <param name="connection">The broken connection, or null for whichever connection is current.</param>
		public void ReportUnhealthyConnection(IConnection connection, string reason)
		{
			connection ??= CurrentConnection;
			if (connection == null) return;
			if (ReferenceEquals(Interlocked.Exchange(ref _reportedConnection, connection), connection)) return;

			var enforce = Reliability.Enforce;
			SocketReliabilityMetrics.UnhealthyConnections.Add(1,
				new KeyValuePair<string, object>("reason", ReasonTag(reason)),
				new KeyValuePair<string, object>("enforced", enforce));
			if (!enforce)
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Connection {Name} looks broken: {reason}. Not replacing it, because BEAM_SOCKET_RELIABILITY_ENFORCE is off.");
				return;
			}

			BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Connection {Name} looks broken: {reason}. Replacing it.");
			var handler = ConnectionUnhealthy;
			if (handler != null)
			{
				_ = Task.Run(() => handler(connection, reason));
			}
		}

		private static string ReasonTag(string reason)
		{
			if (reason.StartsWith("the gateway rejected")) return "rejected_after_reauth";
			if (reason.StartsWith("no reply")) return "reply_stall";
			return "other";
		}

		private void EnsureSweeper()
		{
			if (Interlocked.CompareExchange(ref _sweeperRunning, 1, 0) != 0) return;
			_ = Task.Run(SweepLoop);
		}

		private async Task SweepLoop()
		{
			while (true)
			{
				await Task.Delay(SweepInterval);
				try
				{
					Sweep(Stopwatch.GetTimestamp());
				}
				catch (Exception ex)
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogError($"Checking request deadlines failed. {ex.GetType().Name}: {ex.Message}");
				}

				if (!_pendingMessages.IsEmpty) continue;
				Interlocked.Exchange(ref _sweeperRunning, 0);
				// A request registered after the check above may have found the loop still running and not started one.
				if (_pendingMessages.IsEmpty || Interlocked.CompareExchange(ref _sweeperRunning, 1, 0) != 0) return;
			}
		}

		/// <summary>Ends or reports overdue requests, and checks whether the connection has stopped answering.</summary>
		internal void Sweep(long now)
		{
			IWebsocketResponseListener oldestSent = null;
			foreach (var pair in _pendingMessages)
			{
				var listener = pair.Value;
				if (listener.Deadline.HasValue && now > listener.Deadline.Value)
				{
					var elapsed = Elapsed(listener.StartedAt, now);
					if (listener.DeadlineEnforced)
					{
						if (!TryClaim(listener.Id, out _)) continue;
						SocketReliabilityMetrics.DeadlineExceeded.Add(1,
							new KeyValuePair<string, object>("enforced", true),
							new KeyValuePair<string, object>("path", listener.Path));
						BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Request {listener.Method} {listener.Path} id=[{listener.Id}] exceeded its deadline after {elapsed.TotalSeconds:0.0}s with no reply; failing it.");
						EndWithoutReply(listener, new RequestTimeoutException(listener.Method, listener.Path, listener.Id, elapsed));
						continue;
					}

					if (!listener.OverdueReported)
					{
						listener.OverdueReported = true;
						SocketReliabilityMetrics.DeadlineExceeded.Add(1,
							new KeyValuePair<string, object>("enforced", false),
							new KeyValuePair<string, object>("path", listener.Path));
						BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Request {listener.Method} {listener.Path} id=[{listener.Id}] exceeded its deadline after {elapsed.TotalSeconds:0.0}s with no reply. Still waiting, because BEAM_SOCKET_RELIABILITY_ENFORCE is off.");
					}
				}

				if (listener.SentOn != null && (oldestSent == null || listener.SentAt < oldestSent.SentAt))
				{
					oldestSent = listener;
				}
			}

			CheckForStall(oldestSent, now);
		}

		private void CheckForStall(IWebsocketResponseListener oldestSent, long now)
		{
			var limit = Reliability.StallTimeout;
			if (limit <= TimeSpan.Zero) return;

			long since = 0;
			IConnection on = null;
			lock (_stallLock)
			{
				since = _unansweredSince;
				on = _unansweredOn;
			}

			if (oldestSent != null && oldestSent.SentAt > Volatile.Read(ref _lastReplyAt) && (since == 0 || oldestSent.SentAt < since))
			{
				since = oldestSent.SentAt;
				on = oldestSent.SentOn;
			}

			if (since == 0 || on == null) return;
			var silent = Elapsed(since, now);
			if (silent < limit) return;
			if (!ReferenceEquals(on, CurrentConnection)) return; // it was already replaced.

			ReportUnhealthyConnection(on, $"no reply for {silent.TotalSeconds:0}s to requests sent on it, and no other reply since");
		}

		private static TimeSpan Elapsed(long from, long to) =>
			TimeSpan.FromSeconds((to - from) / (double)Stopwatch.Frequency);


		public HashSet<string> EventNames
		{
			get
			{
				// TODO: handle locking?
				return new HashSet<string>(_subscriptions.Keys);
			}
		}
	}

	public class MicroserviceRequester : IRequester
	{
		private readonly IMicroserviceArgs _env;
		protected readonly RequestContext _requestContext;

		private readonly SocketRequesterContext _fixedSocketContext;
		private readonly Func<SocketRequesterContext> _socketContextSelector;
		private readonly IActivityProvider _activityProvider;
		private readonly bool _waitForAuthorization;

		/// <summary>The deadline for gateway nonce and auth requests, which is enforced even in shadow mode.</summary>
		private static readonly TimeSpan AuthRequestTimeout = TimeSpan.FromSeconds(15);

		private static readonly AsyncLocal<TimeSpan?> ReplyTimeoutOverride = new AsyncLocal<TimeSpan?>();

		// TODO what does concurrency look like?
		public string Cid => _requestContext.Cid;
		public string Pid => _requestContext.Pid;

		// [Obsolete]
		public MicroserviceRequester(
			IMicroserviceArgs env,
			RequestContext requestContext,
			SocketRequesterContext socketContext,
			bool waitForAuthorization,
			IActivityProvider activityProvider)
		{
			_env = env;
			_requestContext = requestContext;
			_fixedSocketContext = socketContext;
			_waitForAuthorization = waitForAuthorization;
			_activityProvider = activityProvider;
		}

		/// <summary>
		/// A requester that picks a connection for each request, such as the first healthy one of a service's
		/// instances, instead of always using one.
		/// </summary>
		public MicroserviceRequester(
			IMicroserviceArgs env,
			RequestContext requestContext,
			Func<SocketRequesterContext> socketContextSelector,
			bool waitForAuthorization,
			IActivityProvider activityProvider)
			: this(env, requestContext, (SocketRequesterContext)null, waitForAuthorization, activityProvider)
		{
			_socketContextSelector = socketContextSelector;
		}

		private SocketRequesterContext CurrentSocketContext => _socketContextSelector?.Invoke() ?? _fixedSocketContext;

		/// <summary>
		/// Uses <paramref name="timeout"/> as the reply deadline for outbound requests made inside the returned scope, for
		/// calls known to take longer than <c>BEAM_OUTBOUND_REQUEST_TIMEOUT_SECONDS</c>. <see cref="TimeSpan.Zero"/> means
		/// no deadline. Dispose the result to end the scope.
		/// </summary>
		public static IDisposable OverrideReplyTimeout(TimeSpan timeout)
		{
			var previous = ReplyTimeoutOverride.Value;
			ReplyTimeoutOverride.Value = timeout;
			return new ReplyTimeoutScope(previous);
		}

		private sealed class ReplyTimeoutScope : IDisposable
		{
			private readonly TimeSpan? _previous;
			public ReplyTimeoutScope(TimeSpan? previous) => _previous = previous;
			public void Dispose() => ReplyTimeoutOverride.Value = _previous;
		}

		public IAccessToken AccessToken { get; }

		/// <summary>
		/// When set (e.g. <c>"cid.pid"</c>), every outgoing request stamps this value as the
		/// <see cref="Constants.Requester.HEADER_SCOPE"/> (<c>X-BEAM-SCOPE</c>) header, overriding the scope the
		/// socket authenticated with. A zone service's socket authenticates as <c>cid.zid</c>; an
		/// <c>AssumeRealm</c> fork sets this to the realm's <c>cid.pid</c> so the gateway routes the request to
		/// the realm instead of the zone (requires the backend to honor the per-request scope header). Left null
		/// for ordinary requests, which inherit the socket's authenticated scope.
		/// </summary>
		public string ScopeOverride { get; set; }

		public Task WaitForAuthorization(TimeSpan timeout = default, string message=null) => CurrentSocketContext.WaitForAuthorization(timeout, message);

		/// <summary>
		/// Acknowledge a message from the websocket.
		/// </summary>
		/// <param name="ctx">The request you wish to ack</param>
		/// <param name="error">an error, or null for a 200 ack.</param>
		/// <returns></returns>
		public Promise<Unit> Acknowledge(RequestContext ctx, WebsocketErrorResponse error=null)
		{
			// only ack if the original request context was spawned from an event
			if (!ctx.IsEvent)
			{
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			var req = new WebsocketReply
			{
				id = ctx.Id,
				status = error?.status ?? 200,
				body = error
			};
			var dict = new ArrayDict
			{
				[nameof(req.id)] = req.id,
				[nameof(req.status)] = req.status,
				[nameof(req.body)] = new RawJsonProvider {Json = JsonConvert.SerializeObject(error, UnitySerializationSettings.Instance)}
			};
			var msg = "";
			using (var stringBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				msg = Json.Serialize(dict, stringBuilder.Builder);
			}

			// An ack is a reply: the gateway doesn't check the session's auth for replies, so it needn't wait for re-auth.
			return CurrentSocketContext.SendMessageSafely(msg, awaitAuthorization: false, disableCompression: _env.DisableOutboundWsCompression);
		}

		public static AsyncLocal<BeamActivity> ContextActivity { get; set; } = new AsyncLocal<BeamActivity>();
		public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
			Func<string, T> parser = null, bool useCache = false)
		{
			return BeamableRequest(new SDKRequesterOptions<T>
			{
				method = method,
				uri = uri,
				body = body,
				includeAuthHeader = includeAuthHeader,
				parser = parser,
				useCache = useCache
			});
		}

		public static readonly string[] DefaultEventNames = new string[]
		{
			Constants.Features.Services.CONTENT_UPDATE_EVENT,
			Constants.Features.Services.REALM_CONFIG_UPDATE_EVENT,
			Constants.Features.Services.LOGGING_CONTEXT_UPDATE_EVENT
		};
		/// <summary>
		/// Each socket only needs to set up one subscription to the server.
		/// All events will get piped to the client.
		/// It's the client job to filter the events, and decide what is valuable.
		/// </summary>
		/// <returns></returns>
		public Promise<EmptyResponse> InitializeSubscription()
		{
			return InitializeSubscription(DefaultEventNames, Array.Empty<string>());
		}

		public Promise<EmptyResponse> InitializeSubscription(IEnumerable<string> eventNames, IEnumerable<string> uniqueBindings)
		{
			var req = new MicroserviceEventProviderRequest
			{
				type = "event",
				evtWhitelist = eventNames?.Distinct().ToArray() ?? Array.Empty<string>(),
				evtUniqueBindings = uniqueBindings?.Distinct().ToArray() ?? Array.Empty<string>()
			};
			var promise = Request<MicroserviceProviderResponse>(Method.POST, "gateway/provider", req);
			return promise.Map(_ => new EmptyResponse());
		}

		public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
		{
			throw new NotImplementedException();
		}

		public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> beamReq)
		{
			return BeamableRequest(beamReq, null, 0);
		}

		/// <param name="deadline">The absolute deadline, carried over from an earlier attempt; null to start one.</param>
		/// <param name="authRetry">How many times this request was already retried after a gateway 403.</param>
		private Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> beamReq, long? deadline, int authRetry)
		{
			var socketContext = CurrentSocketContext;
			var activity = _activityProvider.Create(Constants.Features.Otel.TRACE_REQUEST, ContextActivity.Value);

			var uri = beamReq.uri;
			var body = beamReq.body;
			var method = beamReq.method;

			// peel off the first slash of the uri, because socket paths are not relative, they are absolute. // TODO: xxx gross.
			if (uri.StartsWith('/'))
			{
				uri = uri.Substring(1);
			}

			if (body == null)
			{
				body = new { }; // empty object.
			}

			// build websocket request...

			object bodyWrapper;
			if (body is string bodyJson)
			{
				// serialize this as a raw json object
				bodyWrapper = new RawJsonProvider {Json = bodyJson};
			}
			else
			{
				var json = JsonConvert.SerializeObject(body, UnitySerializationSettings.Instance);

				bodyWrapper = new RawJsonProvider {Json = json};
			}

			var req = new WebsocketRequest
			{
				method = method.ToString().ToLower(),
				body = body,
				path = uri,
				headers = new Dictionary<string, string>()
			};
			if (beamReq.headerInterceptor != null)
			{
				req.headers = beamReq.headerInterceptor.Invoke(req.headers);
			}

			// Stamp the scope override (if any) last so a per-request headerInterceptor can't drop it. This lets an
			// AssumeRealm fork target a realm (cid.pid) over the zone's (cid.zid) socket.
			if (!string.IsNullOrEmpty(ScopeOverride))
			{
				req.headers ??= new Dictionary<string, string>();
				req.headers[Constants.Requester.HEADER_SCOPE] = ScopeOverride;
			}

			if (_requestContext != null &&
			    !_requestContext.IsInvalidUser && // Check to see if the requester has an invalid user --- if it does, the request is being made during
			                                      // the initialization process without an Microservice.AssumeUser call being made before.
			    _requestContext.UserId > 0 && // '0' is not a valid playerId, and represents a null value.
			    beamReq.includeAuthHeader)
			{
				req.from = _requestContext.UserId;
			}

			var isAuthRequest = req.path == "gateway/nonce" || req.path == "gateway/auth";
			deadline ??= ComputeDeadline(isAuthRequest);
			// A lost nonce or auth reply used to hang the auth daemon, so those deadlines are enforced in every mode.
			var enforceDeadline = isAuthRequest || _env.EnforceSocketReliability;
			var attempt = socketContext.AddListener(req, uri, beamReq.parser, activity, deadline, enforceDeadline);

			var dict = new ArrayDict
			{
				[nameof(req.id)] = req.id,
				[nameof(req.method)] = req.method,
				[nameof(req.path)] = req.path,
				[nameof(req.from)] = req.from,
				[nameof(req.body)] = bodyWrapper,
				[nameof(req.headers)] = req.headers
			};
			activity.SetDisplay($"{method} {req.path} ({req.id})");
			activity.SetTags(new TelemetryAttributeCollection()
				.With(TelemetryAttributes.RequestPath(req.path))
				.With(TelemetryAttributes.RequestPlayerId(req.from ?? 0))
				.With(TelemetryAttributes.ConnectionRequestId(req.id)));

			var requestActivity = _activityProvider.Create(Constants.Features.Otel.TRACE_REQUEST_SEND, activity);

			requestActivity.SetDisplay($"Request ({req.id})");

			var msg = "";
			using (var stringBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				msg = Json.Serialize(dict, stringBuilder.Builder);
			}

			var truncatedMsg = msg.Substring(0, Math.Min(_env.LogTruncateLimit, msg.Length));
			BeamableZLoggerProvider.LogContext.Value.LogDebug("sending request " + truncatedMsg);

			// The auth daemon waits for the requests it counted to finish before it re-authenticates again, so this
			// attempt counts as processed when its listener ends, however it ends: reply, deadline, failed send or lost
			// connection. A retry after a 403 is a new attempt with its own count.
			var daemon = socketContext.Daemon;
			daemon.BumpRequestCounter();
			attempt.Then(_ => daemon.BumpRequestProcessedCounter());
			attempt.Error(_ => daemon.BumpRequestProcessedCounter());

			// The caller waits for the reply, not the send: a send stuck behind a reconnect or a blocked connection
			// can't hide the deadline. A send that fails for good ends the request with its error.
			socketContext.SendRequest(req.id, msg, _waitForAuthorization, _env.DisableOutboundWsCompression)
				.Then(_ => requestActivity.StopAndDispose(ActivityStatusCode.Ok))
				.Error(ex => requestActivity.StopAndDispose(ex));

			return attempt.RecoverWith(ex =>
			{
				if (ex is UnauthenticatedException unAuth && unAuth.Error?.service == "gateway")
				{
					if (authRetry > 0)
					{
						// Rejected again right after a successful re-auth: the gateway ended this session (its timeout
						// breaker) and will keep rejecting it. Re-authenticating again can't help; a new connection can.
						socketContext.ReportUnhealthyConnection(null, "the gateway rejected a request again right after re-authentication");
						activity.StopAndDispose(ex);
						throw ex;
					}

					// need to wait for authentication to finish...
					BeamableZLoggerProvider.LogContext.Value.ZLogDebug(
						$"Request {req.id} and {truncatedMsg} failed with 403. Will reauth and and retry.");

					daemon.WakeAuthThread();
					return socketContext.WaitForAuthorization(AuthWaitLimit(deadline), msg).ToPromise()
						.FlatMap(_ => BeamableRequest(beamReq, deadline, authRetry + 1))
						.Then(_ => activity.StopAndDispose(ActivityStatusCode.Ok))
						.RecoverWith(retryEx =>
						{
							activity.StopAndDispose(retryEx);
							throw retryEx;
						});
				}

				activity.StopAndDispose(ex);
				throw ex;
			}).Then(_ => activity.StopAndDispose(ActivityStatusCode.Ok));
		}

		/// <summary>The <see cref="Stopwatch.GetTimestamp"/> deadline for a new request, or null for none.</summary>
		private long? ComputeDeadline(bool isAuthRequest)
		{
			var timeout = ReplyTimeoutOverride.Value ?? TimeSpan.FromSeconds(Math.Max(0, _env.OutboundRequestTimeoutSeconds));
			if (isAuthRequest && (timeout <= TimeSpan.Zero || timeout > AuthRequestTimeout))
			{
				timeout = AuthRequestTimeout;
			}

			if (timeout <= TimeSpan.Zero) return null;
			return Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
		}

		/// <summary>How long a 403 retry may wait for re-authentication: what is left of the deadline, between 1s and 10s.</summary>
		private static TimeSpan AuthWaitLimit(long? deadline)
		{
			var limit = TimeSpan.FromSeconds(10);
			if (!deadline.HasValue) return limit;
			var left = TimeSpan.FromSeconds((deadline.Value - Stopwatch.GetTimestamp()) / (double)Stopwatch.Frequency);
			if (left < TimeSpan.FromSeconds(1)) return TimeSpan.FromSeconds(1);
			return left < limit ? left : limit;
		}

		public string EscapeURL(string url)
		{
			return System.Web.HttpUtility.UrlEncode(url);
		}
	}
}
