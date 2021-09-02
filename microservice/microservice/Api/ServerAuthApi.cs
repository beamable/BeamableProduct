using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace Beamable.Server.Api
{
   public class ServerAuthApi : AuthApi, IMicroserviceAuthApi
   {
      public const string BASIC_SERVICE = "/basic/accounts";
      public const string OBJECT_SERVICE = "/object/accounts";

      public IBeamableRequester Requester { get; }
      public RequestContext Context { get; }

      public ServerAuthApi(IBeamableRequester requester, RequestContext context) : base(requester)
      {
         Requester = requester;
         Context = context;
      }

      public Promise<User> GetUser(long userId)
      {
         return Requester.Request<User>(Method.GET, $"{BASIC_SERVICE}", new GetUserRequest
         {
            gamerTag = userId
         });
      }

      private class GetUserRequest
      {
         public long gamerTag;
      }
   }
}