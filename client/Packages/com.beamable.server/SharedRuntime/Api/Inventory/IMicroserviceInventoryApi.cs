using Beamable.Common;
using Beamable.Common.Api.Inventory;
using System.Collections.Generic;

namespace Beamable.Server.Api.Inventory
{
	/// <summary>
	/// This type defines the %Microservices main entry point for the %Inventory feature.
	/// 
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// 
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature-overview">Inventory</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IMicroserviceInventoryApi : IInventoryApi
	{
		/// <summary>
		/// Send multiple currencies to different user.
		/// </summary>
		/// <param name="currencies">A dictionary where the keys are content IDs of the currency, and the values are the amount of currency to send</param>
		/// <param name="recipientPlayer">Target user identifier.</param>
		/// <param name="transaction">An inventory transaction ID. Leave this argument empty.</param>
		/// <returns>A <see cref="Promise{T}"/> representing the network call.</returns>
		Promise SendCurrency(Dictionary<string, long> currencies, long recipientPlayer, string transaction = null);
	}
}
