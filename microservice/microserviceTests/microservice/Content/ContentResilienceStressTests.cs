using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Reflection;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Content;
using microserviceTests.microservice.Util;
using NUnit.Framework;

namespace microserviceTests.microservice.Content;

[TestFixture]
public class ContentResilienceStressTests
{
   private const int ConcurrentCallCount = 100;

   private ReflectionCache _cache;

   [SetUp]
   public void Setup()
   {
      _cache = new ReflectionCache();
      var contentTypeCache = new ContentTypeReflectionCache();
      _cache.RegisterTypeProvider(contentTypeCache);
      _cache.RegisterReflectionSystem(contentTypeCache);

      var asms = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name).ToList();
      _cache.GenerateReflectionCache(asms);

      LoggingUtil.InitTestCorrelator();
   }

   [Test]
   public async Task ConcurrentContentRequestsShareBoundedManifestRetryAndResolvedContentCache()
   {
      var manifestAttempts = 0;
      var contentResolverCalls = 0;
      var contentResolver = new TestContentResolver(async _ =>
      {
         Interlocked.Increment(ref contentResolverCalls);
         await Task.Delay(50);
         return SerializeItemContent("foo");
      });

      using var harness = CreateContentService(
         contentResolver,
         req =>
         {
            var attempt = Interlocked.Increment(ref manifestAttempts);
            if (attempt <= 2)
            {
               return TransientManifestFailure(req, HttpStatusCode.ServiceUnavailable);
            }

            return ManifestSuccess(req);
         },
         MessageFrequency.Exactly(3));

      var requests = Enumerable
         .Range(0, ConcurrentCallCount)
         .Select(_ => Task.Run(async () => await harness.ContentService.GetContent("items.foo")))
         .ToArray();

      var results = await Task.WhenAll(requests);

      Assert.That(results.Select(result => result.Id), Is.All.EqualTo("items.foo"));
      Assert.That(manifestAttempts, Is.EqualTo(3), "all callers should share one bounded manifest retry group");
      Assert.That(contentResolverCalls, Is.EqualTo(1), "all callers should share one resolved content cache task");
      Assert.That(harness.TestSocket.AllMocksCalled(), Is.True);
   }

   [Test]
   public async Task ConcurrentContentRequestsShareOneBoundedResolvedContentDownloadRetry()
   {
      var content = SerializeItemContent("foo");
      var handler = new CountingHttpMessageHandler(async attempt =>
      {
         await Task.Delay(25);
         return attempt <= 2
            ? Response(HttpStatusCode.ServiceUnavailable)
            : Response(HttpStatusCode.OK, content);
      });
      var contentResolver = new DefaultContentResolver(new TestArgs(), new HttpClient(handler));

      using var harness = CreateContentService(
         contentResolver,
         ManifestSuccess,
         MessageFrequency.Exactly(1));

      var requests = Enumerable
         .Range(0, ConcurrentCallCount)
         .Select(_ => Task.Run(async () => await harness.ContentService.GetContent("items.foo")))
         .ToArray();

      var results = await Task.WhenAll(requests);

      Assert.That(results.Select(result => result.Id), Is.All.EqualTo("items.foo"));
      Assert.That(handler.CallCount, Is.EqualTo(3), "resolved content should be downloaded by one shared cache task with bounded HTTP retry");
      Assert.That(harness.TestSocket.AllMocksCalled(), Is.True);
   }

   private ContentServiceHarness CreateContentService(
      IContentResolver contentResolver,
      TestSocketResponseGenerator manifestResponder,
      MessageFrequencyRequirements manifestFrequency)
   {
      var args = new TestArgs();
      var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
      TestSocket testSocket = null;
      var socketProvider = new TestSocketProvider(socket =>
      {
         testSocket = socket;
         socket.AddMessageHandler(
            MessageMatcher
               .WithRouteContains("basic/content/manifest")
               .WithGet(),
            manifestResponder,
            manifestFrequency);
         socket.SetAuthentication(true);
      });

      var socket = socketProvider.Create("test", args);
      var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
      var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false);
      (_, socketCtx.Daemon) =
         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

      var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

      testSocket.Connect();
      testSocket.OnMessage((_, data, id) =>
      {
         data.TryBuildRequestContext(args, out var rc);
         socketCtx.HandleMessage(rc);
      });

      return new ContentServiceHarness(contentService, testSocket, socketCtx);
   }

   private static WebsocketResponse ManifestSuccess(WebsocketResponse req)
   {
      return req.Succeed(new ContentManifest
      {
         id = "global",
         created = 1,
         references = new List<ContentReference>
         {
            new ContentReference
            {
               id = "items.foo",
               version = "123",
               uri = "https://content.beamable.com/items.foo.json",
               visibility = "public"
            }
         }
      });
   }

   private static WebsocketResponse TransientManifestFailure(WebsocketResponse req, HttpStatusCode statusCode)
   {
      return new WebsocketResponse
      {
         id = req.id,
         from = 0,
         status = (int)statusCode,
         body = new WebsocketErrorResponse
         {
            status = (int)statusCode,
            service = "content",
            error = statusCode.ToString(),
            message = ((int)statusCode).ToString()
         }
      };
   }

   private static string SerializeItemContent(string name)
   {
      var content = new ItemContent();
      content.SetContentName(name);
      return new MicroserviceContentSerializer().Serialize(content);
   }

   private static HttpResponseMessage Response(HttpStatusCode statusCode, string content = "")
   {
      return new HttpResponseMessage(statusCode)
      {
         Content = new StringContent(content)
      };
   }

   private sealed class CountingHttpMessageHandler : HttpMessageHandler
   {
      private readonly Func<int, Task<HttpResponseMessage>> _responseFactory;
      private int _callCount;

      public CountingHttpMessageHandler(Func<int, Task<HttpResponseMessage>> responseFactory)
      {
         _responseFactory = responseFactory;
      }

      public int CallCount => Volatile.Read(ref _callCount);

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken)
      {
         var attempt = Interlocked.Increment(ref _callCount);
         return _responseFactory(attempt);
      }
   }

   private sealed class ContentServiceHarness : IDisposable
   {
      public ContentServiceHarness(
         ContentService contentService,
         TestSocket testSocket,
         SocketRequesterContext socketContext)
      {
         ContentService = contentService;
         TestSocket = testSocket;
         SocketContext = socketContext;
      }

      public ContentService ContentService { get; }
      public TestSocket TestSocket { get; }
      private SocketRequesterContext SocketContext { get; }

      public void Dispose()
      {
         SocketContext.Daemon.KillAuthThread();
      }
   }
}
