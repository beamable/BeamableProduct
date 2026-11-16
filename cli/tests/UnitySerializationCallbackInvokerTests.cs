using Beamable.Server.Common;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace tests;

/// <summary>
/// UnitySerializationCallbackInvoker takes itself out of the serializer's converters while it writes a value. A
/// serializer that is reused (the CLI server keeps one per response) must get it back even when that write fails.
/// </summary>
public class UnitySerializationCallbackInvokerTests
{
	public class Callback : ISerializationCallbackReceiver
	{
		public int Value;
		public void OnBeforeSerialize() => Value = 7;
		public void OnAfterDeserialize() { }
	}

	public class FailingCallback : ISerializationCallbackReceiver
	{
		public Failing failing = new();
		public void OnBeforeSerialize() { }
		public void OnAfterDeserialize() { }
	}

	[JsonConverter(typeof(FailingConverter))]
	public class Failing { }

	public class FailingConverter : JsonConverter<Failing>
	{
		public override void WriteJson(JsonWriter writer, Failing? value, JsonSerializer serializer) =>
			throw new InvalidOperationException("write failed");

		public override Failing ReadJson(JsonReader reader, Type objectType, Failing? existingValue, bool hasExistingValue,
			JsonSerializer serializer) => throw new NotSupportedException();
	}

	[Test]
	public void A_failed_write_leaves_the_serializer_able_to_run_callbacks()
	{
		var serializer = JsonSerializer.CreateDefault(UnitySerializationSettings.Instance);
		var converterCount = serializer.Converters.Count;

		Assert.Catch(() => serializer.Serialize(new StringWriter(), new FailingCallback()));

		Assert.That(serializer.Converters.Count, Is.EqualTo(converterCount), "The invoker must be restored after a failed write.");
		var json = new StringWriter();
		serializer.Serialize(json, new Callback());
		Assert.That(json.ToString(), Is.EqualTo(JsonConvert.SerializeObject(new Callback(), UnitySerializationSettings.Instance)));
	}
}
