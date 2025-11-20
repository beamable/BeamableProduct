using Beamable.Common.Content;
using Beamable.Content;

namespace Beamable.Server.Content
{
   public class MicroserviceContentSerializer : ContentSerializer<IContentObject>
   {
      protected override TContent CreateInstance<TContent>()
      {
         return new TContent();
      }
   }
}