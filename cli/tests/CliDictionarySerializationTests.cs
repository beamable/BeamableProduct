using System;
using System.CodeDom;
using System.Text.RegularExpressions;
using Beamable.Common.BeamCli;
using Beamable.Serialization;
using Beamable.Server.Common;
using cli;
using cli.Deployment.Services;
using cli.OtelCommands;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests;

public class CliDictionarySerializationTests
{
	[TestCase(typeof(DeployablePlan), 2)]
	[TestCase(typeof(FetchCommandLogRecord), 1)]
	[TestCase(typeof(RealmConfigOutput), 1)]
	public void ResultModelsGenerateScopedDictionarySuppressions(Type modelType, int dictionaryCount)
	{
		var unit = new CodeCompileUnit();
		unit.Namespaces.Add(new CodeNamespace("Beamable.Editor.BeamCli.Commands")
		{
			Types = { UnityCliGenerator.GenerateResultStreamType(modelType) }
		});
		var original = UnityHelper.GenerateCsharp(unit);
		var generated = UnityHelper.GenerateJsonModelCsharp(unit);
		const string suppression = "[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Serialization\", \"UAC1015\", Justification = \"Reflection JSON requires this dictionary.\")]";
		Assert.That(Regex.Matches(generated, Regex.Escape(suppression)).Count, Is.EqualTo(dictionaryCount));
		StringAssert.DoesNotContain("#pragma warning", generated);
		StringAssert.DoesNotContain("NonSerialized", generated);
		StringAssert.DoesNotContain("SerializeField", generated);
		Regex.Replace(generated, @"(?m)^[ \t]*\[System.Diagnostics.CodeAnalysis.SuppressMessage[^\r\n]*[\r\n]+", "").AssertSrc(original);
		Assert.That(UnityHelper.GenerateJsonModelCsharp(unit), Is.EqualTo(generated));
		Assert.That(UnityHelper.GenerateCsharp(unit), Is.EqualTo(original));
	}

	[TestCase(typeof(DeployablePlan), "notes")]
	[TestCase(typeof(DeployablePlan), "references")]
	[TestCase(typeof(FetchCommandLogRecord), "LogAttributes")]
	[TestCase(typeof(RealmConfigOutput), "Config")]
	public void ReporterJsonPreservesDictionaryValues(Type modelType, string fieldName)
	{
		var model = Activator.CreateInstance(modelType)!;
		var field = modelType.GetField(fieldName)!;
		foreach (var state in new[] { "null", "empty", "populated" })
		{
			var expected = state == "null" ? JValue.CreateNull() : JToken.Parse(state == "empty" ? "{}" :
				fieldName == "notes" ? "{\"service.one\":[\"changed\",\"quote\\\" and newline\\n\"],\"empty\":[]}" :
				"{\"realm|key\":\"quote\\\" and newline\\n\",\"empty\":\"\",\"unicode\":\"café\"}");
			field.SetValue(model, expected.ToObject(field.FieldType));
			var envelope = new ReportDataPoint { ts = 42, type = "stream", data = model };
			var json = JsonConvert.SerializeObject(envelope, UnitySerializationSettings.Instance);
			var payload = JObject.Parse(json);
			Assert.That((long)payload["ts"]!, Is.EqualTo(42));
			Assert.That((string)payload["type"]!, Is.EqualTo("stream"));
			Assert.That(((JObject)payload["data"]!).ContainsKey(fieldName), Is.True);
			Assert.That(JToken.DeepEquals(payload["data"]![fieldName], expected), Is.True);
			var restored = JsonConvert.DeserializeObject(payload["data"]!.ToString(), modelType, UnitySerializationSettings.Instance)!;
			Assert.That(field.GetValue(restored), Is.EqualTo(field.GetValue(model)));
			if (model is DeployablePlan plan && state != "null")
				Assert.That(JToken.DeepEquals(JObject.Parse(JsonSerializable.ToJson(plan))[fieldName], expected), Is.True);
		}
	}
}
