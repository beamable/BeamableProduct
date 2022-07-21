using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Beamable.Server.Content;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Serilog;
using System.Linq.Expressions;

namespace Beamable.Microservice.Tests.Socket
{
    public class TestSocketProvider : IConnectionProvider
    {
        private readonly Action<TestSocket> _configure;

        public TestSocketProvider(Action<TestSocket> configure)
        {
            _configure = configure;
        }
        public IConnection Create(string _)
        {
            var socket = new TestSocket();
            _configure(socket);
            return socket;
        }
    }

    public static class TestSocketRequestMatcherExtensions
    {

        public static TestSocketMessageMatcher WithRoutePrefix(this TestSocketMessageMatcher matcher, string prefix)
        {
            return And(matcher, MessageMatcher.WithRoutePrefix(prefix));
        }
        public static TestSocketMessageMatcher WithRouteContains(this TestSocketMessageMatcher matcher, string contains)
        {
            return And(matcher, MessageMatcher.WithRouteContains(contains));
        }
        public static TestSocketMessageMatcher WithReqId(this TestSocketMessageMatcher matcher, int id)
        {
            return And(matcher, MessageMatcher.WithReqId(id));
        }

        public static TestSocketMessageMatcher WithStatus(this TestSocketMessageMatcher matcher, int status)
        {
            return And(matcher, MessageMatcher.WithStatus(status));
        }

        public static TestSocketMessageMatcher WithMethod(this TestSocketMessageMatcher matcher, Method method)
        {
            return And(matcher, MessageMatcher.WithMethod(method));
        }

        public static TestSocketMessageMatcher WithGet(this TestSocketMessageMatcher matcher)
        {
            return And(matcher, MessageMatcher.WithGet());
        }
        public static TestSocketMessageMatcher WithPost(this TestSocketMessageMatcher matcher)
        {
            return And(matcher, MessageMatcher.WithPost());
        }
        public static TestSocketMessageMatcher WithDelete(this TestSocketMessageMatcher matcher)
        {
            return And(matcher, MessageMatcher.WithDelete());
        }

        public static TestSocketMessageMatcher WithFrom(this TestSocketMessageMatcher matcher, long fromId)
        {
            return And(matcher, MessageMatcher.WithFrom(fromId));
        }

        public static TestSocketMessageMatcher WithBody<T>(this TestSocketMessageMatcher matcher,
           Func<T, bool> bodyMatcher)
        {
            return And(matcher, MessageMatcher.WithBody(bodyMatcher));
        }

        public static TestSocketMessageMatcher WithPayload<T>(this TestSocketMessageMatcher matcher,
           Func<T, bool> payloadMatcher)
        {
            return And(matcher, MessageMatcher.WithPayload(payloadMatcher));
        }

        public static TestSocketMessageMatcher WithPayload(this TestSocketMessageMatcher matcher, string payload)
        {
            return And(matcher, MessageMatcher.WithPayload(payload));
        }

        public static TestSocketMessageMatcher WithPayload(this TestSocketMessageMatcher matcher, JObject payload)
        {
            return And(matcher, MessageMatcher.WithPayload(payload));
        }

        public static TestSocketMessageMatcher And(this TestSocketMessageMatcher predicate1, TestSocketMessageMatcher predicate2)
        {
            return (req) => predicate1(req) && predicate2(req);
        }
    }


    public class MessageMatcher
    {
        public static TestSocketMessageMatcher Build()
        {
            return (req) => true;
        }

        public static TestSocketMessageMatcher WithBody<T>(Func<T, bool> matcher)
        {
            return req =>
            {

                switch (req.body)
                {
                    case null: return false;
                    case JObject jObject:
                        var parsed = jObject.ToObject<T>();
                        return matcher(parsed);
                    case T typedBody:
                        return matcher(typedBody);
                    default:
                        return false;
                }

                // return req.body switch
                // {
                //    null => false,
                //    JObject jObject => matcher(jObject.ToObject<T>()),
                //    string json => matcher(JsonConvert.DeserializeObject<T>(json)),
                //    T typedBody => matcher(typedBody),
                //    _ => false
                // };
            };
        }

