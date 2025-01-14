namespace Beamable.Common
{
	public interface IFederatedPlayerInit<in T> : IFederation where T : IFederationId, new()
	{
		Promise CreatePlayer(); // TODO: schemas? 
	}
}
