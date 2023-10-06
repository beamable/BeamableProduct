using Beamable.Common.Player;
using Beamable.Player;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;

namespace Beamable.Tests.Runtime
{
	public class ResortTests : BeamContextTest
	{
		static void Check(List<PlayerLeaderboardEntry> list, int index, long playerId, long rank, double score)
		{
			Assert.AreEqual(playerId, list[index].playerId, $"playerId does not match for index=[{index}]");
			Assert.AreEqual(rank, list[index].rank, $"rank does not match for index=[{index}] playerId=[{playerId}]");
			Assert.AreEqual(score, list[index].score, $"score does not match for index=[{index}] playerId=[{playerId}]");
		}

		[Test]
		public void PlayerDoesNotExist()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			Assert.IsEmpty(list);

			var outcome = list.TryUpdateEntryIfExists(10, 3, out var rank);
			Assert.IsFalse(outcome);
			Assert.IsEmpty(list);
		}

		[Test]
		public void OnePlayerInList_Change()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 10, rank = 1 });

			Assert.AreEqual(1, list.Count);

			var outcome = list.TryUpdateEntryIfExists(10, 3, out var rank);
			Assert.IsTrue(outcome);

			Assert.AreEqual(3, listValues[0].score);
			Assert.AreEqual(1, listValues[0].rank);
			Assert.AreEqual(1, rank);
		}


		[Test]
		public void MultiplePlayers_ChangeScore_ButNoRankChange()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 3, rank = 3, score = 80 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 4, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 5, score = 60 });

			Assert.AreEqual(5, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 85, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(3, rank);
			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 1, 1, 100);
			Check(listValues, 1, 2, 2, 90);
			Check(listValues, 2, 3, 3, 85);
			Check(listValues, 3, 4, 4, 70);
			Check(listValues, 4, 5, 5, 60);
		}



		[Test]
		public void MultiplePlayers_MoveToTopOfList_FromMidRange()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 3, rank = 3, score = 80 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 4, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 5, score = 60 });

			Assert.AreEqual(5, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 110, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(1, rank);

			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 3, 1, 110);
			Check(listValues, 1, 1, 2, 100);
			Check(listValues, 2, 2, 3, 90);
			Check(listValues, 3, 4, 4, 70);
			Check(listValues, 4, 5, 5, 60);
		}


		[Test]
		public void MultiplePlayers_MoveToAlmostTopOfList_FromMidRange()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 3, rank = 3, score = 80 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 4, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 5, score = 60 });

			Assert.AreEqual(5, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 95, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(2, rank);

			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 1, 1, 100);
			Check(listValues, 1, 3, 2, 95);
			Check(listValues, 2, 2, 3, 90);
			Check(listValues, 3, 4, 4, 70);
			Check(listValues, 4, 5, 5, 60);
		}


		[Test]
		public void MultiplePlayers_MoveToBottomOfList_FromMidRange()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 3, rank = 3, score = 80 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 4, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 5, score = 60 });

			Assert.AreEqual(5, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 30, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(5, rank);

			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 1, 1, 100);
			Check(listValues, 1, 2, 2, 90);
			Check(listValues, 2, 4, 3, 70);
			Check(listValues, 3, 5, 4, 60);
			Check(listValues, 4, 3, 5, 30);
		}


		[Test]
		public void MultiplePlayers_MoveToAlmostBottomOfList_FromMidRange()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 3, rank = 3, score = 80 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 4, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 5, score = 60 });

			Assert.AreEqual(5, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 65, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(4, rank);

			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 1, 1, 100);
			Check(listValues, 1, 2, 2, 90);
			Check(listValues, 2, 4, 3, 70);
			Check(listValues, 3, 3, 4, 65);
			Check(listValues, 4, 5, 5, 60);
		}


		[Test]
		public void MultiplePlayers_NotInList_ButEnters_FromBelow()
		{
			TriggerContextInit();
			var list = new PlayerTopScoresList(null, Context.ServiceProvider);

			var field = typeof(AbsObservableReadonlyList<PlayerLeaderboardEntry>).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			listValues.Add(new PlayerLeaderboardEntry { playerId = 1, rank = 1, score = 100 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 2, rank = 2, score = 90 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 4, rank = 3, score = 70 });
			listValues.Add(new PlayerLeaderboardEntry { playerId = 5, rank = 4, score = 60 });

			Assert.AreEqual(4, list.Count);

			var outcome = list.TryUpdateEntryIfExists(3, 85, out var rank);
			Assert.IsTrue(outcome);
			Assert.AreEqual(3, rank);
			listValues = (List<PlayerLeaderboardEntry>)field.GetValue(list);

			Check(listValues, 0, 1, 1, 100);
			Check(listValues, 1, 2, 2, 90);
			Check(listValues, 2, 3, 3, 85);
			Check(listValues, 3, 4, 4, 70);
		}

	}
}