        public static TestSocketMessageMatcher WithPayload(string raw)
        {
            return req =>
            {
                // Have to do this otherwise it becomes sensitive to the test-case's JSON's formatting which can be a pain to manage in cases where
                // the payload is large.
                if (req.body is JObject obj)
                {
                    var rawJObj = JObject.Parse(raw);
                    return JToken.DeepEquals(rawJObj, obj);
                }
                return req.body.ToString().Equals(raw);
            };
        }

        public static TestSocketMessageMatcher WithPayload(JObject raw)
        {
            return req =>
            {
                // Have to do this otherwise it becomes sensitive to the test-case's JSON's formatting which can be a pain to manage in cases where
                // the payload is large.
                if (req.body is JObject obj)
                {
                    return JToken.DeepEquals(raw, obj);
                }

                return false;
            };
        }

        public static TestSocketMessageMatcher WithPayload<T>(Func<T, bool> matcher)
        {
            bool Check(object req)
            {
                switch (req)
                {
                    case string json:// when json.StartsWith("{"):
                        {
                            string tmpJson = json.Replace(@"\", "");

                            if (Json.IsValidJson(tmpJson))
                            {
                                var token = JToken.Parse(tmpJson);
                                return !string.IsNullOrEmpty(token.ToString());
                            }
                            else
                            {
                               // var payload = JsonConvert.DeserializeObject<T>(tmpJson);
                               if (json is T x)
                               {
                                   return matcher(x);
                               }
                               else
                               {
                                   return false;
                               }
                            }
                        }
                    case JObject payloadJObject when payloadJObject.TryGetValue("payload", out var payloadToken) :
                        {
                            bool success = true;
                            var settings = new JsonSerializerSettings
                            {
                                Error = (sender, args) =>
                                {
                                    success = false;
                                    args.ErrorContext.Handled = true;
                                },
                                MissingMemberHandling = MissingMemberHandling.Error
                            };

                            // validate that object could be deserializabled if not check again (json string etc.)
                            var str = payloadToken.ToString();
                            var payload = JsonConvert.DeserializeObject<T>(str, settings);
                            return success ? matcher(payload) : Check(payloadToken.ToObject<T>());
                        }
                    case JObject payloadJObject:
                        {
                            var payload = payloadJObject.ToObject<T>();
                            return matcher(payload);
                        }
                    case object raw:
                        try
                        {
                            var cast = Convert.ChangeType(req, typeof(T));
                            return matcher((T)cast);
                        }
                        catch (InvalidCastException ex)
                        {
                            return false;
                        }
                }

                return false;
            }

            return req => Check(req.body);
        }

        public static TestSocketMessageMatcher WithRoutePrefix(string prefix)
        {
            return req => req?.path?.StartsWith(prefix) ?? false;
        }

        public static TestSocketMessageMatcher WithMethod(Method method)
        {
            return req => method.ToString().ToLower().Equals(req.method?.ToLower());
        }

        public static TestSocketMessageMatcher WithFrom(long from)
        {
            return req => req.from == from;
        }

        public static TestSocketMessageMatcher WithPost()
        {
            return WithMethod(Method.POST);
        }
        public static TestSocketMessageMatcher WithGet()
        {
            return WithMethod(Method.GET);
        }

        public static TestSocketMessageMatcher WithDelete()
        {
            return WithMethod(Method.DELETE);
        }

        public static TestSocketMessageMatcher WithRouteContains(string contains)
        {
            return req =>
            {
                var pass = req?.path?.Contains(contains) ?? false;
                return pass;
            };
        }

        public static TestSocketMessageMatcher WithReqId(int id)
        {
            return req => req?.id == id;
        }

        public static TestSocketMessageMatcher WithPositiveReqId()
        {
            return req => req?.id > 0;
        }

        public static TestSocketMessageMatcher WithStatus(int status)
        {
            return req => req?.status == status;
        }

        private MessageMatcher()
        {
        }
    }

    public class MessageResponder
    {
        public static TestSocketResponseGenerator Success<T>(T body)
        {
            return req => req.Succeed(body);
        }

        public static TestSocketResponseGenerator Success<T>(Func<T> generator)
        {
	        return req => req.Succeed(generator());
        }

