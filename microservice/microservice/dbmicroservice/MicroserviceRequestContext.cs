using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json;

namespace Beamable.Server;

public class MicroserviceRequestContext : RequestContext
{
	public MicroserviceRequestContext(string cid, string pid, long id, int status, long userId, string path, string method, string body, HashSet<string> scopes = null, IDictionary<string, string> headers = null) : base(cid, pid, id, status, userId, path, method, body, scopes, headers)
	{
	}

	public MicroserviceRequestContext(string cid, string pid) : base(cid, pid)
	{
	}

	public JsonElement BodyElement { get; set; }
	public JsonElement HeaderElement { get; set; }
	private string _body;
	private RequestHeaders _headers;

	public override string Body
	{
		get
		{
			if (_body != null)
			{
				return _body;
			}
			_body = BodyElement.GetRawText();
			BodyElement = default;

			return _body;
		}
	}

	public override RequestHeaders Headers
	{
		get
		{
			if (_headers != null)
			{
				return _headers;
			}
			_headers = new RequestHeaders(JsonConvert.DeserializeObject<Dictionary<string, string>>(HeaderElement.GetRawText()));
			HeaderElement = default;
			return _headers;
		}
	}
}
