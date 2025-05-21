// this file was copied from nuget package Beamable.Common@4.3.0
// https://www.nuget.org/packages/Beamable.Common/4.3.0

using Beamable.Common.Content;
using System.Text;

namespace Beamable.Serialization
{
	public class OptionalJsonString : Optional<JsonString>
	{
		public OptionalJsonString()
		{
			
		}

		public OptionalJsonString(JsonString value)
		{
			Value = value;
			HasValue = true;
		}
	}
	
	/// <summary>
	/// Represents a nested string of json that may be saved to json with the JsonSerializable type. 
	/// </summary>
	public class JsonString 
	{
		private string _json;
		private object _parsedValue;

		/// <summary>
		/// Lazily serializes the object
		/// </summary>
		public string Json
		{
			get
			{
				if (_json == null)
				{
					_json = SmallerJSON.Json.Serialize(_parsedValue, new StringBuilder());
				}

				return _json;
			}
		}

		/// <summary>
		/// Lazily deserialize the <see cref="Json"/> into an object.
		/// </summary>
		public object ValueObject
		{
			get
			{
				if (_parsedValue != null)
					return _parsedValue;

				_parsedValue = SmallerJSON.Json.Deserialize(_json);
				return _parsedValue;
			}
		}

		public T Deserialize<T>() where T : JsonSerializable.ISerializable, new()
		{
			return JsonSerializable.FromJson<T>(Json);
		}
		
		private JsonString()
		{
			
		}
		
		/// <summary>
		/// Construct a <see cref="JsonString"/> from a given json string.
		/// </summary>
		/// <param name="json">Must be valid json</param>
		/// <returns></returns>
		public static JsonString FromJson(string json)
		{
			return new JsonString
			{
				_json = json
			};
		}

		/// <summary>
		/// Construct a <see cref="JsonString"/> from an existing object. The json string will equal to the serialized json of the object. 
		/// </summary>
		/// <param name="value">must be non-null. </param>
		/// <returns></returns>
		public static JsonString FromValue(object value)
		{
			return new JsonString
			{
				_parsedValue = value
			};
		}

		public static JsonString FromSerializable<T>(T obj) where T : JsonSerializable.ISerializable, new()
		{
			return new JsonString { _json = JsonSerializable.ToJson(obj) };
		}
	}
}
