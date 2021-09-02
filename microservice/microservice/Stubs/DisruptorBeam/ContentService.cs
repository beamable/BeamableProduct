using Beamable.Server;

namespace Beamable.Content
{
   public class ContentService : Beamable.Server.Content.ContentService
   {
      public ContentService(MicroserviceRequester requester, SocketRequesterContext socket, IContentResolver resolver) : base(requester, socket, resolver)
      {
      }
   }
}