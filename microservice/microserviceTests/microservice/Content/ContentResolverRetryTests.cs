using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.Content;

[TestFixture]
public class ContentResolverRetryTests
{
   [Test]
   public async Task ServiceUnavailableThenSuccessRetries()
   {
      var handler = new QueueHttpMessageHandler(
         Response(HttpStatusCode.ServiceUnavailable),
         Response(HttpStatusCode.OK, "content"));
      var resolver = CreateResolver(handler);

      var result = await resolver.RequestContent("https://content.beamable.com/content.json");

      Assert.That(result, Is.EqualTo("content"));
      Assert.That(handler.CallCount, Is.EqualTo(2));
   }

   [TestCase(HttpStatusCode.RequestTimeout)]
   [TestCase(HttpStatusCode.TooManyRequests)]
   [TestCase(HttpStatusCode.InternalServerError)]
   [TestCase(HttpStatusCode.BadGateway)]
   [TestCase(HttpStatusCode.GatewayTimeout)]
   public async Task OtherTransientStatusThenSuccessRetries(HttpStatusCode statusCode)
   {
      var handler = new QueueHttpMessageHandler(
         Response(statusCode),
         Response(HttpStatusCode.OK, "content"));
      var resolver = CreateResolver(handler);

      var result = await resolver.RequestContent("https://content.beamable.com/content.json");

      Assert.That(result, Is.EqualTo("content"));
      Assert.That(handler.CallCount, Is.EqualTo(2));
   }

   [Test]
   public void RepeatedServiceUnavailableFailsAfterThreeAttemptsWithContext()
   {
      var uri = "https://content.beamable.com/content.json";
      var handler = new QueueHttpMessageHandler(
         Response(HttpStatusCode.ServiceUnavailable),
         Response(HttpStatusCode.ServiceUnavailable),
         Response(HttpStatusCode.ServiceUnavailable));
      var resolver = CreateResolver(handler);

      var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await resolver.RequestContent(uri));

      Assert.That(handler.CallCount, Is.EqualTo(3));
      Assert.That(exception!.Message, Does.Contain(uri));
      Assert.That(exception.Message, Does.Contain("503"));
      Assert.That(exception.Message, Does.Contain("3"));
   }

   [Test]
   public void NotFoundDoesNotRetry()
   {
      var handler = new QueueHttpMessageHandler(Response(HttpStatusCode.NotFound));
      var resolver = CreateResolver(handler);

      var exception = Assert.ThrowsAsync<HttpRequestException>(
         async () => await resolver.RequestContent("https://content.beamable.com/missing.json"));

      Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
      Assert.That(handler.CallCount, Is.EqualTo(1));
   }

   [Test]
   public async Task TransportFailureThenSuccessRetries()
   {
      var handler = new QueueHttpMessageHandler(
         Exception(new HttpRequestException("connection reset")),
         Response(HttpStatusCode.OK, "content"));
      var resolver = CreateResolver(handler);

      var result = await resolver.RequestContent("https://content.beamable.com/content.json");

      Assert.That(result, Is.EqualTo("content"));
      Assert.That(handler.CallCount, Is.EqualTo(2));
   }

   [Test]
   public async Task RetryAfterDelayIsCapped()
   {
      var retryResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
      retryResponse.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
      var handler = new QueueHttpMessageHandler(
         () => Task.FromResult(retryResponse),
         Response(HttpStatusCode.OK, "content"));
      var resolver = CreateResolver(handler);
      var stopwatch = Stopwatch.StartNew();

      var result = await resolver.RequestContent("https://content.beamable.com/content.json");

      stopwatch.Stop();
      Assert.That(result, Is.EqualTo("content"));
      Assert.That(handler.CallCount, Is.EqualTo(2));
      Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
      Assert.That(stopwatch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(1.8)));
   }

   private static DefaultContentResolver CreateResolver(HttpMessageHandler handler)
   {
      return new DefaultContentResolver(new TestArgs(), new HttpClient(handler));
   }

   private static Func<Task<HttpResponseMessage>> Response(HttpStatusCode statusCode, string content = "")
   {
      return () => Task.FromResult(new HttpResponseMessage(statusCode)
      {
         Content = new StringContent(content)
      });
   }

   private static Func<Task<HttpResponseMessage>> Exception(Exception exception)
   {
      return () => Task.FromException<HttpResponseMessage>(exception);
   }

   private sealed class QueueHttpMessageHandler : HttpMessageHandler
   {
      private readonly Queue<Func<Task<HttpResponseMessage>>> _responses;

      public QueueHttpMessageHandler(params Func<Task<HttpResponseMessage>>[] responses)
      {
         _responses = new Queue<Func<Task<HttpResponseMessage>>>(responses);
      }

      public int CallCount { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken)
      {
         CallCount++;
         return _responses.Dequeue()();
      }
   }
}
