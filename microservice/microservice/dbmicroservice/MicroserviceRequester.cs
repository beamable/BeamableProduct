using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Pooling;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Core.Server.Common;
using Newtonsoft.Json;
using Serilog;
using UnityEngine;

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

   public class WebsocketErrorResponse
   {
      public int status;
      public string service;
      public string error;
      public string message;

      public override string ToString()
      {
         return $"Websocket Error Response. status=[{status}] service=[{service}] error=[{error}] message=[{message}]";
      }
   }

   public class WebsocketRequesterException : RequesterException
   {
      public WebsocketRequesterException(string method, string uri, long responseCode, string responsePayload) : base("Microservice Requester", method,
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
      void Resolve(RequestContext ctx);
   }
   public class WebsocketResponseListener<T> : IWebsocketResponseListener
   {
      public Promise<T> OnDone;

      public long Id { get; set; }
      public string Uri { get; set; }

      public Func<string, T> Parser { get; set; }

      public void Resolve(RequestContext ctx)
      {
         if (ctx.Status == 0)
         {
            OnDone.CompleteError(new WebsocketRequesterException(ctx.Method, Uri, ctx.Status, "noconnection"));
         }
         else if (ctx.Status == 403)
         {
            var error = JsonConvert.DeserializeObject<WebsocketErrorResponse>(ctx.Body, UnitySerializationSettings.Instance);
            OnDone.CompleteError(new UnauthenticatedException(error, ctx.Method, Uri, ctx.Status, ctx.Body));
         }
         else if (ctx.Status != 200)
         {
            OnDone.CompleteError(new WebsocketRequesterException(ctx.Method, Uri, ctx.Status, ctx.Body));
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
               OnDone.CompleteSuccess(result);
            }
            catch (Exception ex)
            {
               OnDone.CompleteError(ex);
            }
         }
      }
   }

   public class SocketRequesterContext
   {
      private readonly Func<Promise<IConnection>> _socketGetter;
      public Promise<IConnection> Socket => _socketGetter();

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

      public async Promise SendMessageSafely(string message, int retryCount=10)
      {
         var connection = await Socket;
         try
         {
            await connection.SendMessage(message);
         }
         catch
         {
            // hmm, an error happened. We should retry the send.
            if (retryCount > 0)
            {
               await SendMessageSafely(message, retryCount - 1);
               return;
            }

            // if we've retried a lot, and now its time to give up, we should at least return some sort of error response.
            Log.Error("Failed to send message. " + message);
            throw;
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

      public void HandleMessage(RequestContext ctx, string msg)
      {
         if (ctx.IsEvent)
         {

            var eventName = ctx.Path.Substring("event/".Length);
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


      public Promise<T> AddListener<T>(WebsocketRequest req, string uri, Func<string, T> parser)
      {
         var requestId = GetNextRequestId();
         if (_pendingMessages.ContainsKey(requestId))
         {
            BeamableSerilogProvider.Instance.Debug("The request {id} was already taken", requestId);
            return AddListener(req, uri, parser); // try again.
         }

         var promise = new Promise<T>();

         req.id = requestId;
         var listener = new WebsocketResponseListener<T>
         {
            Id = requestId,
            OnDone = promise,
            Parser = parser,
            Uri = uri,
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

   public class MicroserviceRequester : IBeamableRequester
   {
      private readonly IMicroserviceArgs _env;
      protected readonly RequestContext _requestContext;

      private readonly SocketRequesterContext _socketContext;

      private static Promise<Unit> _authenticationPromise = new Promise<Unit>();

      // TODO how do we handle Timeout errors?
      // TODO what does concurrency look like?

      public MicroserviceRequester(IMicroserviceArgs env, RequestContext requestContext, SocketRequesterContext socketContext)
      {
         _env = env;
         _requestContext = requestContext;
         _socketContext = socketContext;
      }

      public IAccessToken AccessToken { get; }


      private Promise<Unit> Reauthenticate()
      {
         lock (_authenticationPromise)
         {
            if (_authenticationPromise.IsCompleted)
            {
               // re-auth...
               return Authenticate();
            }
            else
            {
               return _authenticationPromise;
            }
         }
      }

      public async Promise Authenticate()
      {
         var oldPromise = _authenticationPromise;

         _authenticationPromise = new Promise<Unit>();

         string CalculateSignature(string text)
         {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] hash = md5.ComputeHash(data);
            return Convert.ToBase64String(hash);
         }

         Log.Debug("Authorizing WS connection");
         var res = await Request<MicroserviceNonceResponse>(Method.GET, "gateway/nonce");
         Log.Debug("Got nonce");
         var sig = CalculateSignature(_env.Secret + res.nonce);
         var req = new MicroserviceAuthRequest
         {
            cid = _env.CustomerID,
            pid = _env.ProjectName,
            signature = sig
         };
         var authRes = await Request<MicroserviceAuthResponse>(Method.POST, "gateway/auth", req);
         if (!string.Equals("ok", authRes.result))
         {
            Log.Error("Authorization failed. result=[{result}]", authRes.result);
            throw new Exception("Authorization failed");
         }

         Log.Debug("Authorization complete");

         _authenticationPromise.CompleteSuccess(PromiseBase.Unit);
         oldPromise.CompleteSuccess(PromiseBase.Unit);
      }

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

      public Promise<T> Request<T>(Method method, string uri, object body = null, bool includeAuthHeader = true, Func<string, T> parser = null,
         bool useCache = false)
      {
         // TODO: What do we do about includeAuthHeader?
         // TODO: What do we do about useCache?

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
             includeAuthHeader)
         {
            req.from = _requestContext.UserId;
         }

         var firstAttempt = _socketContext.AddListener(req, uri, parser);

         var dict = new ArrayDict
         {
            [nameof(req.id)] = req.id,
            [nameof(req.method)] = req.method,
            [nameof(req.path)] = req.path,
            [nameof(req.from)] = req.from,
            [nameof(req.body)] = bodyWrapper
         };
         var msg = "";
         using (var stringBuilder = StringBuilderPool.StaticPool.Spawn())
         {
            msg = Json.Serialize(dict, stringBuilder.Builder);
         }

         Log.Debug("sending request {msg}", msg);
         return _socketContext.SendMessageSafely(msg).FlatMap(_ =>
         {
            return firstAttempt.RecoverWith(ex =>
            {
               if (ex is UnauthenticatedException unAuth && unAuth.Error.service == "gateway")
               {
                  // need to wait for authentication to finish...
                  Log.Debug("Request {id} failed with 403. Will reauth and and retry.", req.id);
                  //
                  return Reauthenticate().FlatMap(_ => Request(method, uri, body, includeAuthHeader, parser));
               }

               throw ex;
            });
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
         var req = new MicroserviceProviderRequest
         {
            type = "event"
         };
         var promise = Request<MicroserviceProviderResponse>(Method.POST, "gateway/provider", req);

         return promise.Map(_ => new EmptyResponse());
      }

      public IBeamableRequester WithAccessToken(TokenResponse tokenResponse)
      {
         throw new NotImplementedException();
      }

      public string EscapeURL(string url)
      {
         return System.Web.HttpUtility.UrlEncode(url);
      }
   }
}