// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Beamable.Serialization
{
	public enum DiffType
	{
		Added, Changed, Removed
	}
	
	[DebuggerDisplay("{GetDescription()}")]
	public class DiffChange : JsonSerializable.ISerializable
	{
		public string jsonPath;
		public DiffType type;
		public string currentValue;
		public string nextValue;

		public string GetDescription()
		{
			switch (type)
			{
				case DiffType.Added:
					return $"ADDED {jsonPath} as {nextValue}";
				case DiffType.Removed:
					return $"REMOVED {jsonPath} as {currentValue}";
				case DiffType.Changed:
					return $"CHANGED {jsonPath} from {currentValue} to {nextValue}";
				default:
					throw new NotImplementedException("invalid diff type");
			}
		}

		public bool TryGetNextBooleanValue(out bool isTruthy)
		{
			return bool.TryParse(nextValue, out isTruthy);
		}

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize(nameof(jsonPath), ref jsonPath);
			s.Serialize(nameof(currentValue), ref currentValue);
			s.Serialize(nameof(nextValue), ref nextValue);
			s.SerializeEnum(nameof(type), ref type);
		}
	}

	public class DiffChangeSummary : JsonSerializable.ISerializable
	{
		public List<DiffChange> changes = new List<DiffChange>();

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.SerializeList(nameof(changes), ref changes);
		}
	}
	
	public class DiffStream
	{
		
		public static DiffChangeSummary FindChanges<T>(T current, T next) 
			where T : JsonSerializable.ISerializable
		{
			var currentStream = new JsonPathValueStream();
			current.Serialize(currentStream);

			var nextStream = new JsonPathValueStream();
			next.Serialize(nextStream);

			var summary = new DiffChangeSummary();

			foreach (var kvp in currentStream.jsonPathToValue)
			{
				if (!nextStream.TryGetJsonPathValue(kvp.Key, out var nextValue))
				{
					// the value has been removed!
					summary.changes.Add(new DiffChange
					{
						currentValue = kvp.Value,
						nextValue = null,
						jsonPath = kvp.Key,
						type = DiffType.Removed
					});
				} else {
					// the value needs to be diff checked!
					var currentValue = kvp.Value;

					if (!string.Equals(currentValue, nextValue, StringComparison.InvariantCulture))
					{
						summary.changes.Add(new DiffChange
						{
							currentValue = currentValue,
							nextValue = nextValue,
							jsonPath = kvp.Key,
							type = DiffType.Changed
						});
					}

				}
				
				nextStream.jsonPathToValue.Remove(kvp.Key);
			}

			foreach (var kvp in nextStream.jsonPathToValue)
			{
				// the value has been added
				summary.changes.Add(new DiffChange
				{
					currentValue = null,
					nextValue = kvp.Value,
					jsonPath = kvp.Key,
					type = DiffType.Added
				});
			}
			
			
			return summary;
		}
		
	}

	public class JsonPathValueStream : JsonSerializable.IStreamSerializer
	{
		public Dictionary<string, string> jsonPathToValue = new Dictionary<string, string>();

		public bool TryGetJsonPathValue(string jsonPath, out string stringifiedValue)
		{
			return jsonPathToValue.TryGetValue(jsonPath, out stringifiedValue);
		}
		
		public bool isSaving { get; }
		public bool isLoading { get; }
		public object GetValue(string key)
		{
			throw new NotImplementedException();
		}

		public void SetValue(string key, object value)
		{
			throw new NotImplementedException();
		}

		public bool HasKey(string key)
		{
			return jsonPathToValue.ContainsKey(key);
		}

		public JsonSerializable.ListMode Mode { get; }
		public bool SerializeNestedJson(string key, ref JsonString jsonString)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref IDictionary<string, object> target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref bool target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref bool? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref int target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref int? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref long target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref long? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref ulong target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref ulong? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref float target)
		{
			jsonPathToValue[key] = target.ToString(CultureInfo.InvariantCulture);
			return true;
		}

		public bool Serialize(string key, ref float? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref double target)
		{
			jsonPathToValue[key] = target.ToString(CultureInfo.InvariantCulture);
			return true;
		}

		public bool Serialize(string key, ref double? target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref string target)
		{
			jsonPathToValue[key] = target;
			return true;
		}

		public bool Serialize(string key, ref Guid target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref StringBuilder target)
		{
			jsonPathToValue[key] = target.ToString();
			return true;
		}

		public bool Serialize(string key, ref DateTime target, params string[] formats)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Rect target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Vector2 target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Vector3 target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Vector4 target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Color target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Quaternion target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize(string key, ref Gradient target)
		{
			throw new NotImplementedException();
		}

		public bool Serialize<T>(string key, ref T value) where T : JsonSerializable.ISerializable
		{
			if (value == null)
			{
				return true;
			}
			
			var subStream = new JsonPathValueStream();
			value.Serialize(subStream);

			foreach (var kvp in subStream.jsonPathToValue)
			{
				jsonPathToValue[key + "." + kvp.Key] = kvp.Value;
			}

			return true;
		}

		public bool SerializeInline<T>(string key, ref T value) where T : JsonSerializable.ISerializable
		{
			throw new NotImplementedException();
		}

		public bool SerializeList<TList>(string key, ref TList value) where TList : IList, new()
		{
			throw new NotImplementedException();
		}

		public bool SerializeKnownList<TElem>(string key, ref List<TElem> value) where TElem : JsonSerializable.ISerializable, new()
		{
			throw new NotImplementedException();
		}

		public bool SerializeArray<T>(string key, ref T[] value)
		{
			if (!typeof(JsonSerializable.ISerializable).IsAssignableFrom(typeof(T)))
			{
				throw new NotImplementedException("cannot do a diff check with this type");
			}
			
			if (value == null)
			{
				return true;
			}

			for (var i = 0; i < value.Length; i++)
			{
				var subStream = new JsonPathValueStream();
				var serializeValue = (JsonSerializable.ISerializable)value[i];
				serializeValue.Serialize(subStream);

				foreach (var kvp in subStream.jsonPathToValue)
				{
					jsonPathToValue[key + $"[{i}]." + kvp.Key] = kvp.Value;
				}
			}

			return true;
		}

		public bool SerializeDictionary<T>(string key, ref Dictionary<string, T> target)
		{
			throw new NotImplementedException();
		}

		public bool SerializeDictionary<TDict, TElem>(string key, ref TDict target) where TDict : IDictionary<string, TElem>, new()
		{
			throw new NotImplementedException();
		}

		public bool SerializeILL<T>(string key, ref LinkedList<T> list) where T : ClassPool<T>, new()
		{
			throw new NotImplementedException();
		}
	}
}
