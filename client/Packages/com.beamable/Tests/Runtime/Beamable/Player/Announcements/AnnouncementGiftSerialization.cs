using Beamable.Common.Api.Announcements;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Player.Notifications
{
	public class AnnouncementGiftSerialization
	{
		[Test]
		public void AnnouncementDeserializeWithGift()
		{
			var json = @"{
    ""announcements"": [
        {
            ""id"": ""giftTest"",
            ""channel"": ""main"",
            ""title"": ""title"",
            ""summary"": ""summary"",
            ""body"": ""body"",
            ""attachments"": [],
            ""gift"": {
                ""description"": ""asdf"",
                ""applyVipBonus"": true,
                ""changeCurrencies"": [
                    {
                        ""symbol"": ""currency.gems"",
                        ""amount"": 15
                    }
                ],
				""addItems"": [
				    {
				        ""properties"": {
				            ""a"": ""b""
				        },
				        ""symbol"": ""items.diamond_1""
				    }
				]
            },
            ""isRead"": false,
            ""isClaimed"": false,
            ""isDeleted"": false,
            ""clientDataList"": []
        }
    ]
}";

			var response = AnnouncementSerializationUtil.DeserializeQueryResponse(json);

			Assert.AreEqual(1, response.announcements.Count);

			Assert.AreEqual("giftTest", response.announcements[0].id);
			Assert.AreEqual("body", response.announcements[0].body);
			Assert.AreEqual("summary", response.announcements[0].summary);
			Assert.AreEqual("title", response.announcements[0].title);
			Assert.AreEqual("main", response.announcements[0].channel);
			Assert.AreEqual(false, response.announcements[0].isRead);
			Assert.AreEqual(false, response.announcements[0].isClaimed);

			Assert.AreEqual(true, response.announcements[0].gift.applyVipBonus.HasValue);
			Assert.AreEqual(true, response.announcements[0].gift.applyVipBonus.Value);
			Assert.AreEqual(true, response.announcements[0].gift.description.HasValue);
			Assert.AreEqual("asdf", response.announcements[0].gift.description.Value);
			Assert.AreEqual(true, response.announcements[0].gift.currencies.HasValue);
			Assert.AreEqual(1, response.announcements[0].gift.currencies.Value.listData.Count);
			Assert.AreEqual(15, response.announcements[0].gift.currencies.Value.listData[0].amount);
			Assert.AreEqual("currency.gems", response.announcements[0].gift.currencies.Value.listData[0].symbol.Id);

			Assert.AreEqual(1, response.announcements[0].gift.items.Value.listData.Count);
			Assert.AreEqual("items.diamond_1", response.announcements[0].gift.items.Value.listData[0].symbol.Id);
			Assert.AreEqual(true, response.announcements[0].gift.items.Value.listData[0].properties.HasValue);
			Assert.AreEqual(1, response.announcements[0].gift.items.Value.listData[0].properties.Value.Count);
			Assert.AreEqual("b", response.announcements[0].gift.items.Value.listData[0].properties.Value["a"]);
		}


		[Test]
		public void AnnouncementDeserializeWithGift_Empty()
		{
			var json = @"{
    ""announcements"": [
        {
            ""id"": ""giftTest"",
            ""channel"": ""main"",
            ""title"": ""title"",
            ""summary"": ""summary"",
            ""body"": ""body"",
            ""attachments"": [],
            ""gift"": {
                
            },
            ""isRead"": false,
            ""isClaimed"": false,
            ""isDeleted"": false,
            ""clientDataList"": []
        }
    ]
}";

			var response = AnnouncementSerializationUtil.DeserializeQueryResponse(json);
			Assert.AreEqual(1, response.announcements.Count);

			Assert.AreEqual("giftTest", response.announcements[0].id);
			Assert.AreEqual("body", response.announcements[0].body);
			Assert.AreEqual("summary", response.announcements[0].summary);
			Assert.AreEqual("title", response.announcements[0].title);
			Assert.AreEqual("main", response.announcements[0].channel);
			Assert.AreEqual(false, response.announcements[0].isRead);
			Assert.AreEqual(false, response.announcements[0].isClaimed);

			Assert.AreEqual(false, response.announcements[0].gift.applyVipBonus.HasValue);
			Assert.AreEqual(false, response.announcements[0].gift.description.HasValue);
			Assert.AreEqual(false, response.announcements[0].gift.currencies.HasValue);
			Assert.AreEqual(false, response.announcements[0].gift.items.HasValue);
		}
	}


}
