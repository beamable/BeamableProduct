// Controlled Unity scheduling, PlayerPrefs, logging, pool and JSON adapters for the .NET harness.
// The linked batch/tracker/dependency-provider sources are the actual SDK implementation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace UnityEngine
{
	public sealed class WaitForSeconds { public WaitForSeconds(float seconds) { } }
	public static class Application
	{
		public static string persistentDataPath = System.IO.Path.GetTempPath();
		public static string identifier = "com.beamable.integration-tests";
	}
	public static class Debug
	{
		public static readonly List<string> Warnings = new List<string>();
		public static void LogWarning(object message) { Warnings.Add(message.ToString()); }
		public static void LogError(object message) { throw new Exception(message.ToString()); }
	}
	public static class PlayerPrefs
	{
		public static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
		public static int Writes;
		public static int Saves;
		public static bool FailSave;
		public static bool HasKey(string key) => Values.ContainsKey(key);
		public static string GetString(string key) => Values.TryGetValue(key, out string value) ? value : "";
		public static void SetString(string key, string value) { Writes++; Values[key] = value; }
		public static void DeleteKey(string key) { Values.Remove(key); }
		public static void Save() { Saves++; if (FailSave) { throw new System.IO.IOException("Synthetic legacy save failure."); } }
		public static void Reset() { Values.Clear(); Writes = 0; Saves = 0; FailSave = false; }
	}
}

namespace Beamable.Coroutines
{
	public sealed class CoroutineService
	{
		public readonly List<IEnumerator> Active = new List<IEnumerator>();
		public int Starts;
		public void StartCoroutine(IEnumerator coroutine)
		{
			Starts++; Active.Add(coroutine);
			coroutine.MoveNext(); // Unity executes the first heartbeat immediately, before returning.
		}
		public void StopCoroutine(IEnumerator coroutine) { Active.Remove(coroutine); }
		public void Tick() { foreach (IEnumerator coroutine in Active.ToArray()) { coroutine.MoveNext(); } }
	}
	public static class Yielders { public static UnityEngine.WaitForSeconds Seconds(float seconds) => new UnityEngine.WaitForSeconds(seconds); }
}

namespace Beamable.Common.Api { internal sealed class NamespaceMarker { } }
namespace Beamable.Common.Spew { public static class AnalyticsLogger { public static void LogFormat(string format, params object[] values) { } } }
namespace Beamable.Common
{
	public static class BeamableLogger
	{
		public static void LogError(object value) { throw new Exception(value.ToString()); }
		public static void LogException(Exception exception) { throw exception; }
	}
}
namespace Beamable
{
	public sealed class BeamContextSystemAttribute : Attribute { }
}
namespace Beamable.Common.Dependencies
{
	public sealed class RegisterBeamableDependenciesAttribute : Attribute { }
}
namespace Beamable.Pooling
{
	public sealed class StringBuilderPool
	{
		public static readonly StringBuilderPool StaticPool = new StringBuilderPool();
		public Lease Spawn() => new Lease();
		public sealed class Lease : IDisposable
		{
			public readonly StringBuilder Builder = new StringBuilder();
			public void Dispose() { }
		}
	}
}

namespace Beamable.Serialization.SmallerJSON
{
	public static class Json
	{
		public static string Serialize(Dictionary<string, object> value, StringBuilder builder) => JsonSerializer.Serialize(value);
		public static object Deserialize(string value) { using var document = JsonDocument.Parse(value); return Convert(document.RootElement); }
		private static object Convert(JsonElement value)
		{
			switch (value.ValueKind)
			{
				case JsonValueKind.Object: return value.EnumerateObject().ToDictionary(p => p.Name, p => Convert(p.Value));
				case JsonValueKind.Array: return value.EnumerateArray().Select(Convert).ToList();
				case JsonValueKind.String: return value.GetString();
				case JsonValueKind.Number: return value.GetInt64();
				case JsonValueKind.Null: return null;
				default: throw new FormatException();
			}
		}
	}
}
namespace Beamable.Serialization
{
	public static class JsonSerializable
	{
		public interface ISerializable { void Serialize(IStreamSerializer stream); }
		public interface IStreamSerializer
		{
			void Serialize(string key, ref int value);
			void Serialize(string key, ref long value);
			void Serialize(string key, ref string value);
			void SerializeList<T>(string key, ref List<T> values) where T : ISerializable;
		}
		public static Dictionary<string, object> Serialize(ISerializable value)
		{
			var stream = new Stream(new Dictionary<string, object>(), true); value.Serialize(stream); return stream.Values;
		}
		public static void Deserialize(ISerializable value, Dictionary<string, object> values) { value.Serialize(new Stream(values, false)); }
		private sealed class Stream : IStreamSerializer
		{
			internal readonly Dictionary<string, object> Values;
			private readonly bool _saving;
			internal Stream(Dictionary<string, object> values, bool saving) { Values = values; _saving = saving; }
			public void Serialize(string key, ref int value) { if (_saving) { Values[key] = value; } else { value = System.Convert.ToInt32(Values[key]); } }
			public void Serialize(string key, ref long value) { if (_saving) { Values[key] = value; } else { value = System.Convert.ToInt64(Values[key]); } }
			public void Serialize(string key, ref string value) { if (_saving) { Values[key] = value; } else { value = (string)Values[key]; } }
			public void SerializeList<T>(string key, ref List<T> values) where T : ISerializable
			{
				if (_saving) { Values[key] = values.Select(value => (object)JsonSerializable.Serialize(value)).ToList(); }
				else
				{
					values = ((List<object>)Values[key]).Select(item =>
					{
						T value = Activator.CreateInstance<T>(); JsonSerializable.Deserialize(value, (Dictionary<string, object>)item); return value;
					}).ToList();
				}
			}
		}
	}
}
