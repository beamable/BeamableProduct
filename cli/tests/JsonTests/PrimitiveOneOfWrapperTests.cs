using Beamable.Common.Content;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace tests.JsonTests;

// These tests exercise the runtime shape that `BuildPrimitiveOneOfWrapper` emits for a primitive
// oneOf (e.g. StatsValue = string | long | double | bool | array). The wrapper does NOT implement
// JsonSerializable.ISerializable — it carries IRawJsonProvider and a SerializeAt helper that the
// parent invokes directly. The class body below is a hand-rolled copy of the generator's output,
// so the assertions verify the runtime contract the generator depends on.
public class PrimitiveOneOfWrapperTests
{
	public class StatsValue : IRawJsonProvider
	{
		public OptionalString StringValue;
		public OptionalLong IntValue;
		public OptionalDouble DoubleValue;
		public OptionalBool BoolValue;
		public OptionalArrayOfStatsValue ArrayValue;

		public object ToRawValue()
		{
			if (StringValue != null && StringValue.HasValue) return StringValue.Value;
			if (IntValue != null && IntValue.HasValue) return IntValue.Value;
			if (DoubleValue != null && DoubleValue.HasValue) return DoubleValue.Value;
			if (BoolValue != null && BoolValue.HasValue) return BoolValue.Value;
			if (ArrayValue != null && ArrayValue.HasValue)
			{
				var raw = new List<object>();
				foreach (var item in ArrayValue.Value) raw.Add(item != null ? item.ToRawValue() : null);
				return raw;
			}
			return null;
		}

		public string ToJson()
		{
			var raw = ToRawValue();
			if (raw == null) return "null";
			var sb = new StringBuilder();
			Json.Serialize(raw, sb);
			return sb.ToString();
		}

		public void SerializeAt(JsonSerializable.IStreamSerializer s, string key)
		{
			if (s.isSaving)
			{
				s.SetValue(key, ToRawValue());
				return;
			}
			if (!s.HasKey(key)) return;
			AssignFromRaw(this, s.GetValue(key));
		}

		private static void AssignFromRaw(StatsValue target, object raw)
		{
			if (raw == null) return;
			if (raw is string vStr) { target.StringValue = new OptionalString(vStr); return; }
			if (raw is bool vBool) { target.BoolValue = new OptionalBool(vBool); return; }
			if (raw is long vLong) { target.IntValue = new OptionalLong(vLong); return; }
			if (raw is int vInt) { target.IntValue = new OptionalLong(vInt); return; }
			if (raw is double vDouble) { target.DoubleValue = vDouble; return; }
			if (raw is float vFloat) { target.DoubleValue = (double)vFloat; return; }
			if (raw is IList vList)
			{
				var built = new List<StatsValue>();
				foreach (var elem in vList)
				{
					var inner = new StatsValue();
					AssignFromRaw(inner, elem);
					built.Add(inner);
				}
				target.ArrayValue = new OptionalArrayOfStatsValue(built.ToArray());
			}
		}
	}

	public class OptionalArrayOfStatsValue : OptionalArray<StatsValue>
	{
		public OptionalArrayOfStatsValue() { }
		public OptionalArrayOfStatsValue(StatsValue[] value)
		{
			HasValue = true;
			Value = value;
		}
	}

