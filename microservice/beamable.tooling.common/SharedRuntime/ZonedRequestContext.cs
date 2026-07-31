using Beamable.Common;
using Beamable.Common.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using beamable.tooling.common.Microservice;

namespace Beamable.Server
{
	/// <summary>
	/// The request context for a zone (<c>cid.zid</c>) scoped service — the analog of
	/// <see cref="RequestContext"/> for services that run above realms. It carries the customer id and a
	/// <see cref="Zid"/> instead of a realm <c>Pid</c>, and has no player/<c>UserId</c> concept.
	///
	/// <para>
	/// Shares the scope-neutral surface with the realm context via <see cref="IRequestContext"/>. The lazy
	/// JSON body/header parsing that <c>MicroserviceRequestContext</c> provides is intentionally not shared
	/// yet (single-inheritance); a zone-flavored lazy variant can be extracted alongside a shared envelope
	/// helper as the zone request path matures.
	/// </para>
	/// </summary>
	public class ZonedRequestContext : IRequestContext
	{
		/// <summary>The customer id that this request originated from.</summary>
		public string Cid { get; }

		/// <summary>The zone id that this request is scoped to (the realm-less analog of <c>Pid</c>).</summary>
		public string Zid { get; }

		public long Id { get; }
		public int Status { get; }
		public BeamActivity ActivityContext { get; set; }
		public string Path { get; }
		public string Method { get; }
		public virtual string Body { get; }
		public HashSet<string> Scopes { get; }
		public virtual RequestHeaders Headers { get; }

		public ZonedRequestContext(string cid, string zid, long id, int status, string path, string method,
			string body, HashSet<string> scopes = null, IDictionary<string, string> headers = null)
		{
			Cid = cid;
			Zid = zid;
			Id = id;
			Status = status;
			Path = path;
			Method = method;
			Body = body;
			Scopes = scopes ?? new HashSet<string>();
			Scopes.RemoveWhere(string.IsNullOrEmpty);
			if (headers != null)
			{
				Headers = new RequestHeaders(headers);
			}
		}

		public ZonedRequestContext(string cid, string zid)
		{
			Cid = cid;
			Zid = zid;
			Id = -1;
			Path = "";
			Method = "";
			Status = 0;
			Body = "";
			Scopes = new HashSet<string>();
		}

		public bool HasScopes(IEnumerable<string> scopes) => HasScopes(scopes.ToArray());

		public bool HasScopes(params string[] scopes)
		{
			if (Scopes.Contains("*")) return true;
			return scopes.Count(required => !Scopes.Contains(required)) == 0;
		}

		public void RequireScopes(params string[] scopes)
		{
			if (!HasScopes(scopes))
				throw new MissingScopesException(Scopes);
		}

		public void AssertAdmin()
		{
			if (!IsAdmin)
				throw new MissingScopesException(Scopes);
		}

		public bool IsAdmin => HasScopes("*");

		public virtual void ThrowIfCancelled()
		{
			// no-op in the base; a lazy/cancellable variant can override.
		}

		public virtual bool IsCancelled { get; }

		public bool IsEvent => Path?.StartsWith("event/") ?? false;

		public string EventName => IsEvent
			? Path?.Substring("event/".Length)
			: null;
	}
}
