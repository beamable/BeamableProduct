namespace Beamable.Api.Commerce
{
	public interface ICommerceConfig
	{
		/// <summary>
		/// The <see cref="CommerceService"/> will use the <see cref="Beamable.Api.Payments.PlayerStoreView.nextDeltaSeconds"/> value
		/// to automatically refresh the store content.
		/// <para>
		/// However, the value of the nextDeltaSeconds may be too small, and result in overly chatty networking.
		/// To prevent excess networking, the <see cref="CommerceListingRefreshSecondsMin"/> value is used as a
		/// minimum number of seconds to wait before automatically refreshing the store.
		/// </para>
		/// <para>
		/// When this value is 0, there is effectively no minimum wait period.
		/// </para>
		/// </summary>
		int CommerceListingRefreshSecondsMin { get; }
	}
}
