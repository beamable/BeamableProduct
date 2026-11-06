using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Beamable.Server
{
   public interface IContentResolver
   {
      Task<string> RequestContent(string uri);
   }

   public class DefaultContentResolver : IContentResolver
   {
	   private const string BeamableSuffix = ".beamable.com";
	   private const int MaxAttempts = 3;
	   private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(2);

	   private readonly HttpClient _client;
	   private readonly string _suffixFilter;

	   public DefaultContentResolver(IMicroserviceArgs args)
		   : this(args, null)
	   {
	   }

	   internal DefaultContentResolver(IMicroserviceArgs args, HttpClient httpClient)
	   {
		   _suffixFilter = GetContentHostSuffix(args?.Host);

		   if (httpClient != null)
		   {
			   _client = httpClient;
			   return;
		   }

		   var handler = new HttpClientHandler
		   {
			   ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback
		   };
		   _client = new HttpClient(handler);
	   }

	   /// <summary>
	   /// The domain suffix that content hosts must share with HOST. A HOST under beamable.com, however
	   /// many labels deep, serves content from *.beamable.com; a private cloud serves it from the parent
	   /// domain of its API host.
	   /// </summary>
	   internal static string GetContentHostSuffix(string host)
	   {
		   if (string.IsNullOrEmpty(host) || host.Contains("localhost"))
		   {
			   return BeamableSuffix;
		   }

		   // HOST is normally a URI (wss://api.beamable.com/socket), but a bare host name, with or
		   // without a port or path, is accepted too.
		   var hostName = Uri.TryCreate(host, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host)
			   ? uri.Host
			   : host.Split('/')[0].Split(':')[0];

		   if (hostName == "beamable.com" || hostName.EndsWith(BeamableSuffix))
		   {
			   return BeamableSuffix;
		   }

		   return "." + string.Join(".", hostName.Split('.').Skip(1));
	   }

	   public bool ServerCertificateCustomValidationCallback(HttpRequestMessage msg, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors)
	   {
		   // This resolver is only valid for content in the stack's own domain; any other host means an
		   // invalid URI was passed in.
		   var host = msg.RequestUri.Host;
		   if (!host.EndsWith(_suffixFilter))
		   {
			   return false;
		   }

		   // Beamable's content CDNs serve publicly trusted certificates, so validate them. Private clouds
		   // may serve content with certificates the container doesn't trust, so their domains still skip
		   // validation.
		   if (host.EndsWith(BeamableSuffix))
		   {
			   return errors == SslPolicyErrors.None;
		   }

		   return true;
	   }

	   /// <summary>
	   /// Downloads immutable content, retrying bounded transient HTTP and transport failures.
	   /// </summary>
	   public async Task<string> RequestContent(string uri)
	   {
		   for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		   {
			   HttpResponseMessage response;
			   try
			   {
				   response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
			   }
			   catch (HttpRequestException ex)
			   {
				   if (attempt == MaxAttempts)
				   {
					   throw CreateFinalException(uri, ex.StatusCode, attempt, ex);
				   }

				   await Task.Delay(GetRetryDelay(null, attempt));
				   continue;
			   }

			   using (response)
			   {
				   if (response.IsSuccessStatusCode)
				   {
					   return await response.Content.ReadAsStringAsync();
				   }

				   if (!IsTransient(response.StatusCode))
				   {
					   response.EnsureSuccessStatusCode();
				   }

				   if (attempt == MaxAttempts)
				   {
					   throw CreateFinalException(uri, response.StatusCode, attempt, null);
				   }

				   await Task.Delay(GetRetryDelay(response.Headers.RetryAfter, attempt));
			   }
		   }

		   throw CreateFinalException(uri, null, MaxAttempts, null);
	   }

	   private static bool IsTransient(HttpStatusCode statusCode)
	   {
		   return statusCode is HttpStatusCode.RequestTimeout 
			   or HttpStatusCode.TooManyRequests 
			   or HttpStatusCode.InternalServerError 
			   or HttpStatusCode.BadGateway 
			   or HttpStatusCode.ServiceUnavailable 
			   or HttpStatusCode.GatewayTimeout;
	   }

	   private static TimeSpan GetRetryDelay(
		   System.Net.Http.Headers.RetryConditionHeaderValue retryAfter,
		   int attempt)
	   {
		   TimeSpan delay;
		   if (retryAfter?.Delta != null)
		   {
			   delay = retryAfter.Delta.Value;
		   }
		   else if (retryAfter?.Date != null)
		   {
			   delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
		   }
		   else
		   {
			   var backoffMilliseconds = 200 * (1 << (attempt - 1));
			   delay = TimeSpan.FromMilliseconds(backoffMilliseconds + Random.Shared.Next(0, 101));
		   }

		   if (delay < TimeSpan.Zero)
		   {
			   return TimeSpan.Zero;
		   }

		   return delay > MaxRetryDelay ? MaxRetryDelay : delay;
	   }

	   private static HttpRequestException CreateFinalException(string uri, HttpStatusCode? statusCode, int attempts, Exception innerException)
	   {
		   var status = statusCode.HasValue
			   ? $"{(int)statusCode.Value} {statusCode.Value}"
			   : "unavailable";
		   return new HttpRequestException(
			   $"Content request failed. uri=[{uri}] status=[{status}] attempts=[{attempts}]",
			   innerException,
			   statusCode);
	   }
   }
}
