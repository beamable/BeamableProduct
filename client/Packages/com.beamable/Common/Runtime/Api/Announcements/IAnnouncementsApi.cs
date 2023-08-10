using Beamable.Common.Announcements;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Common.Api.Announcements
{
	public interface IAnnouncementsApi : ISupportsGet<AnnouncementQueryResponse>
	{
		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// When an announcement is created, it will be sent to all players, and will be "unread".
		/// This method will change the <see cref="AnnouncementView.isRead"/> field to true, and
		/// prevent the announcement from appearing as "new" to a <see cref="Auth.User"/>.
		/// However, the announcement is still in the player's inbox.
		/// </summary>
		/// <param name="id">The id of an <see cref="AnnouncementView"/>. This should be the <see cref="AnnouncementView.id"/> field.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> MarkRead(string id);

		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// When an announcement is created, it will be sent to all players, and will be "unread".
		/// This method will change the <see cref="AnnouncementView.isRead"/> field to true for a given set of announcements, and
		/// prevent the announcements from appearing as "new" to a <see cref="Auth.User"/>.
		/// However, the announcement is still in the player's inbox.
		/// </summary>
		/// <param name="ids">A set of ids from <see cref="AnnouncementView"/>s. These should be the <see cref="AnnouncementView.id"/> field values.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> MarkRead(List<string> ids);

		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// This method will permanently delete an announcement from the player's inbox so that they never see it again.
		/// <b>Be careful!</b>, an announcement may also have rewards for the player, but if the announcement is deleted before
		/// the rewards are claimed, then the rewards are lost. Use the <see cref="Claim(string)"/> method to make sure the rewards
		/// are granted before you delete a player's announcement. Use the <see cref="AnnouncementView.isClaimed"/> field to check if
		/// the announcement has been claimed.
		/// </summary>
		/// <param name="id">The id of an <see cref="AnnouncementView"/>. This should be the <see cref="AnnouncementView.id"/> field.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> MarkDeleted(string id);

		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// This method will permanently delete a set of announcements from the player's inbox so that they never see them again.
		/// <b>Be careful!</b>, announcements may also have rewards for the player, but if announcements are deleted before
		/// the rewards are claimed, then the rewards are lost. Use the <see cref="Claim(List{string})"/> method to make sure the rewards
		/// are granted before you delete player's announcements. Use the <see cref="AnnouncementView.isClaimed"/> field to check if
		/// the announcements have been claimed.
		/// </summary>
		/// <param name="ids">A set of ids from <see cref="AnnouncementView"/>s. These should be the <see cref="AnnouncementView.id"/> field values.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> MarkDeleted(List<string> ids);

		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// Announcements can have rewards associated with them that the player can claim one time.
		/// These rewards are stored in the <see cref="AnnouncementView.gift"/> field, and some legacy rewards
		/// are visible in the <see cref="AnnouncementView.attachments"/> field.
		/// This method will grant the rewards to the player for one announcement.
		/// Once the rewards have been claimed, the <see cref="AnnouncementView.isClaimed"/> will be true.
		/// </summary>
		/// <param name="id">The id of an <see cref="AnnouncementView"/>. This should be the <see cref="AnnouncementView.id"/> field.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> Claim(string id);

		/// <summary>
		/// A <see cref="Auth.User"/> can have an inbox of <see cref="AnnouncementView"/>.
		/// Announcements can have rewards associated with them that the player can claim one time.
		/// These rewards are stored in the <see cref="AnnouncementView.gift"/> field, and some legacy rewards
		/// are visible in the <see cref="AnnouncementView.attachments"/> field.
		/// This method will grant the rewards to the player for a set of announcements.
		/// Once the rewards have been claimed, the <see cref="AnnouncementView.isClaimed"/> will be true.
		/// </summary>
		/// <param name="ids">A set of ids from <see cref="AnnouncementView"/>s. These should be the <see cref="AnnouncementView.id"/> field values.</param>
		/// <returns>An empty <see cref="Promise"/> representing the network call to Beamable.</returns>
		Promise<EmptyResponse> Claim(List<string> ids);
	}


	[Serializable]
	public class AnnouncementQueryResponse : JsonSerializable.ISerializable
	{
		public List<AnnouncementView> announcements;
		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.SerializeList(nameof(announcements), ref announcements);
		}
	}

	/// <summary>
	/// The <see cref="AnnouncementView"/> is the network response from the Beamable Announcement API.
	/// It contains details describing the current state of a particular announcement.
	/// </summary>
	[Serializable]
	public class AnnouncementView : CometClientData, JsonSerializable.ISerializable
	{
		/// <summary>
		/// The runtime id of the announcement. This is not guaranteed to be the same as the content's id that spawned the announcement.
		/// </summary>
		public string id;

		/// <summary>
		/// The name of the inbox that the announcement should be categorized with. The value can be whatever you'd like.
		/// Common usages could be, "Primary", "Advertising", "Seasonal", etc.
		/// </summary>
		public string channel;

		/// <summary>
		/// Announcements can have an optional <see cref="startDate"/> <see cref="endDate"/>. If the current time is before the start date, or
		/// after the end date, then the announcement won't be visible to the player.
		/// If there is no start date, then this value will be null.
		/// </summary>
		public string startDate;

		/// <summary>
		/// Announcements can have an optional <see cref="startDate"/> <see cref="endDate"/>. If the current time is before the start date, or
		/// after the end date, then the announcement won't be visible to the player.
		/// If there is no end date, then this value will be null.
		/// </summary>
		public string endDate;

		/// <summary>
		/// Announcements can have an <see cref="endDate"/>, after which the announcement will no longer be visible.
		/// The <see cref="secondsRemaining"/> is the number of seconds before the <see cref="endDate"/>, as evaluated when this
		/// <see cref="AnnouncementView"/> instances was received. This value will not update in real time.
		/// If there is no <see cref="endDate"/>, then this value will be 0, the default(long)
		/// </summary>
		public long secondsRemaining;

		/// <summary>
		/// Announcements can have an optional <see cref="startDate"/> <see cref="endDate"/>. If the current time is before the start date, or
		/// after the end date, then the announcement won't be visible to the player.
		/// If there is no end date, then this value will be the default(DateTime)
		/// </summary>
		public DateTime endDateTime;

		/// <summary>
		/// Every announcement must have a title. This is similar to the subject line of an email.
		/// </summary>
		public string title;

		/// <summary>
		/// Every announcement must have a brief summary. This can be used to provide some flavor text of the
		/// announcement before the player decides to read the announcement.
		/// </summary>
		public string summary;

		/// <summary>
		/// Every announcement must have a body. This body is a plain string, but can be evaluated however you like.
		/// The body is where the main content of the announcement should be.
		/// </summary>
		public string body;

		/// <summary>
		/// An announcement can have rewards that the player can claim with the <see cref="IAnnouncementsApi.Claim(string)"/> method.
		/// This list of <see cref="AnnouncementAttachment"/> is a set of rewards that the player will get when they claim the announcement.
		/// The contents of this field align with the values of the <see cref="AnnouncementContent.attachments"/> field.
		/// <b>Be careful!</b> Player rewards can also exist in the <see cref="gift"/> field.
		/// In the future, this field will be deprecated in favor of the <see cref="gift"/> field.
		/// </summary>
		public List<AnnouncementAttachment> attachments;

		/// <summary>
		/// An announcement can have rewards that the player can claim with the <see cref="IAnnouncementsApi.Claim(string)"/> method.
		/// The <see cref="AnnouncementPlayerRewards"/> is the set of rewards that the player will get when they claim the announcement.
		/// The contents of this field align with the values of the <see cref="AnnouncementContent.gift"/> field.
		///  <b>Be careful!</b> Player rewards can also exist in the <see cref="attachments"/> field.
		/// In the future, this field will replace the <see cref="attachments"/> field entirely.
		/// </summary>
		public PlayerRewardView gift = new PlayerRewardView();

		/// <summary>
		/// You can mark an announcement as "read" so that it doesn't appear as "new" for the player. This field shows if the
		/// announcement has been read. You can mark an announcement as read with the <see cref="IAnnouncementsApi.MarkRead(string)"/> method.
		/// </summary>
		public bool isRead;

		/// <summary>
		/// Announcements can have rewards that the player can claim. If the player has already claimed the rewards for the
		/// announcement, this field will be true. You can claim the rewards for an announcement with the <see cref="IAnnouncementsApi.Claim(string)"/> method.
		///
		/// Also see the <see cref="HasClaimsAvailable"/> method.
		/// </summary>
		public bool isClaimed;

		/// <summary>
		/// Announcements can have rewards that the player can claim. This method will tell you if there are pending rewards to claim.
		/// The value of <see cref="isClaimed"/> may be false, even if there are no rewards anyway.
		/// </summary>
		/// <returns>true if there are available rewards, and the player hasn't already claimed them; false otherwise</returns>
		public bool HasClaimsAvailable()
		{
			return !isClaimed && (attachments?.Count > 0 || (gift?.HasAnyReward() ?? false));
		}

		internal void Init()
		{
			endDateTime = DateTime.UtcNow.AddSeconds(secondsRemaining);
		}

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize(nameof(id), ref id);
			s.Serialize(nameof(channel), ref channel);
			s.Serialize(nameof(startDate), ref startDate);
			s.Serialize(nameof(endDate), ref endDate);
			s.Serialize(nameof(secondsRemaining), ref secondsRemaining);
			s.Serialize(nameof(title), ref title);
			s.Serialize(nameof(summary), ref summary);
			s.Serialize(nameof(body), ref body);
			s.Serialize(nameof(isRead), ref isRead);
			s.Serialize(nameof(isClaimed), ref isClaimed);
			s.SerializeList(nameof(attachments), ref attachments);
			s.Serialize(nameof(gift), ref gift);
			s.SerializeArray(nameof(clientDataList), ref clientDataList);
		}
	}

	public static class AnnouncementSerializationUtil
	{
		public static AnnouncementQueryResponse DeserializeQueryResponse(string json)
		{
			var res = JsonSerializable.FromJson<AnnouncementQueryResponse>(json);
			return res;
		}
	}


	[System.Serializable]
	public class PlayerRewardCurrencyChangeView : JsonSerializable.ISerializable
	{
		public CurrencyRef symbol = new CurrencyRef();
		public long amount;

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			if (s.HasKey(nameof(symbol))
				|| symbol != default(CurrencyRef))
			{
				s.Serialize(nameof(symbol), ref symbol.Id);
			}

			s.Serialize(nameof(amount), ref amount);
		}
	}


	[Serializable]
	public class PlayerRewardNewItemView : JsonSerializable.ISerializable
	{
		public ItemRef symbol = new ItemRef();
		public OptionalSerializableDictionaryStringToString properties = new OptionalSerializableDictionaryStringToString();

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			if (s.HasKey(nameof(symbol))
				|| symbol != default(ItemRef))
			{
				s.Serialize(nameof(symbol), ref symbol.Id);
			}

			if ((s.HasKey(nameof(properties))
				 || ((properties != default(OptionalSerializableDictionaryStringToString))
					 && properties.HasValue)))
			{
				s.SerializeDictionary<SerializableDictionaryStringToString, string>(nameof(properties), ref properties.Value);
				properties.HasValue = true;
			}
		}
	}


	[Serializable]
	public class PlayerRewardListOfCurrencyChangeView : DisplayableList<PlayerRewardCurrencyChangeView>
	{
		public List<PlayerRewardCurrencyChangeView> listData = new List<PlayerRewardCurrencyChangeView>();

		protected override IList InternalList => listData;
		public override string GetListPropertyPath() => nameof(listData);
	}



	[Serializable]
	public class PlayerRewardListOfNewItemView : DisplayableList<PlayerRewardNewItemView>
	{
		public List<PlayerRewardNewItemView> listData = new List<PlayerRewardNewItemView>();

		protected override IList InternalList => listData;
		public override string GetListPropertyPath() => nameof(listData);
	}


	[Serializable]
	public class OptionalPlayerRewardCurrencyChangeView : Optional<PlayerRewardListOfCurrencyChangeView>
	{
		public OptionalPlayerRewardCurrencyChangeView()
		{
			Value = new PlayerRewardListOfCurrencyChangeView();
		}
	}

	[Serializable]
	public class OptionalPlayerRewardNewItemView : Optional<PlayerRewardListOfNewItemView>
	{
		public OptionalPlayerRewardNewItemView()
		{
			Value = new PlayerRewardListOfNewItemView();
		}
	}

	[Serializable]
	public class PlayerRewardView : JsonSerializable.ISerializable
	{
		public OptionalString description = new OptionalString();
		public OptionalBool applyVipBonus = new OptionalBool();
		public OptionalPlayerRewardCurrencyChangeView currencies = new OptionalPlayerRewardCurrencyChangeView();
		public OptionalPlayerRewardNewItemView items = new OptionalPlayerRewardNewItemView();

		/// <summary>
		/// Check if there are any currencies or items in this <see cref="PlayerReward"/>
		/// </summary>
		/// <returns>true if there are any rewards, false otherwise.</returns>
		public virtual bool HasAnyReward()
		{
			var anyCurrencies = currencies.GetOrElse(() => null)?.Count > 0;
			var anyItems = items?.GetOrElse(() => null)?.Count > 0;
			return anyCurrencies || anyItems;
		}


		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			if ((s.HasKey(nameof(applyVipBonus))
				 || ((applyVipBonus != default(OptionalBool))
					 && applyVipBonus.HasValue)))
			{
				s.Serialize(nameof(applyVipBonus), ref applyVipBonus.Value);
				applyVipBonus.HasValue = true;
			}

			if ((s.HasKey(nameof(description))
				 || ((description != default(OptionalString))
					 && description.HasValue)))
			{
				s.Serialize(nameof(description), ref description.Value);
				description.HasValue = true;
			}

			if ((s.HasKey("changeCurrencies")
				 || ((currencies != default(OptionalPlayerRewardCurrencyChangeView))
					 && currencies.HasValue)))
			{
				s.SerializeList("changeCurrencies", ref currencies.Value.listData);
				currencies.HasValue = true;
			}

			if ((s.HasKey("addItems")
				 || ((items != default(OptionalPlayerRewardNewItemView))
					 && items.HasValue)))
			{
				s.SerializeList("addItems", ref items.Value.listData);
				items.HasValue = true;
			}
		}
	}

	[Serializable]
	public class AnnouncementRequest
	{
		public List<string> announcements;

		public AnnouncementRequest(List<string> announcements)
		{
			this.announcements = announcements;
		}
	}
}
