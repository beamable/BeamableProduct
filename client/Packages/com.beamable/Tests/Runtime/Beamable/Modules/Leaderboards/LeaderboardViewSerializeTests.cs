using Beamable.Common.Api.Leaderboards;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.Modules.Leaderboards
{
	public class LeaderboardViewSerializeTests
	{
		[Test]
		public void LeaderBoardV2ViewResponse_RankgtGiven()
		{
			var instance = JsonUtility.FromJson<LeaderBoardV2ViewResponse>(LEADERBOARD_JSON_WITH_RANKGT);
			Assert.That(instance.lb.rankgt, Is.Not.Null);
			Assert.That(instance.lb.rankgt.gt, Is.EqualTo(1725855903908867L));
		}
		
		[Test]
		public void LeaderBoardV2ViewResponse_RankgtInferredFromUserId()
		{
			var instance = JsonUtility.FromJson<LeaderBoardV2ViewResponse>(LEADERBOARD_JSON_WITHOUT_RANKGT);
			instance.lb.userId = 1715864498066877;
			Assert.That(instance.lb.rankgt, Is.Not.Null);
			Assert.That(instance.lb.rankgt.gt, Is.EqualTo(1715864498066877));
		}

		private const string LEADERBOARD_JSON_WITH_RANKGT = @"{
  ""result"": ""ok"",
  ""lb"": {
    ""lbId"": ""leaderboards.issue"",
    ""boardSize"": 5,
    ""rankgt"": {
      ""gt"": 1725855903908867,
      ""rank"": 1,
      ""columns"": {
        ""score"": 5
      },
      ""score"": 5.0,
      ""stats"": []
    },
    ""rankings"": [
      {
        ""gt"": 1725855903908867,
        ""rank"": 1,
        ""columns"": {
          ""score"": 5
        },
        ""score"": 5.0,
        ""stats"": []
      },
      {
        ""gt"": 1725859500139527,
        ""rank"": 2,
        ""columns"": {
          ""score"": 5
        },
        ""score"": 5.0,
        ""stats"": []
      },
      {
        ""gt"": 1715864498066877,
        ""rank"": 3,
        ""columns"": {
          ""score"": 3
        },
        ""score"": 3.0,
        ""stats"": []
      }
    ]
  }
}";
		
		private const string LEADERBOARD_JSON_WITHOUT_RANKGT = @"{
  ""result"": ""ok"",
  ""lb"": {
    ""lbId"": ""leaderboards.issue"",
    ""boardSize"": 5,
    ""rankings"": [
      {
        ""gt"": 1725855903908867,
        ""rank"": 1,
        ""columns"": {
          ""score"": 5
        },
        ""score"": 5.0,
        ""stats"": []
      },
      {
        ""gt"": 1725859500139527,
        ""rank"": 2,
        ""columns"": {
          ""score"": 5
        },
        ""score"": 5.0,
        ""stats"": []
      },
      {
        ""gt"": 1715864498066877,
        ""rank"": 3,
        ""columns"": {
          ""score"": 3
        },
        ""score"": 3.0,
        ""stats"": []
      }
    ]
  }
}";
	}
}
