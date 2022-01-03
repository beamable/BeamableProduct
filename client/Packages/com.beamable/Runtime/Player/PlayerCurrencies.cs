using Beamable.Api;
using Beamable.Api.Inventory;
using Beamable.Common;
using System;
using System.Linq;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Common.Player;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player
{
	[Serializable]
	public class PlayerCurrency : ObservableLong
	{
		public string CurrencyId;

		public long Amount
		{
			get => Value;
			set => Value = value;
		}
		// TODO: add the currencyRef? As a computed property of CurrencyId probably.

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

	[Serializable]
	public class PlayerCurrencyGroup : AbsObservableReadonlyList<PlayerCurrency>
	{
		private readonly IPlatformService _platformService;
		private readonly InventoryService _inventoryApi;
		private readonly INotificationService _notificationService;
		private readonly ISdkEventService _sdkEventService;
		private readonly IDependencyProvider _provider;

		public PlayerCurrencyGroup(
			IPlatformService platformService,
		                           InventoryService inventoryApi,
		                           INotificationService notificationService,
		                           ISdkEventService sdkEventService,
		                           IDependencyProvider provider)
		{
			_platformService = platformService;
			_inventoryApi = inventoryApi;
			_notificationService = notificationService;
			_sdkEventService = sdkEventService;
			_provider = provider;

			// _sdkEventService.Register(nameof(PlayerCurrencyGroup), HandleEvent);
			notificationService.Subscribe(notificationService.GetRefreshEventNameForService("inventory"),
			                              HandleSubscriptionUpdate);

			// _inventoryApi.Subscribe(rootRef, HandleSubscriptionUpdate);

			var _ = Refresh(); // automatically start.
			IsInitialized = true;
		}


		private async Promise HandleEvent(SdkEvent evt)
		{
			switch (evt.Event)
			{
				case "add":

					// TODO: pull out into separate method
					var currencyId = evt.Args[0];
					var amount = long.Parse(evt.Args[1]);
					var currency = GetCurrency(currencyId);
					// currency.Amount += amount;
					// TriggerUpdate();
					try
					{
						await _inventoryApi.AddCurrency(currencyId, amount);
					}
					finally
					{
						TriggerUpdate();
					}

					break;
				default:
					throw new Exception($"Unhandled event: {evt.Event}");
			}
		}

		private void HandleSubscriptionUpdate(object raw)
		{
			var _ = Refresh(); // fire-and-forget.
		}

		protected override async Promise PerformRefresh()
		{
			await _platformService.OnReady;

			try
			{
				var data = await _inventoryApi.GetCurrencies(new string[] { });

				var next = new List<PlayerCurrency>();
				var seen = new HashSet<PlayerCurrency>();
				foreach (var kvp in data)
				{
					var existing = this.FirstOrDefault(c => c.CurrencyId == kvp.Key);
					if (existing != null)
					{
						next.Add(existing);
						existing.Amount = kvp.Value;
						seen.Add(existing);
					}
					else
					{
						next.Add(new PlayerCurrency {CurrencyId = kvp.Key, Amount = kvp.Value});
					}
				}

				var unseen = this.Except(seen);
				foreach (var currency in unseen)
				{
					currency.Amount = 0; // deleted.
					next.Add(currency);
				}

				SetData(next);
			}
			catch (NoConnectivityException)
			{
				// oh well, not much to do.
			}
		}

		/// <summary>
		/// Get a specific currency.
		/// If the currency doesn't yet exist, it will be returned as a new currency with a value of 0.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public PlayerCurrency GetCurrency(CurrencyRef id)
		{
			var existing = this.FirstOrDefault(a => string.Equals(a.CurrencyId, id));
			if (existing == null)
			{
				var currency = new PlayerCurrency {CurrencyId = id, Amount = 0}; // TODO: should this be the starting amount?
				// commit this to memory.

				var next = this.ToList();
				next.Add(currency);
				SetData(next);
				existing = currency;
			}
			return existing;
		}

		public Promise Add(CurrencyRef currencyId, long amount)
		{
			// any writes should go through the inventory service itself to optimize performance and code-single-use.
			// we can't pre-fetch the PlayerInventory service because it would cause a cyclic reference in the dependency graph.
			return _provider.GetService<PlayerInventory>().Update(b => b.CurrencyChange(currencyId, amount));
		}

		// public Promise Add(CurrencyRef currencyId, long amount) =>
		// 	_sdkEventService.Add(new SdkEvent(nameof(PlayerCurrencyGroup), "add", currencyId.Id, amount.ToString()));
	}
}
