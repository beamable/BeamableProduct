namespace Beamable.Common.Api.Presence
{
	public class PresenceService : IPresenceApi
	{
		private readonly IBeamableRequester _requester;
		private readonly IUserContext _userContext;

		public PresenceService(IBeamableRequester requester, IUserContext userContext)
		{
			_requester = requester;
			_userContext = userContext;
		}

		public Promise<EmptyResponse> SendHeartbeat()
		{
			return _requester.Request<EmptyResponse>(
			  Method.PUT,
			  $"/players/{_userContext.UserId}/presence"
			);
		}
	}
}
