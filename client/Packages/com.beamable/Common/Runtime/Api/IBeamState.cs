namespace Beamable.Common.Api
{
	public interface IBeamState
	{
		Promise<Unit> OnPlayerReady { get; }
	}
}
