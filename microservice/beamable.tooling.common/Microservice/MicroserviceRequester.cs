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
	/// One outbound request waiting for its reply. Whichever path claims it from the pending table (reply, deadline,
	/// failed send, lost connection or shutdown) ends it.
	/// </summary>
	internal sealed class PendingRequest
	{
		public IWebsocketResponseListener Listener;
		public Action<Exception> Fail;
		/// <summary><see cref="Stopwatch.GetTimestamp"/> when the request started.</summary>
		public long StartedAt;
		/// <summary>The connection the request was written to; null until it was.</summary>
		public IConnection SentOn;
		public CancellationTokenRegistration Deadline;

		private readonly CancellationTokenSource _ended = new CancellationTokenSource();

		/// <summary>Cancelled when the request ends, so a write still queued for it is skipped.</summary>
		public CancellationToken Ended => _ended.Token;

		public void MarkEnded()
		{
			Deadline.Dispose();
			_ended.Cancel();
		}
	}

	/// <summary>
	/// Deadline and connection health settings for one <see cref="SocketRequesterContext"/>.
	/// </summary>
	public class SocketReliabilityOptions
	{
		/// <summary>How long a request may go unanswered, with no reply of any kind arriving, before the connection is judged broken. Zero turns the check off.</summary>
		public TimeSpan StallTimeout { get; set; }

		/// <summary>When false, a connection judged broken is only reported, not replaced.</summary>
		public bool Enforce { get; set; }

		public static SocketReliabilityOptions From(IMicroserviceArgs args) => new SocketReliabilityOptions
		{
			StallTimeout = TimeSpan.FromSeconds(Math.Max(0, args.SocketStallTimeoutSeconds)),
			Enforce = args.EnforceSocketReliability
		};
	}

	/// <summary>A point-in-time view of one connection's outbound requests, for health checks and diagnostics.</summary>
	public class SocketHealthSnapshot
	{
		public string Name { get; set; }
		public int PendingRequests { get; set; }
		public TimeSpan OldestPendingRequest { get; set; }
		/// <summary>Time since any reply arrived, or null if none has yet.</summary>
		public TimeSpan? SinceLastReply { get; set; }
		/// <summary>How long a request written since the last reply has gone unanswered, or null if none has.</summary>
		public TimeSpan? WaitingForReply { get; set; }
		public bool ReportedBroken { get; set; }

		public override string ToString() =>
			$"connection=[{Name}] pending=[{PendingRequests}] oldest-pending=[{OldestPendingRequest.TotalSeconds:0.0}s] " +
			$"since-last-reply=[{(SinceLastReply.HasValue ? $"{SinceLastReply.Value.TotalSeconds:0.0}s" : "never")}] " +
			$"waiting-for-reply=[{(WaitingForReply.HasValue ? $"{WaitingForReply.Value.TotalSeconds:0.0}s" : "no")}] " +
			$"reported-broken=[{ReportedBroken}]";
	}

	public class SocketRequesterContext
	{
		private const int HandledEventMemory = 1024;

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

		private ConcurrentDictionary<long, PendingRequest> _pendingMessages = new ConcurrentDictionary<long, PendingRequest>();
		private ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>> _subscriptions = new ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>>();
		private long _lastRequestId = 0;

		// Each gateway event message id with its delivery, so an event that reaches this context more than once runs once.
		private readonly ConcurrentDictionary<string, Lazy<Task>> _events = new ConcurrentDictionary<string, Lazy<Task>>();
		private readonly ConcurrentQueue<string> _eventsOrder = new ConcurrentQueue<string>();

		private long _lastReplyAt;
		private IConnection _reportedConnection;

		// The first request written since the last reply arrived; null once a reply arrives.
		private sealed class Waiting
		{
			public long Since;
			public IConnection On;
		}
		private Waiting _waiting;

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

		/// <summary><see cref="Stopwatch.GetTimestamp"/> when any reply last arrived, or 0 if none has.</summary>
		public long LastReplyAt => Volatile.Read(ref _lastReplyAt);

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

		public SocketHealthSnapshot GetHealth(long now)
		{
			var lastReply = LastReplyAt;
			var waiting = Volatile.Read(ref _waiting);
			var current = CurrentConnection;
			return new SocketHealthSnapshot
			{
				Name = Name,
				PendingRequests = PendingRequestCount,
				OldestPendingRequest = OldestPendingRequestAge,
				SinceLastReply = lastReply == 0 ? (TimeSpan?)null : Elapsed(lastReply, now),
				WaitingForReply = waiting == null ? (TimeSpan?)null : Elapsed(waiting.Since, now),
				ReportedBroken = current != null && ReferenceEquals(Volatile.Read(ref _reportedConnection), current)
			};
		}


		public Task WaitForAuthorization(TimeSpan timeout=default, string message=null) =>
			WaitForAuthorization(timeout, message, CancellationToken.None);

		/// <summary>Like <see cref="WaitForAuthorization(TimeSpan, string)"/>, but gives up when <paramref name="giveUp"/> is cancelled.</summary>
		public async Task WaitForAuthorization(TimeSpan timeout, string message, CancellationToken giveUp)
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
				giveUp.ThrowIfCancellationRequested();
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
			await SendWithRetries(message, awaitAuthorization, retryCount, sw, disableCompression, CancellationToken.None, null);
		}

		/// <summary>
		/// Sends a request whose listener is already registered, and ends the request if every attempt fails. It stops once
		/// the request has ended (for example, at its deadline), including a write still queued for it, so a request that
		/// already failed its caller is never sent later, where the caller's retry could repeat it.
		/// </summary>
		internal Promise<Unit> SendRequest(long requestId, string message, bool awaitAuthorization, bool disableCompression)
		{
			return Send().ToPromise();

			async Task Send()
			{
				if (!_pendingMessages.TryGetValue(requestId, out var pending)) return;
				try
				{
					await SendWithRetries(message, awaitAuthorization, 10, null, disableCompression, pending.Ended, connection =>
					{
						pending.SentOn = connection;
						var since = new Waiting { Since = Stopwatch.GetTimestamp(), On = connection };
						if (Volatile.Read(ref _waiting) == null) Interlocked.CompareExchange(ref _waiting, since, null);
					});
				}
				catch (OperationCanceledException) when (pending.Ended.IsCancellationRequested)
				{
					// the request already ended; nothing was sent.
				}
				catch (Exception ex)
				{
					FailIfPending(requestId, ex);
					throw;
				}
			}
		}

		/// <summary>
		/// Sends a reply to a request, or an ack of an event, that arrived on <paramref name="origin"/>. It only goes back on
		/// that connection: the gateway matches it to the session that sent the request, and numbers requests per session,
		/// so on another connection the same id belongs to a different request. It doesn't wait for re-authentication,
		/// because the gateway doesn't check auth for replies. A reply that can't be sent is dropped with a warning: a
		/// failed write closes the connection, so there is nothing to retry, and the gateway times the request out.
		/// </summary>
		public async Promise SendReply(IConnection origin, string message, Stopwatch sw = null, bool disableCompression = false)
		{
			if (origin == null)
			{
				await SendMessageSafely(message, awaitAuthorization: false, sw: sw, disableCompression: disableCompression);
				return;
			}

			try
			{
				await WriteTo(origin, message, sw, disableCompression, CancellationToken.None);
			}
			catch (Exception ex)
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Dropped a reply because the connection its request arrived on closed. {ex.GetType().Name}: {ex.Message}");
			}
		}

		private async Task<IConnection> SendWithRetries(string message, bool awaitAuthorization, int retryCount, Stopwatch sw,
			bool disableCompression, CancellationToken ended, Action<IConnection> onWriting)
		{
			var failures = new List<Exception>();
			for (var retry = 0; retry < retryCount; retry++)
			{
				ended.ThrowIfCancellationRequested();
				try
				{
					var connection = await Socket;
					if (awaitAuthorization)
					{
						await WaitForAuthorization( message: message);
					}

					// the request may have ended while this waited for the socket or for authorization.
					ended.ThrowIfCancellationRequested();
					onWriting?.Invoke(connection);
					await WriteTo(connection, message, sw, disableCompression, ended);
					return connection;
				}
				catch (OperationCanceledException) when (ended.IsCancellationRequested)
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

		private async Task WriteTo(IConnection connection, string message, Stopwatch sw, bool disableCompression, CancellationToken skipIfEnded)
		{
			// authorization needs to be complete if this is any message _other_ than auth related
			// Use compression if negotiated and message is large enough
			var codec = Daemon?.NegotiatedCodec;
			if (codec != null && !disableCompression && SocketCompression.ShouldCompress(message))
			{
				var compressed = SocketCompression.Compress(message, codec);
				await connection.SendBinaryMessage(compressed, sw, skipIfEnded);
			}
			else
			{
				await connection.SendMessage(message, sw, skipIfEnded);
			}
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

				var messageId = EventMessageId(ctx);
				if (messageId == null)
				{
					await DispatchEvent(provider, ctx, eventName);
					return;
				}

				await DispatchEventOnce(provider, ctx, eventName, messageId);

			} else if (TryClaim(ctx.Id, out var pending))
			{
				// this is a response to some pending request...
				NoteReply();
				parentActivity.SetDisplay($"Response ({ctx.Id})");
				pending.Listener.Resolve(ctx);
			}
			else
			{
				NoteReply(); // a late reply still shows the connection carries traffic both ways.
				// Request ids count down from -1 and are never reused, so an id in that range was a request that already ended.
				if (ctx.Id < 0 && ctx.Id >= Interlocked.Read(ref _lastRequestId))
				{
					BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Ignoring a late reply to request {ctx.Id}, which already ended.");
				}
				else
				{
					BeamableLogger.LogError("There was no listener for request {id}", ctx.Id);
				}
			}
		}

		private async Task DispatchEvent(IDependencyProvider provider, MicroserviceRequestContext ctx, string eventName)
		{
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
		}

		private static string EventMessageId(RequestContext ctx)
		{
			if (ctx.Headers == null || !ctx.Headers.TryGetValue("msgId", out var messageId) || string.IsNullOrEmpty(messageId))
			{
				return null;
			}

			return messageId;
		}

		/// <summary>
		/// Runs an event's handlers once per gateway message id. Every connection receives realm events, so the same event
		/// can reach this context more than once during a failover. Every copy shares the first delivery's outcome, so none
		/// is acked before the event was handled. A failed delivery is forgotten: it is acked 500, the gateway redelivers it
		/// with the same id, and that delivery runs the handlers again.
		/// </summary>
		private async Task DispatchEventOnce(IDependencyProvider provider, MicroserviceRequestContext ctx, string eventName, string messageId)
		{
			var mine = new Lazy<Task>(() => DispatchEvent(provider, ctx, eventName));
			var delivery = _events.GetOrAdd(messageId, mine);
			if (delivery == mine)
			{
				_eventsOrder.Enqueue(messageId);
				while (_eventsOrder.Count > HandledEventMemory && _eventsOrder.TryDequeue(out var oldest))
				{
					_events.TryRemove(oldest, out _);
				}
			}
			else
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Event {eventName} id=[{ctx.Id}] was already delivered; sharing that delivery's outcome.");
			}

			try
			{
				await delivery.Value;
			}
			catch
			{
				((ICollection<KeyValuePair<string, Lazy<Task>>>)_events).Remove(new KeyValuePair<string, Lazy<Task>>(messageId, delivery));
				throw;
			}
		}


		public Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser, BeamActivity parentActivity)
		{
			return AddListener(req, uri, parser, parentActivity, CancellationToken.None, false, Stopwatch.GetTimestamp());
		}

		/// <summary>
		/// Registers a listener for the reply to <paramref name="req"/> and assigns its id.
		/// </summary>
		/// <param name="deadline">Cancelled when the request runs out of time.</param>
		/// <param name="enforceDeadline">When true, a request out of time fails with <see cref="RequestTimeoutException"/>; otherwise it is only reported.</param>
		/// <param name="startedAt">When the request started, as a <see cref="Stopwatch.GetTimestamp"/>.</param>
		internal Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser, BeamActivity parentActivity,
			CancellationToken deadline, bool enforceDeadline, long startedAt)
		{
			var requestId = GetNextRequestId();
			if (_pendingMessages.ContainsKey(requestId))
			{
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"The request {requestId} was already taken");
				return AddListener(req, uri, parser, parentActivity, deadline, enforceDeadline, startedAt); // try again.
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
				Activity = parentActivity
			};
			var pending = new PendingRequest { Listener = listener, Fail = promise.CompleteError, StartedAt = startedAt };

			if (!_pendingMessages.TryAdd(requestId, pending))
			{
				promise.CompleteError(new Exception("request Id has already been taken in socket context. id=" +requestId));
				return promise;
			}

			SocketReliabilityMetrics.Pending.Add(1);
			if (deadline.CanBeCanceled)
			{
				pending.Deadline = deadline.Register(() => OnDeadline(pending, enforceDeadline));
			}
			return promise;
		}

		private void OnDeadline(PendingRequest pending, bool enforced)
		{
			var listener = pending.Listener;
			var elapsed = Elapsed(pending.StartedAt, Stopwatch.GetTimestamp());
			if (enforced)
			{
				if (!TryClaim(listener.Id, out _)) return;
				SocketReliabilityMetrics.DeadlineExceeded.Add(1,
					new KeyValuePair<string, object>("enforced", true),
					new KeyValuePair<string, object>("path", listener.Path));
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Request {listener.Method} {listener.Path} id=[{listener.Id}] exceeded its deadline after {elapsed.TotalSeconds:0.0}s with no reply; failing it.");
				pending.Fail(new RequestTimeoutException(listener.Method, listener.Path, listener.Id, elapsed));
				return;
			}

			if (!_pendingMessages.ContainsKey(listener.Id)) return;
			SocketReliabilityMetrics.DeadlineExceeded.Add(1,
				new KeyValuePair<string, object>("enforced", false),
				new KeyValuePair<string, object>("path", listener.Path));
			BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Request {listener.Method} {listener.Path} id=[{listener.Id}] exceeded its deadline after {elapsed.TotalSeconds:0.0}s with no reply. Still waiting, because BEAM_SOCKET_RELIABILITY_ENFORCE is off.");
		}

		public bool TryGetListener(long id, out IWebsocketResponseListener listener)
		{
			var found = _pendingMessages.TryGetValue(id, out var pending);
			listener = pending?.Listener;
			return found;
		}

		/// <summary>
		/// Takes ownership of a pending request so it can be ended. Every path that ends a request claims it first; only
		/// the one that wins may complete it.
		/// </summary>
		private bool TryClaim(long id, out PendingRequest pending)
		{
			if (!_pendingMessages.TryRemove(id, out pending)) return false;
			pending.MarkEnded();
			SocketReliabilityMetrics.Pending.Add(-1);
			return true;
		}

		public void Remove(long id)
		{
			if (!TryClaim(id, out _))
			{
				throw new Exception("request Id could not be removed from the socket context. id=" +id);
			}
		}

		/// <summary>Ends a request with <paramref name="ex"/> if nothing else has ended it yet.</summary>
		public bool FailIfPending(long id, Exception ex)
		{
			if (!TryClaim(id, out var pending)) return false;
			pending.Fail(ex);
			return true;
		}

		/// <summary>
		/// Ends every request that was sent on <paramref name="connection"/>; the gateway can't answer them on any other
		/// connection. Requests not sent yet are left to go out on the replacement.
		/// </summary>
		public int FailPendingFor(IConnection connection, Exception ex)
		{
			var waiting = Volatile.Read(ref _waiting);
			if (waiting != null && ReferenceEquals(waiting.On, connection)) Interlocked.CompareExchange(ref _waiting, null, waiting);

			var failed = FailPending(pending => ReferenceEquals(pending.SentOn, connection), ex);
			if (failed > 0)
			{
				SocketReliabilityMetrics.ConnectionLost.Add(failed);
				BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Failed {failed} request(s) that were sent on connection {Name} before it closed.");
			}
			return failed;
		}

		/// <summary>Ends every pending request, for shutdown.</summary>
		public int FailAllPending(Exception ex) => FailPending(_ => true, ex);

		private int FailPending(Func<PendingRequest, bool> which, Exception ex)
		{
			var failed = 0;
			foreach (var pair in _pendingMessages)
			{
				if (!which(pair.Value) || !TryClaim(pair.Key, out var pending)) continue;
				failed++;
				pending.Fail(ex);
			}
			return failed;
		}

		private void NoteReply()
		{
			Volatile.Write(ref _lastReplyAt, Stopwatch.GetTimestamp());
			Volatile.Write(ref _waiting, null);
		}

		/// <summary>
		/// Reports the connection broken when a request written to it has gone unanswered for the stall limit, with no reply
		/// of any kind arriving since. Called regularly by the process health monitor.
		/// </summary>
		public void CheckForStall(long now)
		{
			var limit = Reliability.StallTimeout;
			var waiting = Volatile.Read(ref _waiting);
			if (limit <= TimeSpan.Zero || waiting == null) return;

			var silent = Elapsed(waiting.Since, now);
			if (silent < limit || !ReferenceEquals(waiting.On, CurrentConnection)) return;
			ReportUnhealthyConnection(waiting.On, $"no reply for {silent.TotalSeconds:0}s to requests sent on it, and no other reply since", "reply_stall");
		}

		/// <summary>
		/// Reports that this context's connection looks broken. In enforce mode the owner is asked to replace it through
		/// <see cref="ConnectionUnhealthy"/>; otherwise it is only logged. Each connection is reported once.
		/// </summary>
		/// <param name="connection">The broken connection, or null for whichever connection is current.</param>
		/// <param name="reasonTag">A short, fixed tag for the metric.</param>
		public void ReportUnhealthyConnection(IConnection connection, string reason, string reasonTag = "other")
		{
			connection ??= CurrentConnection;
			if (connection == null) return;
			if (ReferenceEquals(Interlocked.Exchange(ref _reportedConnection, connection), connection)) return;

			var enforce = Reliability.Enforce;
			SocketReliabilityMetrics.UnhealthyConnections.Add(1,
				new KeyValuePair<string, object>("reason", reasonTag),
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

		private readonly Func<SocketRequesterContext> _socketContext;
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
			: this(env, requestContext, () => socketContext, waitForAuthorization, activityProvider)
		{
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
		{
			_env = env;
			_requestContext = requestContext;
			_socketContext = socketContextSelector;
			_waitForAuthorization = waitForAuthorization;
			_activityProvider = activityProvider;
		}

		private SocketRequesterContext CurrentSocketContext => _socketContext();

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
		/// Acknowledge a message from the websocket, on the connection in use now. Prefer
		/// <see cref="Acknowledge(RequestContext, IConnection, WebsocketErrorResponse)"/>, which keeps the ack on the
		/// connection the event arrived on.
		/// </summary>
		/// <param name="ctx">The request you wish to ack</param>
		/// <param name="error">an error, or null for a 200 ack.</param>
		/// <returns></returns>
		public Promise<Unit> Acknowledge(RequestContext ctx, WebsocketErrorResponse error=null) => Acknowledge(ctx, null, error);

		/// <summary>
		/// Acknowledge an event on the connection it arrived on. An ack is a reply: the gateway matches it to the session
		/// that delivered the event and numbers requests per session, so on another connection the same id belongs to a
		/// different request. When that connection is gone, the ack is not sent at all, and the gateway redelivers.
		/// </summary>
		/// <param name="ctx">The event to ack</param>
		/// <param name="origin">The connection the event arrived on</param>
		/// <param name="error">an error, or null for a 200 ack.</param>
		public Promise<Unit> Acknowledge(RequestContext ctx, IConnection origin, WebsocketErrorResponse error = null)
		{
			if (!ctx.IsEvent)
			{
				return Promise<Unit>.Successful(PromiseBase.Unit);
			}

			return CurrentSocketContext.SendReply(origin, BuildAck(ctx, error), disableCompression: _env.DisableOutboundWsCompression);
		}

		private static string BuildAck(RequestContext ctx, WebsocketErrorResponse error)
		{
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
			using (var stringBuilder = StringBuilderPool.StaticPool.Spawn())
			{
				return Json.Serialize(dict, stringBuilder.Builder);
			}
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
			var activity = _activityProvider.Create(Constants.Features.Otel.TRACE_REQUEST, ContextActivity.Value);
			var path = beamReq.uri.StartsWith('/') ? beamReq.uri.Substring(1) : beamReq.uri;
			var isAuthRequest = path == "gateway/nonce" || path == "gateway/auth";
			// A lost nonce or auth reply used to hang the auth daemon, so those deadlines are enforced in every mode.
			var enforceDeadline = isAuthRequest || _env.EnforceSocketReliability;
			// One deadline for the whole request, shared by a retry after re-authentication.
			var timeout = ReplyTimeout(isAuthRequest);
			var deadline = timeout > TimeSpan.Zero ? new CancellationTokenSource(timeout) : null;
			var startedAt = Stopwatch.GetTimestamp();

			var result = new Promise<T>();
			Attempt(beamReq, activity, deadline?.Token ?? CancellationToken.None, enforceDeadline, startedAt, false)
				.Then(value =>
				{
					activity.StopAndDispose(ActivityStatusCode.Ok);
					deadline?.Dispose();
					result.CompleteSuccess(value);
				})
				.Error(ex =>
				{
					activity.StopAndDispose(ex);
					deadline?.Dispose();
					result.CompleteError(ex);
				});
			return result;
		}

		/// <param name="retried">Whether this attempt is the retry after a gateway 403.</param>
		private Promise<T> Attempt<T>(SDKRequesterOptions<T> beamReq, BeamActivity activity, CancellationToken deadline,
			bool enforceDeadline, long startedAt, bool retried)
		{
			var socketContext = CurrentSocketContext;
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

			var attempt = socketContext.AddListener(req, uri, beamReq.parser, activity, deadline, enforceDeadline, startedAt);

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
			// attempt counts as processed when its listener ends, however it ends.
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
				if (!(ex is UnauthenticatedException unAuth && unAuth.Error?.service == "gateway")) throw ex;
				if (retried)
				{
					// Rejected again right after a successful re-auth: the gateway ended this session (its timeout
					// breaker) and will keep rejecting it. Re-authenticating again can't help; a new connection can.
					socketContext.ReportUnhealthyConnection(null, "the gateway rejected a request again right after re-authentication", "rejected_after_reauth");
					throw ex;
				}

				// need to wait for authentication to finish...
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug(
					$"Request {req.id} and {truncatedMsg} failed with 403. Will reauth and and retry.");

				daemon.WakeAuthThread();
				// The 403 ended this attempt's listener, so the deadline is checked here while re-authentication runs.
				var giveUp = enforceDeadline ? deadline : CancellationToken.None;
				return socketContext.WaitForAuthorization(default, msg, giveUp).ToPromise()
					.RecoverWith(waitEx => throw (giveUp.IsCancellationRequested ? Timeout(req, startedAt) : waitEx))
					.FlatMap(_ => giveUp.IsCancellationRequested
						? throw Timeout(req, startedAt)
						: Attempt(beamReq, activity, deadline, enforceDeadline, startedAt, true));
			});
		}

		/// <summary>How long a new request may wait for its reply, or zero for no deadline.</summary>
		private TimeSpan ReplyTimeout(bool isAuthRequest)
		{
			var timeout = ReplyTimeoutOverride.Value ?? TimeSpan.FromSeconds(Math.Max(0, _env.OutboundRequestTimeoutSeconds));
			if (isAuthRequest && (timeout <= TimeSpan.Zero || timeout > AuthRequestTimeout))
			{
				timeout = AuthRequestTimeout;
			}

			return timeout;
		}

		/// <summary>The timeout for a request whose deadline passed while it waited to be retried.</summary>
		private static RequestTimeoutException Timeout(WebsocketRequest req, long startedAt)
		{
			var elapsed = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - startedAt) / (double)Stopwatch.Frequency);
			SocketReliabilityMetrics.DeadlineExceeded.Add(1,
				new KeyValuePair<string, object>("enforced", true),
				new KeyValuePair<string, object>("path", req.path));
			BeamableZLoggerProvider.LogContext.Value.ZLogWarning($"Request {req.method} {req.path} id=[{req.id}] exceeded its deadline after {elapsed.TotalSeconds:0.0}s while re-authenticating; not retrying it.");
			return new RequestTimeoutException(req.method, req.path, req.id, elapsed);
		}

		public string EscapeURL(string url)
		{
			return System.Web.HttpUtility.UrlEncode(url);
		}
	}
}
