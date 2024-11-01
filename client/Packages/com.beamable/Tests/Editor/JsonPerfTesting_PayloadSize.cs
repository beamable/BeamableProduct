using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using NUnit.Framework;
using System;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class JsonPerfTesting_PayloadSize
	{
		private const string currencyJson = // shortish
			"{\"id\":\"currency.gems\",\"version\":\"123\",\"properties\":{\"clientPermission\":{\"data\":{\"write_self\":false}},\"icon\":{\"data\":{\"referenceKey\":\"f819a6beb22c04c8d9f8222f930252b5\",\"subObjectName\":\"\"}},\"startingAmount\":{\"data\":0}}}";

		private const string tournamentJson = // longish
			"{\"id\":\"tournaments.New_TournamentContent\",\"version\":\"123\",\"properties\":{\"anchorTimeUTC\":{\"data\":\"2020-01-01T12:00:00Z\"},\"ChampionColor\":{\"data\":{\"a\":1,\"b\":0,\"g\":0.709803939,\"r\":0.968627453}},\"cycleDuration\":{\"data\":\"P1D\"},\"DefaultEntryColor\":{\"data\":{\"a\":1,\"b\":0,\"g\":0,\"r\":0.623529434}},\"groupRewards\":{\"data\":{\"rankRewards\":[]}},\"name\":{\"data\":\"sample\"},\"playerLimit\":{\"data\":3},\"rankRewards\":{\"data\":[{\"currencyRewards\":[],\"maxRank\":1,\"minRank\":1,\"name\":\"Winner\",\"stageMax\":1,\"stageMin\":1,\"tier\":0}]},\"scoreRewards\":{\"data\":[{\"currencyRewards\":[],\"maxScore\":1,\"minScore\":1,\"name\":\"Winner Score\",\"stageMax\":1,\"stageMin\":1,\"tier\":0}]},\"stageChanges\":{\"data\":[{\"color\":{\"a\":1,\"b\":0,\"g\":0.709803939,\"r\":0.968627453},\"delta\":2,\"maxRank\":1,\"minRank\":1},{\"color\":{\"a\":1,\"b\":0.7921569,\"g\":0.7921569,\"r\":0.7921569},\"delta\":2,\"maxRank\":2,\"minRank\":2},{\"color\":{\"a\":1,\"b\":0.160784319,\"g\":0.333333343,\"r\":0.721568644},\"delta\":2,\"maxRank\":3,\"minRank\":3},{\"color\":{\"a\":0,\"b\":0,\"g\":0,\"r\":0},\"delta\":1,\"maxRank\":10,\"minRank\":4},{\"color\":{\"a\":1,\"b\":0.6901961,\"g\":0.6901961,\"r\":0.6901961},\"delta\":0,\"maxRank\":20,\"minRank\":11},{\"color\":{\"a\":1,\"b\":0,\"g\":0,\"r\":0.623529434},\"delta\":-1,\"maxRank\":50,\"minRank\":41}]},\"stagesPerTier\":{\"data\":1},\"tiers\":{\"data\":[{\"color\":{\"a\":1,\"b\":0.160784319,\"g\":0.333333343,\"r\":0.721568644},\"name\":\"Bronze\"},{\"color\":{\"a\":1,\"b\":0.7921569,\"g\":0.7921569,\"r\":0.7921569},\"name\":\"Silver\"},{\"color\":{\"a\":1,\"b\":0,\"g\":0.709803939,\"r\":0.968627453},\"name\":\"Gold\"}]}}}";

		[Serializable]
		class VersionProxy
		{
			public string version;
		}

		[Performance]
		[Test]
		public void ContentJson_ShortClass()
		{
			var serializer = new ClientContentSerializer();
			var count = 1000;
			Measure.Method(() =>
				   {
					   for (var i = 0; i < count; i++)
					   {

						   serializer.Deserialize<ContentObject>(currencyJson);
					   }
				   })
				   .GC()
				   .MeasurementCount(10)
				   .Run();
		}

		[Performance]
		[Test]
		public void ContentJson_LongClass()
		{
			var serializer = new ClientContentSerializer();
			var count = 1000;
			Measure.Method(() =>
				   {
					   for (var i = 0; i < count; i++)
					   {

						   serializer.Deserialize<ContentObject>(tournamentJson);
					   }
				   })
				   .GC()
				   .MeasurementCount(10)
				   .Run();
		}

		[Performance]
		[Test]
		public void UnityJson_ShortClass()
		{
			var count = 1000;
			Measure.Method(() =>
				   {
					   for (var i = 0; i < count; i++)
					   {
						   JsonUtility.FromJson<VersionProxy>(currencyJson);
					   }
				   })
				   .GC()
				   .MeasurementCount(10)
				   .Run();
		}

		[Performance]
		[Test]
		public void UnityJson_LongClass()
		{
			var count = 1000;
			Measure.Method(() =>
				   {
					   for (var i = 0; i < count; i++)
					   {
						   JsonUtility.FromJson<VersionProxy>(tournamentJson);
					   }
				   })
				   .GC()
				   .MeasurementCount(10)
				   .Run();
		}
	}
}
