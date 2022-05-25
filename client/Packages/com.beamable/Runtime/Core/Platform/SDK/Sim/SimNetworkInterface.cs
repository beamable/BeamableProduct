using System.Collections.Generic;

namespace Beamable.Experimental.Api.Sim
{
	/// <summary>
	/// This type defines the %SimNetworkInterface for the %Multiplayer feature.
	///
	/// This guarantees that a given frame will only be sent to peers a maximum
	/// of 1 time and that a given frame will be surfaced to the SimClient a maximum
	/// of 1 time
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Experimental.Api.Sim.SimClient script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface SimNetworkInterface
	{

		/// <summary>
		/// Get a unique id for the client which is consistent across the network.
		/// Often, this will be the gamertag of the client's player.
		/// </summary>
		string ClientId { get; }

		/// <summary>
		/// Is the network ready to operate?
		/// </summary>
		bool Ready { get; }

		/// <summary>
		/// Synchronize the network interface and receive any fully realized frames by the network
		/// </summary>
		/// <param name="curFrame"></param>
		/// <param name="maxFrame"></param>
		/// <param name="expectedMaxFrame"></param>
		/// <returns></returns>
		List<SimFrame> Tick(long curFrame, long maxFrame, long expectedMaxFrame);

		/// <summary>
		/// Push (or queue) an event onto the network
		/// </summary>
		/// <param name="evt">A <see cref="SimEvent"/></param>
		void SendEvent(SimEvent evt);

	}
}
