using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Beamable.Server;

public class MicroserviceRequestContext : RequestContext
{
	public MicroserviceRequestContext(string cid, string pid, long id, int status, long userId, string path, string method, string body, HashSet<string> scopes = null, IDictionary<string, string> headers = null) : base(cid, pid, id, status, userId, path, method, body, scopes, headers)
	{
	}

	public MicroserviceRequestContext(string cid, string pid) : base(cid, pid)
	{
	}

	public CancellationToken CancellationToken { get; set; }
	
	public JsonElement BodyElement { get; set; }
	public JsonElement HeaderElement { get; set; }
	private string _body;
	private RequestHeaders _headers;

	public override void ThrowIfCancelled()
	{
		CancellationToken.ThrowIfCancellationRequested();
	}

	public override bool IsCancelled => CancellationToken.IsCancellationRequested;

	public override string Body => _body ??= BodyElement.GetRawText();

	public override RequestHeaders Headers
	{
		get
		{
			if (_headers != null)
			{
				return _headers;
			}

			if (HeaderElement.ValueKind == JsonValueKind.Object)
			{
				_headers = new RequestHeaders(
					JsonConvert.DeserializeObject<Dictionary<string, string>>(HeaderElement.GetRawText()));
			}
			else
			{
				_headers = new RequestHeaders();
			}

			return _headers;
		}
	}
}
