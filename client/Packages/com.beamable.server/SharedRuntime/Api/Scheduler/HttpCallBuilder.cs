// this file was copied from nuget package Beamable.Server.Common@0.0.0-PREVIEW.NIGHTLY-202407161549
// https://www.nuget.org/packages/Beamable.Server.Common/0.0.0-PREVIEW.NIGHTLY-202407161549

using Beamable.Common.Api;
using Beamable.Common.Scheduler;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Server
{

	public class HttpCallBuilder
	{

		public HttpAction Run(Method method,
							  string uri,
							  string contentType = "application/json",
							  Dictionary<string, string> headers = null)
		{
			var action = new HttpAction
			{
				method = method,
				body = "",
				contentType = contentType,
				uri = uri,
				headers = headers?.Select(kvp => new HttpCallHeader { key = kvp.Key, value = kvp.Value }).ToList() ??
						  new List<HttpCallHeader>()
			};
			return action;
		}

		public HttpAction Run<T>(Method method, string uri, T body, Dictionary<string, string> headers = null)
		{
			var action = new HttpAction
			{
				method = method,
				body = JsonUtility.ToJson(body),
				contentType = "application/json",
				uri = uri,
				headers = headers?.Select(kvp => new HttpCallHeader { key = kvp.Key, value = kvp.Value }).ToList() ??
						  new List<HttpCallHeader>()
			};
			return action;
		}

		public HttpAction Run(Method method,
							  string uri,
							  string body,
							  string contentType = "application/json",
							  Dictionary<string, string> headers = null)
		{
			var action = new HttpAction
			{
				method = method,
				body = body,
				contentType = contentType,
				uri = uri,
				headers = headers?.Select(kvp => new HttpCallHeader { key = kvp.Key, value = kvp.Value }).ToList() ??
						  new List<HttpCallHeader>()
			};
			return action;
		}
	}
}
