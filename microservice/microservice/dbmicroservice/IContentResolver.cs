using System.Net.Http;
using System.Threading.Tasks;
using Beamable.Common;
using System;
using System.Net;

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
		   client = new HttpClient();
	   }

	   public Task<string> RequestContent(string uri)
	   {
		   return client.GetStringAsync(uri);
	   }
   }
}