        public static TestSocketResponseGenerator AuthFailure()
        {
            return req => req.AuthFailure();
        }

        public static TestSocketResponseGenerator Custom(TestSocketResponseGenerator generator)
        {
	        return generator;
        }

        public static TestSocketResponseGeneratorAsync SuccessWithDelay<T>(int ms, Func<T> bodyGenerator)
        {
	        return async res =>
	        {
		        await Task.Delay(ms);
		        return res.Succeed(bodyGenerator());
	        };
        }

        public static TestSocketResponseGeneratorAsync SuccessWithDelay<T>(int ms, T body) =>
	        SuccessWithDelay(ms, () => body);

        public static TestSocketResponseGeneratorAsync SuccessAfterCondition<T>(Func<bool> condition, T body)
        {
            return async res =>
            {
                while (!condition())
                {
                    await Task.Delay(1);
                }
                return res.Succeed(body);
            };
        }

        public static TestSocketResponseGeneratorAsync CustomAsync(TestSocketResponseGeneratorAsync response)
        {
	        return response;
        }

        public static TestSocketResponseGenerator NoResponse()
        {
            return req => null;
        }
    }


    public delegate bool TestSocketMessageMatcher(WebsocketResponse request);

    public delegate Task<object> TestSocketResponseGeneratorAsync(WebsocketResponse request);
    public delegate object TestSocketResponseGenerator(WebsocketResponse request);


    public static class MessageFrequency
    {
        public static MessageFrequencyRequirements Exactly(int n)
        {
            return new MessageFrequencyRequirements
            {
                Min = n,
                Max = n + 1
            };
        }
        public static MessageFrequencyRequirements OnlyOnce()
        {
            return new MessageFrequencyRequirements
            {
                Min = 1,
                Max = 2
            };
        }
        public static MessageFrequencyRequirements AtLeast(int min)
        {
            return new MessageFrequencyRequirements
            {
                Min = min
            };
        }

        public static MessageFrequencyRequirements Between(int min, int max)
        {
	        return new MessageFrequencyRequirements
	        {
		        Min = min,
		        Max = max
	        };
        }
    }

    public class MessageFrequencyRequirements
    {
        public int? Min, Max;

        private long _callCount;
        public long CallCount => _callCount;

        public bool AboveMin => !Min.HasValue || CallCount >= Min;
        public bool BelowMax => !Max.HasValue || CallCount < Max;

        public bool ValidCount => AboveMin && BelowMax;

        public bool Call()
        {
            Interlocked.Increment(ref _callCount);
            if (Max.HasValue && CallCount >= Max)
            {
                return false;
            }

            return true;
        }

        public bool CanCall()
        {
            var c = Interlocked.Read(ref _callCount);
            if ((c + 1) >= Max)
            {
                return false;
            }

            return true;
        }
    }

    public class MockTestRequestHandler
    {
	    /// <summary>
	    /// An optional, hopefully helpful, string that describes the request.
	    /// </summary>
	    public string Description;

	    /// <summary>
	    /// An optional, but usually true, value that indicates if the socket must be authorized for this request to match.
	    /// </summary>
	    public bool? RequiresAuthorization = true;
        public Expression<TestSocketMessageMatcher> Matcher;
        public TestSocketResponseGeneratorAsync Responder;
        public MessageFrequencyRequirements Frequency = new MessageFrequencyRequirements();
    }

    public class WebsocketResponse
    {
        public long id;
        public int status;
        public string method;
        public string path;
        public object body;
        public long? from;
    }

