using System;
using System.Collections.Generic;
using System.Text;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Editor.Tests
{
	public class GeneratedCliDictionaryTests
	{
		public static IEnumerable<TestCaseData> DictionaryCases()
		{
			foreach (var pair in new[]
			{
				(typeof(BeamDeployablePlan), "notes"), (typeof(BeamDeployablePlan), "references"),
				(typeof(BeamFetchCommandLogRecord), "LogAttributes"), (typeof(BeamRealmConfigOutput), "Config")
			})
			foreach (var state in new[] { "null", "empty", "populated" })
				yield return new TestCaseData(pair.Item1, pair.Item2, state);
		}

		[TestCaseSource(nameof(DictionaryCases))]
		public void ReflectionJsonPreservesDictionaryFields(Type modelType, string fieldName, string state)
		{
			var model = Activator.CreateInstance(modelType);
			var field = modelType.GetField(fieldName);
			var value = DictionaryValue(fieldName, state);
			field.SetValue(model, value);
			var json = Json.Serialize(model, new StringBuilder());
			var payload = (ArrayDict)Json.Deserialize(json);
			Assert.That(payload.ContainsKey(fieldName), Is.True);
			Assert.That(Json.Serialize(payload[fieldName], new StringBuilder()),
				Is.EqualTo(Json.Serialize(value, new StringBuilder())));
			if (state != "null")
			{
				var restored = Json.Deserialize(json, modelType);
				Assert.That(field.GetValue(restored), Is.EqualTo(value));
			}
		}

		[TestCaseSource(nameof(DictionaryCases))]
		public void UnityStreamDecodeKeepsExistingDictionaryBehavior(Type modelType, string fieldName, string state)
		{
			var json = "{\"ts\":42,\"type\":\"stream\",\"data\":{\"" + fieldName + "\":"
				+ Json.Serialize(DictionaryValue(fieldName, state), new StringBuilder()) + "}}";
			var envelopeType = typeof(ReportDataPoint<>).MakeGenericType(modelType);
			var restored = JsonUtility.FromJson(json, envelopeType);
			Assert.That(envelopeType.GetField("ts").GetValue(restored), Is.EqualTo(42L));
			Assert.That(envelopeType.GetField("type").GetValue(restored), Is.EqualTo("stream"));
			var data = envelopeType.GetField("data").GetValue(restored);
			Assert.That(data, Is.Not.Null);
			// Unity's typed callbacks currently skip these dictionaries.
			Assert.That(modelType.GetField(fieldName).GetValue(data), Is.Null);
			var encoded = (ArrayDict)Json.Deserialize(JsonUtility.ToJson(data));
			Assert.That(encoded.ContainsKey(fieldName), Is.False);
		}

		private static object DictionaryValue(string fieldName, string state)
		{
			if (state == "null") return null;
			if (fieldName == "notes")
				return state == "empty" ? new Dictionary<string, List<string>>() : new Dictionary<string, List<string>>
				{
					["service.one"] = new List<string> { "changed", "quote\" and newline\n" },
					["empty"] = new List<string>()
				};
			return state == "empty" ? new Dictionary<string, string>() : new Dictionary<string, string>
			{
				["realm|key"] = "quote\" and newline\n", ["empty"] = "", ["unicode"] = "café"
			};
		}
	}
}
