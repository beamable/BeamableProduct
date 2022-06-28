
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using Beamable.Common.Api.Tournaments;

namespace Beamable.Server.Api.Tournament
{
	public class MicroserviceTournamentApi : TournamentApi, IMicroserviceTournamentApi
	{
		public MicroserviceTournamentApi(IStatsApi stats, IBeamableRequester requester, IUserContext ctx) : base(stats, requester, ctx)
		{
		}
	}
}
