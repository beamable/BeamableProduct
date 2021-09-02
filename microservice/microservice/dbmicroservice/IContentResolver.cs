using System.Net.Http;
using System.Threading.Tasks;
using Beamable.Common;

namespace Beamable.Server
{
   public interface IContentResolver
   {
      Task<string> RequestContent(string uri);
   }

   public class DefaultContentResolver : IContentResolver
   {
      public async Task<string> RequestContent(string uri)
      {
         using var client = new HttpClient();
         var result = await client.GetStringAsync(uri);
         return result;
      }
   }
}