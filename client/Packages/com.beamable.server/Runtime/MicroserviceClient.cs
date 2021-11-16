using System.Collections;
using System.Collections.Generic;
using System.Text;
using Beamable.Common;
using Beamable.Common.Api;
using UnityEngine;
using Beamable.Serialization.SmallerJSON;
using System;

namespace Beamable.Server
{
	public class MicroserviceClient
	{
		[System.Serializable]
		public class ResponseObject
		{
			public string payload;
		}

		[System.Serializable]
		public class RequestObject
		{
			public string payload;
		}

		private static readonly StringBuilder Builder = new StringBuilder();

		protected string _prefix;

		public static string SerializeArgument<T>(T arg)
		{
			// JSONUtility will serialize objects correctly, but doesn't handle primitives well.
			if (arg == null)
			{
				return "null";
			}

			switch (arg)
			{
				case string prim:
					return Json.IsValidJson(prim) ? "[" + prim + "]" : "\"" + prim + "\"";
				case IDictionary dictionary:
					return Json.Serialize(dictionary, Builder);
				case IEnumerable enumerable:
				{
					var output = new List<string>();
					foreach (var elem in enumerable)
					{
						output.Add(SerializeArgument(elem));
					}

					var outputJson = "[" + string.Join(",", output) + "]";
					return outputJson;
				}
				case bool prim:
					return prim ? "true" : "false";
				case long prim:
					return prim.ToString();
				case double prim:
					return prim.ToString();
				case float prim:
					return prim.ToString();
				case int prim:
					return prim.ToString();
				case Vector2Int prim:
					return JsonUtility.ToJson(new Vector2IntEx(prim));
				case Vector3Int prim:
					return JsonUtility.ToJson(new Vector3IntEx(prim));
			}

			return JsonUtility.ToJson(arg);
		}

		public static T DeserializeResult<T>(string json)
		{
			var type = typeof(T);
			var defaultInstance = default(T);

			if (typeof(Unit).IsAssignableFrom(type))
			{
				return (T)(object)PromiseBase.Unit;
			}

			if (type == typeof(string))
			{
				return (T)(object)json;
			}

			switch (defaultInstance)
			{
				case float _:
					return (T)(object)float.Parse(json);
				case long _:
					return (T)(object)long.Parse(json);
				case double _:
					return (T)(object)double.Parse(json);
				case bool _:
					return (T)(object)bool.Parse(json);
				case int _:
					return (T)(object)int.Parse(json);
				case Vector2Int _:
					return (T)(object)Vector2IntEx.DeserializeToVector2(json);
				case Vector3Int _:
					return (T)(object)Vector3IntEx.DeserializeToVector3(json);
			}

			if (typeof(IDictionary).IsAssignableFrom(type))
			{
				var arrayDict = (ArrayDict)Json.Deserialize(json);
				object result = default(T);
				if (typeof(Dictionary<string, string>) == type)
				{
					result = ConvertArrayDictToDictionary<string>(arrayDict);
				}
				else if (typeof(Dictionary<string, double>) == type)
				{
					result = ConvertArrayDictToDictionary<double>(arrayDict);
				}
				else if (typeof(Dictionary<string, float>) == type)
				{
					result = ConvertArrayDictToDictionary<float>(arrayDict);
				}
				else if (typeof(Dictionary<string, int>) == type)
				{
					result = ConvertArrayDictToDictionary<int>(arrayDict);
				}
				else if (typeof(Dictionary<string, bool>) == type)
				{
					result = ConvertArrayDictToDictionary<bool>(arrayDict);
				}
				else if (typeof(Dictionary<string, long>) == type)
				{
					result = ConvertArrayDictToDictionary<long>(arrayDict);
				}
				else
				{
					Debug.LogWarning($"Cannot convert json to Dictionary:\n{json}");
				}

				return (T)result;
			}

			if (json.StartsWith("[") && json.EndsWith("]"))
			{
				json = $"{{\"items\": {json}}}";
				var wrapped = JsonUtility.FromJson<JsonUtilityWrappedList<T>>(json);
				return wrapped.items;
			}

			return JsonUtility.FromJson<T>(json);
		}

		public static Dictionary<string, T> ConvertArrayDictToDictionary<T>(ArrayDict arrayDict)
		{
			var dictionary = new Dictionary<string, T>(arrayDict.Count);
			if (typeof(T) == typeof(int))
			{
				foreach (var pair in arrayDict)
				{
					var intValue =  (int)((long)pair.Value % Int32.MaxValue);
					dictionary.Add(pair.Key, (T)(object)intValue);
				}
			}
			else if (typeof(T) == typeof(float))
			{
				foreach (var pair in arrayDict)
				{
					var floatValue = (float)((double)pair.Value);
					dictionary.Add(pair.Key, (T)(object)floatValue);
				}
			}
			else
			{
				foreach (var pair in arrayDict)
				{
					dictionary.Add(pair.Key, (T)pair.Value);
				}
			}

			return dictionary;
		}

		private class JsonUtilityWrappedList<TList>
		{
			public TList items = default;
		}

		protected string CreateUrl(string cid, string pid, string serviceName, string endpoint)
		{
			var prefix = _prefix ?? (_prefix = MicroserviceIndividualization.GetServicePrefix(serviceName));
			var path = $"{prefix}micro_{serviceName}/{endpoint}";
			var url = $"/basic/{cid}.{pid}.{path}";
			return url;
		}

		protected async Promise<T> Request<T>(string serviceName, string endpoint, string[] serializedFields)
		{
			Debug.Log($"Client called {endpoint} with {serializedFields.Length} arguments");
			var argArray = "[ " + string.Join(",", serializedFields) + " ]";
			Debug.Log(argArray);

			T Parser(string json)
			{
				// TODO: Remove this in 0.13.0
				if (!(json?.StartsWith("{\"payload\":\"") ?? false)) return DeserializeResult<T>(json);

#pragma warning disable 618
				Debug.LogWarning(
					$"Using legacy payload string. Redeploy the {serviceName} service without the UseLegacySerialization setting.");
#pragma warning restore 618
				var responseObject = DeserializeResult<ResponseObject>(json);
				var result = DeserializeResult<T>(responseObject.payload);
				return result;
			}

			var b = await API.Instance;
			var requester = b.Requester;
			var url = CreateUrl(b.Token.Cid, b.Token.Pid, serviceName, endpoint);
			var req = new RequestObject {payload = argArray};
			Debug.Log($"Sending Request uri=[{url}]");
			return await requester.Request<T>(Method.POST, url, req, parser: Parser);
		}

		protected static string CreateEndpointPrefix(string serviceName)
		{
#if UNITY_EDITOR

#endif

			return serviceName;
		}
	}
}
