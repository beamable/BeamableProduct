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
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/game-economy/inventory-overview/">Inventory</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IMicroserviceInventoryApi : IInventoryApi
	{
		/// <summary>
		/// Send one or more currencies to different user. The currencies will
		/// be withdrawn from the current player and deposited to the recipient
		/// player. If the donor player does not have enough of any of the
		/// specified currencies, the whole transaction will be terminated with
		/// no effect.
		/// The recipient player must be a different player from the donor (the
		/// current player according to the Beamable context); trying to transfer
		/// currencies to the same player who is donating is an error.
		/// Transaction IDs are normally assigned by Beamable services internally;
		/// omit the transaction argument unless you have a need to manage the
		/// transaction IDs externally.
		/// </summary>
		/// <param name="currencies">A dictionary where the keys are content IDs of the currency, and the values are the amount of currency to send</param>
		/// <param name="recipientPlayer">Target user identifier.</param>
		/// <param name="transaction">(optional) An inventory transaction ID. In the vast majority of cases you should leave this argument empty.</param>
		/// <returns>A <see cref="Promise{T}"/> representing the network call.</returns>
		Promise SendCurrency(Dictionary<string, long> currencies, long recipientPlayer, string transaction = null);
	}
}