	// A container that owns a StatsValue field — this is how the generator emits parent models
	// that reference a primitive-oneOf schema (it calls field.SerializeAt(s, "key") instead of
	// the default s.Serialize(key, ref field) path).
	public class PlayerStat : JsonSerializable.ISerializable
	{
		public string key;
		public StatsValue value = new StatsValue();

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize("key", ref key);
			value.SerializeAt(s, "value");
		}
	}

	[Test]
	public void StringBranch_RoundTrip()
	{
		var stat = new PlayerStat
		{
			key = "displayName",
			value = new StatsValue { StringValue = new OptionalString("Alice") }
		};

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""displayName"",""value"":""Alice""}"));

		var clone = JsonSerializable.FromJson<PlayerStat>(json);
		Assert.That(clone.value.StringValue?.HasValue, Is.True);
		Assert.That(clone.value.StringValue.Value, Is.EqualTo("Alice"));
		Assert.That(clone.value.IntValue?.HasValue ?? false, Is.False);
		Assert.That(clone.value.BoolValue?.HasValue ?? false, Is.False);
	}

	[Test]
	public void IntBranch_RoundTrip()
	{
		var stat = new PlayerStat
		{
			key = "score",
			value = new StatsValue { IntValue = new OptionalLong(42) }
		};

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""score"",""value"":42}"));

		var clone = JsonSerializable.FromJson<PlayerStat>(json);
		Assert.That(clone.value.IntValue?.HasValue, Is.True);
		Assert.That(clone.value.IntValue.Value, Is.EqualTo(42L));
	}

	[Test]
	public void DoubleBranch_RoundTrip()
	{
		var stat = new PlayerStat
		{
			key = "ratio",
			value = new StatsValue { DoubleValue = 0.5 }
		};

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""ratio"",""value"":0.5}"));

		var clone = JsonSerializable.FromJson<PlayerStat>(json);
		Assert.That(clone.value.DoubleValue?.HasValue, Is.True);
		Assert.That(clone.value.DoubleValue.Value, Is.EqualTo(0.5));
	}

	[Test]
	public void BoolBranch_RoundTrip()
	{
		var stat = new PlayerStat
		{
			key = "isActive",
			value = new StatsValue { BoolValue = new OptionalBool(true) }
		};

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""isActive"",""value"":true}"));

		var clone = JsonSerializable.FromJson<PlayerStat>(json);
		Assert.That(clone.value.BoolValue?.HasValue, Is.True);
		Assert.That(clone.value.BoolValue.Value, Is.True);
	}

	[Test]
	public void ArrayBranch_RoundTripWithMixedPrimitives()
	{
		// Recursive array branch — each element is itself a StatsValue picking whichever primitive
		// branch matches the JSON token kind on the way back in.
		var stat = new PlayerStat
		{
			key = "tags",
			value = new StatsValue
			{
				ArrayValue = new OptionalArrayOfStatsValue(new[]
				{
					new StatsValue { StringValue = new OptionalString("alpha") },
					new StatsValue { IntValue = new OptionalLong(7) },
					new StatsValue { BoolValue = new OptionalBool(false) }
				})
			}
		};

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""tags"",""value"":[""alpha"",7,false]}"));

		var clone = JsonSerializable.FromJson<PlayerStat>(json);
		Assert.That(clone.value.ArrayValue?.HasValue, Is.True);
		Assert.That(clone.value.ArrayValue.Value, Has.Length.EqualTo(3));
		Assert.That(clone.value.ArrayValue.Value[0].StringValue.Value, Is.EqualTo("alpha"));
		Assert.That(clone.value.ArrayValue.Value[1].IntValue.Value, Is.EqualTo(7L));
		Assert.That(clone.value.ArrayValue.Value[2].BoolValue.Value, Is.False);
	}

	[Test]
	public void NoBranchSet_SerializesAsNull()
	{
		var stat = new PlayerStat { key = "empty", value = new StatsValue() };

		var json = JsonSerializable.ToJson(stat);
		Assert.That(json, Is.EqualTo(@"{""key"":""empty"",""value"":null}"));
	}

	// Regression: CliRequester.RequestJson goes through JsonSerializable.Serialize(obj) which
	// uses SaveStream — and SaveStream.SerializeNestedJson throws NotImplementedException.
	// The wrapper must serialize via SetValue, not SerializeNestedJson, so request bodies that
	// contain a primitive-oneOf field don't crash. This test exercises the SaveStream path.
	[Test]
	public void SaveStream_StoresRawValueInDictionary()
	{
		var stat = new PlayerStat
		{
			key = "score",
			value = new StatsValue { IntValue = new OptionalLong(42) }
		};

		var dict = JsonSerializable.Serialize(stat);
		Assert.That(dict["key"], Is.EqualTo("score"));
		Assert.That(dict["value"], Is.EqualTo(42L));
	}

	[Test]
	public void SaveStream_ArrayBranchStoresFlattenedList()
	{
		var stat = new PlayerStat
		{
			key = "tags",
			value = new StatsValue
			{
				ArrayValue = new OptionalArrayOfStatsValue(new[]
				{
					new StatsValue { StringValue = new OptionalString("alpha") },
					new StatsValue { IntValue = new OptionalLong(7) }
				})
			}
		};

		var dict = JsonSerializable.Serialize(stat);
		Assert.That(dict["value"], Is.InstanceOf<IList>());
		var list = (IList)dict["value"];
		Assert.That(list[0], Is.EqualTo("alpha"));
		Assert.That(list[1], Is.EqualTo(7L));
	}
}
