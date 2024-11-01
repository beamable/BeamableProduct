using System.Net.Http;
using System.Threading.Tasks;
using Beamable.Common;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Beamable.Server
{
   public interface IContentResolver
   {
      Task<string> RequestContent(string uri);
   }

   public class DefaultContentResolver : IContentResolver
   {
	   private HttpClient client;

	   public DefaultContentResolver()
	   {
		   var handler = new HttpClientHandler();
		   handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
		   client = new HttpClient(handler);
	   }

	   private bool ServerCertificateCustomValidationCallback(HttpRequestMessage msg, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors)
	   {
		   // the URI must start with the Beamable URI, otherwise an invalid URI may have been passed in, and this resolver is only valid for Beamable content.
		   var isBeamableAddr = msg.RequestUri.Host.EndsWith(".beamable.com");
		   return isBeamableAddr;
	   }

	   public Task<string> RequestContent(string uri)
	   {
		   return client.GetStringAsync(uri);
	   }
   }
}
