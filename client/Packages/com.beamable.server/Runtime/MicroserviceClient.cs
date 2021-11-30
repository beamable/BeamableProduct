using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Serialization;
using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Server
{
	[Serializable]
	public class MicroserviceClientDataWrapper<T> : ScriptableObject
	{
		public T Data;
	}

	public class MicroserviceClient
	{
		protected async Promise<T> Request<T>(string serviceName, string endpoint, string[] serializedFields)
		{
			return await MicroserviceClientHelper.Request<T>(serviceName, endpoint, serializedFields);
		}

		protected string SerializeArgument<T>(T arg) => MicroserviceClientHelper.SerializeArgument(arg);

		protected string CreateUrl(string cid, string pid, string serviceName, string endpoint)
		   => MicroserviceClientHelper.CreateUrl(cid, pid, serviceName, endpoint);
	}


	public static class MicroserviceClientHelper
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

		static string _prefix;

		public static void SetPrefix(string prefix) => _prefix = prefix;


		public static string SerializeArgument<T>(T arg)
		{
			try
			{
				return BeamableJson.Serialize(arg);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			return JsonUtility.ToJson(arg);
		}

		public static T DeserializeResult<T>(string json)
		{
			try
			{
				return BeamableJson.Deserialize<T>(json);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}

			return default;
		}

		public static string CreateUrl(string cid, string pid, string serviceName, string endpoint)
		{
			var prefix = _prefix ?? (_prefix = MicroserviceIndividualization.GetServicePrefix(serviceName));
			var path = $"{prefix}micro_{serviceName}/{endpoint}";
			var url = $"/basic/{cid}.{pid}.{path}";
			return url;
		}

		public static async Promise<T> Request<T>(string serviceName, string endpoint, string[] serializedFields)
		{
			Debug.Log($"Client called {endpoint} with {serializedFields.Length} arguments");
			var argArray = "[ " + string.Join(",", serializedFields) + " ]";
			Debug.Log(argArray);

			T Parser(string json)
			{
				// TODO: Remove this in 0.13.0
				if (!(json?.StartsWith("{\"payload\":\"") ?? false)) return DeserializeResult<T>(json);

#pragma warning disable 618
				Debug.LogWarning($"Using legacy payload string. Redeploy the {serviceName} service without the UseLegacySerialization setting.");
#pragma warning restore 618
				var responseObject = DeserializeResult<ResponseObject>(json);
				var result = DeserializeResult<T>(responseObject.payload);
				return result;
			}

			var b = await API.Instance;
			var requester = b.Requester;
			var url = CreateUrl(b.Token.Cid, b.Token.Pid, serviceName, endpoint);
			var req = new RequestObject {
				payload = argArray
			};
			Debug.Log($"Sending Request uri=[{url}]");
			return await requester.Request<T>(Method.POST, url, req, parser: Parser);
		}
	}
}