    public static class ClientRequest
    {
        public static WebsocketRequest Event<T>(string path, int reqId, T body)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"event/{path}",
                body = body,
                method = "post"
            };
        }
        public static WebsocketRequest ClientCallable(string serviceName, string methodName, int reqId, int dbid, params object[] args)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"{serviceName}/{methodName}",
                body = new
                {
                    payload = args
                },
                from = dbid,
                method = "post"
            };
        }

        public static WebsocketRequest ClientCallableAsAdmin(string serviceName, string methodName, int reqId, int dbid, params object[] args)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"{serviceName}/{methodName}",
                body = new
                {
                    payload = args
                },
                from = dbid,
                method = "post",
                scopes = new[]{"*"}
            };
        }

        public static WebsocketRequest ClientCallablePayloadArgs(string serviceName, string methodName, int reqId, int dbid, string payloadArgs)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"{serviceName}/{methodName}",
                body = new
                {
                    payload = payloadArgs
                },
                from = dbid,
                method = "post"
            };
        }

        public static WebsocketRequest ClientCallableNamed(string serviceName, string methodName, int reqId, int dbid, object args)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"{serviceName}/{methodName}",
                body = args,
                from = dbid,
                method = "post"
            };
        }
        public static WebsocketRequest ClientCallableWithScopes(string serviceName, string methodName, int reqId, int dbid, string[] scopes, params object[] args)
        {
            return new WebsocketRequest
            {
                id = reqId,
                path = $"{serviceName}/{methodName}",
                body = new
                {
                    payload = args
                },
                from = dbid,
                method = "post",
                scopes = scopes
            };
        }
    }

    public class WebsocketClientResponse
    {
        public object payload;
    }

    public static class WebsocketRequestExtensions
    {
        public static WebsocketResponse Succeed<T>(this WebsocketResponse req, T body)
        {
            return new WebsocketResponse
            {
                id = req.id,
                body = body,
                from = 0,
                status = 200
            };
        }

        public static WebsocketResponse AuthFailure(this WebsocketResponse res)
        {
            return new WebsocketResponse
            {
                id = res.id,
                from = 0,
                status = 403,
                body = new WebsocketErrorResponse
                {
                    service = "gateway"
                }
            };
        }
    }

    public class NoHandlerException : Exception
    {
        private readonly string _messageJson;
        private readonly WebsocketResponse _request;

        public NoHandlerException(string messageJson, WebsocketResponse request) : base($"No handler set up. id={request.id} method={request.method} path={request.path} message={messageJson.Replace("{", "  ").Replace("}", "  ")}")
        {
            _messageJson = messageJson;
            _request = request;
        }

    }

    public class ExhaustedHandlerException : Exception
    {
        private readonly string _messageJson;
        private readonly WebsocketResponse _request;

        public ExhaustedHandlerException(string messageJson, WebsocketResponse request) : base($"A handler exists, but it is exhausted. id={request.id} method={request.method} path={request.path} message={messageJson.Replace("{", "  ").Replace("}", "  ")}")
        {
            _messageJson = messageJson;
            _request = request;
        }

    }

    public class TestSocket : IConnection
    {
        private event Action<TestSocket> _onConnectionCallbacks;
        private Action<IConnection, string, long> _onMessageCallbacks = (s, m, id) => { };

        public Action MockConnect;
        public Action<Action<TestSocket>> MockOnConnect;
        public event Action<TestSocket, bool> _onDisconnectionCallbacks;
        public Action<Action<IConnection, string, long>> MockOnMessage;
        // public Action<string> MockSendMessage;

        /// <summary>
        /// A mock implementation of an authenticated socket.
        /// Defaults to false, and will be true when the /auth callback returns.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        public string Name;

        private long id;
        private List<MockTestRequestHandler> _handlers = new List<MockTestRequestHandler>();

        public WebSocketState State => WebSocketState.Open;
        private bool _failOnException = true;
        private Exception _failureEx;

        public TestSocket()
        {
            MockConnect = () =>
            {
                _onConnectionCallbacks?.Invoke(this);
            };
            MockOnConnect = (cb) =>
            {
                _onConnectionCallbacks += cb;
            };
            MockOnMessage = (cb) =>
            {
                _onMessageCallbacks += cb;
            };
            // MockSendMessage = async (msg) =>
            // {
            //    Console.WriteLine("Websocket received message: " + msg);
            //    await HandleRequestWithResponders(msg); // TODO THIS ISN"T CATCHING THE EXCEPTION ANYMORE
            // };
        }

        private void Fail(Exception ex)
        {
            if (_failOnException)
            {
                _failureEx = ex;

                // Assert.Fail(ex.Message + " \n " + ex.StackTrace);
            }
            else
            {
                throw ex;
            }
        }

        public async Task HandleRequestWithResponders(string message)
        {
            var req = JsonConvert.DeserializeObject<WebsocketResponse>(message);
            var handler = _handlers.FirstOrDefault(h =>
            {
	            var func = h.Matcher.Compile();
	            var matches = func(req) && h.Frequency.CanCall();
	            return matches;
            });

            if (handler == null)
            {
	            Log.Verbose("Test socket found no handler for " + message);
                Fail(new NoHandlerException(message, req));
                return;
            }
            if (!handler.Frequency.Call())
            {
                Fail(new ExhaustedHandlerException(message, req));
                return;
                //throw new Exception("No handler available for test socket. There was a mock, but it has been exhausted. " + message);
            }
            try
            {
	            if ((handler.RequiresAuthorization ?? false) && !IsAuthenticated)
	            {
		            Fail(new Exception("found a handler, but it required authorization, and the socket isn't."));
		            return;
	            }
                var res = await handler.Responder(req);

                if (res == null) return;
                var responseJson = JsonConvert.SerializeObject(res);
                SendToClient(responseJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILURE AT RESPONDER" + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }

        public void AddMessageHandler(MockTestRequestHandler responder)
        {
            _handlers.Add(responder);
        }

        public TestSocket WithName(string name)
        {
            Name = name;
            return this;
        }


        public TestSocket AddStandardMessageHandlers(int requestIdOffset = 0, bool addShutdownResponder = true)
        {
            return AddAuthMessageHandlers(requestIdOffset)
               .AddProviderMessageHandlers(requestIdOffset, addShutdownResponder);
        }

        public TestSocket AddInitialContentMessageHandler(int reqId = 6, params ContentReference[] references)
        {
            return AddMessageHandler(
               MessageMatcher
                  .WithRouteContains("basic/content/manifest")
                  .WithReqId(reqId)
                  .WithGet(),
               MessageResponder.Success(new ContentManifest
               {
                   id = "global",
                   created = 1,
                   references = references.ToList()
               }),
               MessageFrequency.OnlyOnce()
            );
        }

        public TestSocket SetAuthentication(bool isAuthenticated)
        {
	        IsAuthenticated = isAuthenticated;
	        return this;
        }

        public TestSocket AddInitialContentMessageHandler(Func<long, bool> reqIdMatcher, params ContentReference[] references)
        {
            return AddMessageHandler(
               MessageMatcher
                  .WithRouteContains("basic/content/manifest")
                  .And(req => reqIdMatcher(req.id))
                  .WithGet(),
               MessageResponder.Success(new ContentManifest
               {
                   id = "global",
                   created = 1,
                   references = references.ToList()
               }),
               MessageFrequency.OnlyOnce()
            );
        }


        public TestSocket AddAuthMessageHandlers(int requestIdOffset = 0)
        {
	        return AddMessageHandler(
			        MessageMatcher.WithRouteContains("nonce").WithReqId(-1 - requestIdOffset),
			        MessageResponder.Success(new MicroserviceNonceResponse { nonce = "testnonce" }),
			        MessageFrequency.OnlyOnce(),
			        "nonce", false
		        )
		        .AddMessageHandler(
			        MessageMatcher.WithRouteContains("auth").WithReqId(-2 - requestIdOffset),
			        MessageResponder.Success(() =>
			        {
				        IsAuthenticated = true;
				        return new MicroserviceAuthResponse { result = "ok" };
			        }),
			        MessageFrequency.OnlyOnce(),
			        "auth", false
		        );
        }

        public TestSocket AddAuthMessageHandlersWithDelay(int nonceDelay, int authDelay, int requestIdOffset = 0, int authRequestIdOffset= 0)
        {
            return AddMessageHandler(
                  MessageMatcher.WithRouteContains("nonce").WithReqId(-1 - requestIdOffset),
                  MessageResponder.SuccessWithDelay(nonceDelay, new MicroserviceNonceResponse { nonce = "testnonce" }),
                  MessageFrequency.OnlyOnce(),
                  "nonce",
                  false
               )
               .AddMessageHandler(
                  MessageMatcher.WithRouteContains("auth").WithReqId(-2 - requestIdOffset - authRequestIdOffset),
                  MessageResponder.SuccessWithDelay(authDelay, () =>
                  {
	                  IsAuthenticated = true;
	                  return new MicroserviceAuthResponse { result = "ok" };
                  }),
                  MessageFrequency.OnlyOnce(),
                  "aut",
                  false
               );
        }

        public TestSocket AddProviderMessageHandlers(int requestIdOffset = 0, bool addShutdownResponder = true)
        {
            var socket = AddMessageHandler(
                  MessageMatcher
                     .WithRouteContains("gateway/provider")
                     .WithReqId(-3 - requestIdOffset)
                     .WithPost()
                     .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                  MessageResponder.Success(new MicroserviceProviderResponse()),
                  MessageFrequency.OnlyOnce()
               )
               .AddMessageHandler(
                  MessageMatcher
                     .WithRouteContains("gateway/provider")
                     .WithReqId(-4 - requestIdOffset)
                     .WithPost()
                     .WithBody<MicroserviceProviderRequest>(body => body.type == "event"),
                  MessageResponder.Success(new MicroserviceProviderResponse()),
                  MessageFrequency.OnlyOnce()
               );
            if (addShutdownResponder)
            {
                socket = socket.AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithDelete()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.Success(new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                );
            }

            return socket;

        }

        public TestSocket AddMessageHandler(TestSocketMessageMatcher matcher, TestSocketResponseGenerator responder, MessageFrequencyRequirements frequencyRequirements = null, string desc=null, bool? requiresAuth=true)
        {
	        Task<object> Example(WebsocketResponse req)
            {
                return Task.FromResult(responder(req));
            }

            AddMessageHandler(matcher, (req) =>
            {
                var t = Example(req);
                return t;
            }, frequencyRequirements, desc, requiresAuth);
            return this;
        }
        public TestSocket AddMessageHandler(TestSocketMessageMatcher matcher, TestSocketResponseGeneratorAsync responder, MessageFrequencyRequirements frequencyRequirements = null, string desc=null, bool? requiresAuth=true)
        {
            AddMessageHandler(new MockTestRequestHandler
            {
                Matcher = (req) => matcher(req),
                Responder = responder,
                Frequency = frequencyRequirements ?? new MessageFrequencyRequirements(),
                Description = desc,
                RequiresAuthorization = requiresAuth
            });
            return this;
        }


        public void SendToClient(string msg)
        {
            var next = Interlocked.Increment(ref id);
            _onMessageCallbacks(this, msg, next);
        }

        public void SendToClient<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            SendToClient(json);
        }

        public virtual IConnection Connect()
        {
            MockConnect?.Invoke();
            return this;
        }

        public Task Close()
        {
            return Task.CompletedTask;
        }

        public void RemoteClose()
        {
            _onDisconnectionCallbacks?.Invoke(this, true);
        }

        public void Fault()
        {
	        MockIsConnectionOpen = false;
            _onDisconnectionCallbacks?.Invoke(this, false);
        }

        public bool MockIsConnectionOpen = true;
        public async Task SendMessage(string message)
        {
            if (!MockIsConnectionOpen)
            {
                throw new Exception("Connection is not open");
            }
            await HandleRequestWithResponders(message);
        }

        public IConnection OnConnect(Action<IConnection> onConnect)
        {
            MockOnConnect?.Invoke(onConnect);
            return this;
        }

        public IConnection OnDisconnect(Action<IConnection, bool> onDisconnect)
        {
            _onDisconnectionCallbacks += onDisconnect;
            return this;
        }

        public IConnection OnMessage(Action<IConnection, string, long> onMessage)
        {
            MockOnMessage?.Invoke(onMessage);
            return this;
        }

        public bool AllMocksCalled()
        {
            Assert.IsNull(UnhandledError());
            return _handlers.All(h => h.Frequency.ValidCount);
        }

        public IEnumerable<MockTestRequestHandler> UnfinishedMocks()
        {
            return _handlers.Where(h => !h.Frequency.ValidCount);
        }

        public Exception UnhandledError() => _failureEx;
    }
}
