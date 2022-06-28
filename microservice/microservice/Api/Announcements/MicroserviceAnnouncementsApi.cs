using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Announcements;

namespace Beamable.Server.Api.Announcements
{
	public class MicroserviceAnnouncementsApi : AbsAnnouncementsApi, IMicroserviceAnnouncementsApi
	{
		private BeamableGetApiResource<AnnouncementQueryResponse> _getter;


		public MicroserviceAnnouncementsApi(IBeamableRequester requester, IUserContext ctx) : base(requester, ctx)
		{
			_getter = new BeamableGetApiResource<AnnouncementQueryResponse>();
		}

		public override Promise<AnnouncementQueryResponse> GetCurrent(string scope = "")
		{
			return _getter.RequestData(Requester, Ctx, "announcements", scope);
		}
	}
}
