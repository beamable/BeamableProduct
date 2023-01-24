namespace Beamable.Common.Api.Presence
{
	public class PresenceService : IPresenceApi
	{
		private readonly IRequester _requester;
		private readonly IUserContext _userContext;

		public PresenceService(IRequester requester, IUserContext userContext)
		{
			_requester = requester;
			_userContext = userContext;
		}

		public Promise<EmptyResponse> SendHeartbeat()
		{
			return _requester.BeamableRequest(new SDKRequesterOptions<EmptyResponse>
			{
				method = Method.PUT,
				uri = $"/players/{_userContext.UserId}/presence",
				includeAuthHeader = true,
				useConnectivityPreCheck = false // the magic sauce to allow this to ignore the connectivity
			});
		}
	}
}
