using Beamable.Server;
using System.Collections.Generic;

namespace microserviceTests.microservice.Util;

public static class WebsocketRequestExtensions
{
	public static WebsocketRequest WithHeader(this WebsocketRequest req, string header, string value)
	{
		req.headers ??= new Dictionary<string, string>();
		req.headers[header] = value;
		return req;
	}
}
