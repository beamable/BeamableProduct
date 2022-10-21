using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Editor.Environment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class BeamableEditorWebRequester : IEditorHttpRequester
{
	public Promise<T> ManualRequest<T>(Method method,
	                                   string url,
	                                   object body = null,
	                                   Dictionary<string, string> headers = null,
	                                   string contentType = "application/json",
	                                   Func<string, T> parser = null)
	{
		if (body != null)
			throw new NotImplementedException();
		
		var result = new Promise<T>();
		var request = WebRequest.Create(url);
		
		if (headers != null)
		{
			var headerCollection = new WebHeaderCollection();
			
			foreach (var header in headers)
			{
				headerCollection.Add(header.Key, header.Value);
			}
			
			request.Headers = headerCollection;
		}
		
		var response = request.GetResponseAsync();
		
		response.ToPromise().Then(value =>
		{
			try
			{
				using (var streamReader = new StreamReader(value.GetResponseStream()))
				{
					var responsePayload = streamReader.ReadToEnd();

					T parsedResult = parser == null
						? JsonUtility.FromJson<T>(responsePayload)
						: parser(responsePayload);

					result.CompleteSuccess(parsedResult);
				}
			}
			catch (Exception ex)
			{
				result.CompleteError(ex);
			}
		});
		
		return result;
	}
}
