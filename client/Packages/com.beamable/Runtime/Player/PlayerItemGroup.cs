// unset

using Beamable.Api;
using Beamable.Api.Inventory;
using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Inventory;
using Beamable.Common.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Player
{
	/// <summary>
	/// An item in the player's inventory. The item is uniquely identified by the <see cref="ContentId"/> and <see cref="ItemId"/> as a pair.
	/// <para>
	/// Use the <see cref="Properties"/> dictionary to store runtime instance data about a particular item.
	/// </para>
	/// </summary>
	[Serializable]
	public class PlayerItem : DefaultObservable
	{
		protected bool Equals(PlayerItem other)
		{
			return ContentId == other.ContentId && ItemId == other.ItemId && CreatedAt == other.CreatedAt && UpdatedAt == other.UpdatedAt && Equals(Properties, other.Properties);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((PlayerItem) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (ContentId != null ? ContentId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
				hashCode = (hashCode * 397) ^ CreatedAt.GetHashCode();
				hashCode = (hashCode * 397) ^ UpdatedAt.GetHashCode();
				hashCode = (hashCode * 397) ^ (Properties != null ? Properties.GetHashCode() : 0);
				return hashCode;
			}
		}

		/// <summary>
		/// The id of <see cref="ItemContent"/> that this item is an instance of.
		/// </summary>
		public string ContentId;
		public long ItemId;
		public long CreatedAt;
		public long UpdatedAt;
		public SerializableDictionaryStringToString Properties = new SerializableDictionaryStringToString();

		// TODO: do a binding to the content fields.
		public ItemContent Content;

		public event Action OnDeleted;

		internal void TriggerUpdate(InventoryObject<ItemContent> item)
		{
			CreatedAt = item.CreatedAt;
			UpdatedAt = item.UpdatedAt;
			Properties = new SerializableDictionaryStringToString(item.Properties);
			Content = item.ItemContent;
			TriggerUpdate();
		}

		internal new void TriggerUpdate() => base.TriggerUpdate();

		public override int GetBroadcastChecksum()
		{
			// TODO: XXX: There must be a better way to implement this nonsense.
			var propertiesBroadcastCode = Properties.Select(x => $"{x.Key}:{x.Value}").ToList();
			propertiesBroadcastCode.Sort();
			return $"{ContentId}-{ItemId}-{CreatedAt}-{UpdatedAt}-{string.Join(",",propertiesBroadcastCode)}".GetHashCode();
		}

		internal void TriggerDeletion()
		{
			OnDeleted?.Invoke();
		}
	}

	/// <summary>
	/// A list of <see cref="PlayerItem"/>s grouped by a common type.
	/// </summary>
	[Serializable]
	public class PlayerItemGroup : AbsObservableReadonlyList<PlayerItem>
	{
		private readonly ItemRef _rootRef;
		private readonly IPlatformService _platformService;
		private readonly InventoryService _inventoryService;

		public PlayerItemGroup(ItemRef rootRef, IPlatformService platformService, InventoryService inventoryService, IDependencyProvider provider)
		{
			_rootRef = rootRef;
			_platformService = platformService;
			_inventoryService = inventoryService;
			_platformService.OnReady.Then(_ =>
			{
				_inventoryService.Subscribe(rootRef, OnItemsUpdated);
			});
		}

		private void OnItemsUpdated(InventoryView view)
		{
			// ignore the view, and do a refresh on our own. Yes, this produces more network traffic.
			var _ = Refresh();
		}

		protected override async Promise PerformRefresh()
		{
			await _platformService.OnReady;
			var data = await _inventoryService.GetItems(_rootRef);

			var next = new List<PlayerItem>();
			var seen = new HashSet<PlayerItem>();
			foreach (var kvp in data)
			{
				var existing = this.FirstOrDefault(c => c.ContentId == kvp.ItemContent.Id && c.ItemId == kvp.Id);
				if (existing != null)
				{
					next.Add(existing);
					existing.TriggerUpdate(kvp);
					seen.Add(existing);
				}
				else
				{
					next.Add(new PlayerItem
					{
						Content = kvp.ItemContent,
						ContentId = kvp.ItemContent.Id,
						CreatedAt = kvp.CreatedAt,
						UpdatedAt = kvp.UpdatedAt,
						Properties = new SerializableDictionaryStringToString(kvp.Properties),
						ItemId = kvp.Id
					});
				}
			}

			var unseen = this.Except(seen);
			foreach (var item in unseen)
			{
				item.TriggerDeletion();
			}
			SetData(next);
		}

	}
}
