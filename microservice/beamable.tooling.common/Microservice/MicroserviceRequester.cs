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
      void Resolve(RequestContext ctx);
   }
   public class PlatformSubscription<T> : IPlatformSubscription
   {
      public string EventName;
      public Action Unsubscribe;
      public Action<T> OnEvent;

      public void Resolve(RequestContext ctx)
      {
         var data = JsonConvert.DeserializeObject<T>(ctx.Body, UnitySerializationSettings.Instance);
         OnEvent?.Invoke(data);
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

   public class SocketRequesterContext
   {
	   public MicroserviceAuthenticationDaemon Daemon { get; set; }
	   private readonly Func<Promise<IConnection>> _socketGetter;
      public Promise<IConnection> Socket => _socketGetter();

      public IActivityProvider ActivityProvider { get; set; }
      private ConcurrentDictionary<long, IWebsocketResponseListener> _pendingMessages = new ConcurrentDictionary<long, IWebsocketResponseListener>();
      private ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>> _subscriptions = new ConcurrentDictionary<string, SynchronizedCollection<IPlatformSubscription>>();
      private long _lastRequestId = 0;

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

      public async Promise SendMessageSafely(string message, bool awaitAuthorization=true, int retryCount=10, Stopwatch sw=null)
      {
         var failures = new List<Exception>();
         for (var retry = 0; retry < retryCount; retry++)
         {
	         try
	         {
		         var connection = await Socket;
		         if (awaitAuthorization)
		         {
			        await WaitForAuthorization( message: message);
		         }

		         // authorization needs to be complete if this is any message _other_ than auth related
		         // Use compression if negotiated and message is large enough
		         var codec = Daemon?.NegotiatedCodec;
		         if (codec != null && SocketCompression.ShouldCompress(message))
		         {
			         var compressed = SocketCompression.Compress(message, codec);
			         await connection.SendBinaryMessage(compressed, sw);
		         }
		         else
		         {
			         await connection.SendMessage(message, sw);
		         }
		         return;
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
         var subscription = new PlatformSubscription<T>
         {
            EventName = eventName
         };
         subscription.OnEvent += callback;

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

      public void HandleMessage(RequestContext ctx, BeamActivity parentActivity=null)
      {
         if (parentActivity == null)
         {
            parentActivity = BeamActivity.Noop;
         }
         
         if (ctx.IsEvent)
         {
            var eventName = ctx.Path.Substring("event/".Length);
            parentActivity.SetDisplay($"On{eventName}");

            if (TryGetEventSubscriptions(eventName, out var subscriptions))
            {
               var startCount = subscriptions.Count; // take the count at the moment the event is processed. If something else subscribes at the same frame, they're too late.
               for (var i = 0; i < startCount; i++) // TODO: there is still a bug with multi-threaded access; if an item is removed/ unsub
               {
                  subscriptions[i].Resolve(ctx);
               }
            }

            //</color> <color=blue>BaseGet [{"id":1,"method":"post","path":"event/content.manifest","body":{"categories":["tournaments","announcements","listings","items","stores","sagamap","skus","currency","leaderboards","emails","game_types"]}}

         } else if (TryGetListener(ctx.Id, out var listener))
         {
            // this is a response to some pending request...
            try
            {
               parentActivity.SetDisplay($"Response ({ctx.Id})");
               listener.Resolve(ctx);
            }
            finally
            {               
               Remove(ctx.Id);
            }
         }
         else
         {
            BeamableLogger.LogError("There was no listener for request {id}", ctx.Id);
         }
      }


      public Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser, BeamActivity parentActivity)
      {
         var requestId = GetNextRequestId();
         if (_pendingMessages.ContainsKey(requestId))
         {
            BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"The request {requestId} was already taken");
            return AddListener(req, uri, parser, parentActivity); // try again.
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

         if (!_pendingMessages.TryAdd(requestId, listener))
         {
            promise.CompleteError(new Exception("request Id has already been taken in socket context. id=" +requestId));
         }
         return promise;
      }

      public bool TryGetListener(long id, out IWebsocketResponseListener listener)
      {
         return _pendingMessages.TryGetValue(id, out listener);
      }

      public void Remove(long id)
      {
         if (!_pendingMessages.TryRemove(id, out _))
         {
            throw new Exception("request Id could not be removed from the socket context. id=" +id);
         }
      }


   }

   public class MicroserviceRequester : IRequester
   {
      private readonly IMicroserviceArgs _env;
      protected readonly RequestContext _requestContext;

      private readonly SocketRequesterContext _socketContext;
      private readonly IActivityProvider _activityProvider;
      private readonly bool _waitForAuthorization;

      // TODO how do we handle Timeout errors?
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
         _socketContext = socketContext;
         _waitForAuthorization = waitForAuthorization;
         _activityProvider = activityProvider;
      }

      public IAccessToken AccessToken { get; }

      public Task WaitForAuthorization(TimeSpan timeout = default, string message=null) => _socketContext.WaitForAuthorization(timeout, message);

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

         return _socketContext.SendMessageSafely(msg);
      }

      public static AsyncLocal<BeamActivity> ContextActivity { get; set; } = new AsyncLocal<BeamActivity>();
      public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true,
	      Func<string, T> parser = null, bool useCache = false)
      {
// TODO: What do we do about includeAuthHeader?
         // TODO: What do we do about useCache?
          
         
         var activity = _activityProvider.Create(Constants.Features.Otel.TRACE_REQUEST, ContextActivity.Value);
         
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
         };


         if (_requestContext != null &&
             !_requestContext.IsInvalidUser && // Check to see if the requester has an invalid user --- if it does, the request is being made during
                                               // the initialization process without an Microservice.AssumeUser call being made before.
             _requestContext.UserId > 0 && // '0' is not a valid playerId, and represents a null value.
             includeAuthHeader)
         {
            req.from = _requestContext.UserId;
         }

         var firstAttempt = _socketContext.AddListener(req, uri, parser, activity);

         var dict = new ArrayDict
         {
            [nameof(req.id)] = req.id,
            [nameof(req.method)] = req.method,
            [nameof(req.path)] = req.path,
            [nameof(req.from)] = req.from,
            [nameof(req.body)] = bodyWrapper
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
         _socketContext.Daemon.BumpRequestCounter();
         return _socketContext.SendMessageSafely(msg, _waitForAuthorization).FlatMap(_ =>
            {
               requestActivity.StopAndDispose(ActivityStatusCode.Ok);
               
               return firstAttempt.RecoverWith(ex =>
               {
                  if (ex is UnauthenticatedException unAuth && unAuth.Error.service == "gateway")
                  {
                     // need to wait for authentication to finish...
                     BeamableZLoggerProvider.LogContext.Value.ZLogDebug(
                        $"Request {req.id} and {truncatedMsg} failed with 403. Will reauth and and retry.");

                     _socketContext.Daemon.WakeAuthThread();
                     var waitForAuth = WaitForAuthorization(message: msg).ToPromise();
                     return waitForAuth
                           .FlatMap(x =>
                              Request(method, uri, body, includeAuthHeader, parser, useCache))
                        ;
                  }

                  _socketContext.Daemon.BumpRequestProcessedCounter();
                  activity.StopAndDispose(ex);
                  requestActivity.StopAndDispose(ex);
                  throw ex;
               });
            })
	         .Then(_ =>
            {
               activity.StopAndDispose(ActivityStatusCode.Ok);
               _socketContext.Daemon.BumpRequestProcessedCounter();
            });
      }

      /// <summary>
      /// Each socket only needs to set up one subscription to the server.
      /// All events will get piped to the client.
      /// It's the client job to filter the events, and decide what is valuable.
      /// </summary>
      /// <returns></returns>
      public Promise<EmptyResponse> InitializeSubscription()
      {
         var req = new MicroserviceEventProviderRequest
         {
            type = "event",
            evtWhitelist = new []
            {
	            Constants.Features.Services.CONTENT_UPDATE_EVENT,
	            Constants.Features.Services.REALM_CONFIG_UPDATE_EVENT,
				Constants.Features.Services.LOGGING_CONTEXT_UPDATE_EVENT,
            }
         };
         var promise = Request<MicroserviceProviderResponse>(Method.POST, "gateway/provider", req);

         return promise.Map(_ => new EmptyResponse());
      }

      public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
      {
         throw new NotImplementedException();
      }

      public Promise<T> BeamableRequest<T>(SDKRequesterOptions<T> req)
      {
	      throw new NotImplementedException();
      }

      public string EscapeURL(string url)
      {
         return System.Web.HttpUtility.UrlEncode(url);
      }
   }
}
