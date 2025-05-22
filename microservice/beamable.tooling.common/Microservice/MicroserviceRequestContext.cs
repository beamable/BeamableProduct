using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace Beamable.Server;

/// <summary>
/// Represents a microservice-specific request context.
/// </summary>
public class MicroserviceRequestContext : RequestContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MicroserviceRequestContext"/> class.
	/// </summary>
	public MicroserviceRequestContext(string cid, string pid, long id, int status, long userId, string path, string method, string body, HashSet<string> scopes = null, IDictionary<string, string> headers = null) : base(cid, pid, id, status, userId, path, method, body, scopes, headers)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MicroserviceRequestContext"/> class with minimal information.
	/// </summary>
	public MicroserviceRequestContext(string cid, string pid) : base(cid, pid)
	{
	}

	/// <summary>
	/// The cancellation token associated with the request context.
	/// </summary>
	public CancellationToken CancellationToken { get; set; }
	
	/// <summary>
	/// The JSON element representing the request body.
	/// </summary>
	public JsonElement BodyElement { get; set; }
	/// <summary>
	/// The JSON element representing the request headers.
	/// </summary>
	public JsonElement HeaderElement { get; set; }

	/// <summary>
	/// Gets a value indicating whether the cancellation token is canceled.
	/// </summary>
	public override bool IsCancelled => CancellationToken.IsCancellationRequested;
	
	/// <summary>
	/// Gets the request body as a string.
	/// </summary>
	public override string Body
	{
		get
		{
			if (_body != null)
			{
				return _body;
			}
			
			_body = BodyElement.ToString();
			return _body;
		}
	}

	/// <summary>
	/// Gets the request headers.
	/// </summary>
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

	private string _body;
	private RequestHeaders _headers;
	
	/// <summary>
	/// Throws a cancellation exception if the cancellation token is canceled.
	/// </summary>
	public override void ThrowIfCancelled()
	{
		CancellationToken.ThrowIfCancellationRequested();
	}
}
