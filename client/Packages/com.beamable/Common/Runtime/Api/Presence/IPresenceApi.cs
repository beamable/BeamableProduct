namespace Beamable.Common.Api.Presence
{
	public interface IPresenceApi
	{
		Promise<EmptyResponse> SendHeartbeat();
	}
}
