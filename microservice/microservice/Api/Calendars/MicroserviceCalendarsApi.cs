using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Experimental.Common.Api.Calendars;

namespace Beamable.Server.Api.Calendars
{
	public class MicroserviceCalendarsApi : AbsCalendarApi, IMicroserviceCalendarsApi
	{
		private BeamableGetApiResource<CalendarView> _getter;
		public MicroserviceCalendarsApi(IBeamableRequester requester, IUserContext ctx) : base(requester, ctx)
		{
			_getter = new BeamableGetApiResource<CalendarView>();
		}

		public override Promise<CalendarView> GetCurrent(string scope = "")
		{
			return _getter.RequestData(Requester, Ctx, SERVICE_NAME, scope);
		}
	}
}
