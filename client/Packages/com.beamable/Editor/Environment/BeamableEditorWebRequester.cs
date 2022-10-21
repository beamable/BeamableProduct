using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Editor.Environment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

public class BeamableEditorWebRequester : IEditorWebRequester
{
	public Promise<T> ManualRequest<T>(Method method,
	                                   string url,
	                                   object body = null,
	                                   Dictionary<string, string> headers = null,
	                                   string contentType = "application/json",
	                                   Func<string, T> parser = null)
	{
		
		var result = new Promise<T>();
		var request = WebRequest.Create(url);
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
