using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
	   private string _suffixFilter;

	   public DefaultContentResolver(IMicroserviceArgs args)
	   {
		   if (string.IsNullOrEmpty(args?.Host) || args.Host.Contains("localhost"))
		   {
			   _suffixFilter = ".beamable.com";
		   } else
		   {
			   var hostLike = args.Host.Replace("dev.api", "dev-api");
			   _suffixFilter = "." + string.Join(".", hostLike
				   .Split(".")
				   .Skip(1));
		   }
		   
		   var handler = new HttpClientHandler();
		   handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
		   client = new HttpClient(handler);
	   }

	   public bool ServerCertificateCustomValidationCallback(HttpRequestMessage msg, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors)
	   {
		   // the URI must start with the Beamable URI, otherwise an invalid URI may have been passed in, and this resolver is only valid for Beamable content.
		   var isBeamableAddr = msg.RequestUri.Host.EndsWith(_suffixFilter);
		   return isBeamableAddr;
	   }

	   public Task<string> RequestContent(string uri)
	   {
		   return client.GetStringAsync(uri);
	   }
   }
}
