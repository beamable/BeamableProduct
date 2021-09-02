using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;

namespace Beamable.Server.Api.Leaderboards
{
   public class MicroserviceLeaderboardApi : LeaderboardApi, IMicroserviceLeaderboardsApi
   {
      public RequestContext Context { get; }

      public MicroserviceLeaderboardApi(IBeamableRequester requester, RequestContext context, UserDataCache<RankEntry>.FactoryFunction factoryFunction)
         : base(requester, context, factoryFunction)
      {
         Context = context;
      }

   }
}