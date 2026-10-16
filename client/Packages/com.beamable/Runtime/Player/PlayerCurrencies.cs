using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Player;
using System;
using UnityEngine;

namespace Beamable.Player
{
	/// <summary>Represents a currency's current local amount, content, and properties for a player.</summary>
	/// <remarks>Instances can start as zero-valued placeholders while loading. Await
	/// <see cref="PlayerInventory.LoadCurrencies(Beamable.Common.Inventory.CurrencyRef)"/> before relying on initial amounts.
	/// Observe <see cref="OnAmountUpdated"/> for changes and remove handlers when no longer needed.</remarks>
	[Serializable]
	public class PlayerCurrency : DefaultObservable
	{
		/// <summary>Occurs when a currency update is broadcast, providing the current amount.</summary>
		/// <remarks>Updates can also reflect changes to other currency data. Detach event handlers when their consumer is disposed.</remarks>
		public event Action<long> OnAmountUpdated;

		/// <summary>
		/// The id of the <see cref="CurrencyContent"/> that this currency is for.
		/// </summary>
		public string CurrencyId;

		[SerializeField]
		private long _amount;

		/// <summary>Gets or sets the amount held in this local currency object.</summary>
		/// <value>The local amount, possibly zero while the currency is loading.</value>
		/// <remarks>Setting this property only changes local state and triggers change detection; it does not persist
		/// an inventory change. Use <see cref="PlayerInventory.Update(System.Action{Beamable.Common.Api.Inventory.InventoryUpdateBuilder},string)"/>
		/// with <see cref="Beamable.Common.Api.Inventory.InventoryUpdateBuilder.CurrencyChange"/> to submit changes.</remarks>
		public long Amount
		{
			get => _amount;
			set
			{
				_amount = value;
				TriggerUpdate();
			}
		}

		/// <summary>
		/// The reference to the <see cref="CurrencyContent"/> that this <see cref="PlayerCurrency"/> is connected to.
		/// </summary>
		[NonSerialized]
		public CurrencyContent Content;

		/// <summary>The locally cached properties associated with this currency.</summary>
		/// <remarks>Mutating this dictionary does not persist changes. Submit changes through
		/// <see cref="PlayerInventory.Update(System.Action{Beamable.Common.Api.Inventory.InventoryUpdateBuilder},string)"/>.</remarks>
		public SerializableDictionaryStringToString Properties = new SerializableDictionaryStringToString();


		public PlayerCurrency()
		{
			OnUpdated += () => OnAmountUpdated?.Invoke(Amount);
		}

		public new void TriggerUpdate() => base.TriggerUpdate();

		#region Auto Generated Equality Members

		protected bool Equals(PlayerCurrency other)
		{
			return CurrencyId == other.CurrencyId && Amount == other.Amount;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((PlayerCurrency)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((CurrencyId != null ? CurrencyId.GetHashCode() : 0) * 397) ^ Amount.GetHashCode();
			}
		}

		#endregion

	}


	/// <summary>Contains observable player currencies under a content scope, including its subtypes.</summary>
	/// <remarks>Obtain groups through <see cref="PlayerInventory.GetCurrencies(Beamable.Common.Inventory.CurrencyRef)"/>.
	/// Await <see cref="OnReady"/> before reading initial data. Use inherited <c>OnUpdated</c> notifications
	/// to observe changes and detach handlers when their consumer is disposed.</remarks>
	[Serializable]
	public class PlayerCurrencyGroup : AbsObservableReadonlyList<PlayerCurrency>
	{
		private readonly CurrencyRef _rootRef;
		private readonly PlayerInventory _inventory;
		/// <summary>A promise that completes after this group's initial refresh.</summary>
		/// <remarks>Refresh failures reject it. Use <see cref="Beamable.Common.Player.IRefreshable.Refresh"/> for subsequent refreshes.</remarks>
		public Promise OnReady;

		/// <summary>
		/// The scope defines which currencies in the inventory this group will be able to view.
		/// If the scope is "currency", then this group will view every currency in the player inventory.
		/// However, if the scope was "currency.a", then the group would only show items that were
		/// instances of "currency.a" and sub types.
		/// </summary>
		public string RootScope => _rootRef?.Id ?? "currency";

		public PlayerCurrencyGroup(CurrencyRef rootRef, PlayerInventory inventory)
		{
			_rootRef = rootRef;
			_inventory = inventory;
			OnReady = Refresh(); // automatically refresh..
		}

		public void Notify()
		{
			var data = _inventory.LocalCurrencies.GetAll(RootScope);
			SetData(data);
		}

		protected override async Promise PerformRefresh()
		{
			await _inventory.Refresh(RootScope);
			Notify();
		}

		/// <summary>
		/// Get a specific currency.
		/// If the currency doesn't yet exist, it will be returned as a new currency with a value of 0.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public PlayerCurrency GetCurrency(CurrencyRef id)
		{
			return _inventory.GetCurrency(id);
		}

		/// <summary>
		/// Update the currency's <see cref="PlayerCurrency.Amount"/> by the given amount.
		/// Internally, this will trigger an <see cref="PlayerInventory.Update(Beamable.Common.Api.Inventory.InventoryUpdateBuilder,string)"/>
		/// </summary>
		/// <param name="currency">The currency to modify</param>
		/// <param name="amount">The amount the currency will change by. This is incremental. A negative number will decrement the currency.</param>
		/// <returns>A promise representing when the add operation has completed</returns>
		public Promise Add(CurrencyRef currency, long amount)
		{
			return _inventory.Update(b => b.CurrencyChange(currency, amount));
		}

	}
}
