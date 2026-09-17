using System.Collections.Generic;
using beamable.tooling.common.Microservice;

namespace Beamable.Server
{
	/// <summary>
	/// The scope-neutral surface of a microservice request: the request envelope (id, status, path, method,
	/// body, headers, scopes) plus the customer id and scope/permission helpers. It deliberately excludes
	/// realm/player identity (<c>Pid</c>, <c>UserId</c>) so it can be shared by both the realm
	/// <see cref="RequestContext"/> and the zone <see cref="ZonedRequestContext"/>.
	/// </summary>
	public interface IRequestContext
	{
		/// <summary>The customer id that this request originated from.</summary>
		string Cid { get; }

		/// <summary>
		/// The request id. Positive if user-generated, negative for internal Beamable framework messages.
		/// </summary>
		long Id { get; }

		/// <summary>The HTTP status code of the operation.</summary>
		int Status { get; }

		/// <summary>The relative url path for the request.</summary>
		string Path { get; }

		/// <summary>The HTTP method used to initiate this request, such as "POST" or "GET".</summary>
		string Method { get; }

		/// <summary>The raw body of this request.</summary>
		string Body { get; }

		/// <summary>Permissions associated with the caller of this request.</summary>
		HashSet<string> Scopes { get; }

		/// <summary>HTTP headers associated with this request.</summary>
		RequestHeaders Headers { get; }

		BeamActivity ActivityContext { get; set; }

		bool HasScopes(IEnumerable<string> scopes);
		bool HasScopes(params string[] scopes);
		void RequireScopes(params string[] scopes);
		void AssertAdmin();
		bool IsAdmin { get; }

		void ThrowIfCancelled();
		bool IsCancelled { get; }

		bool IsEvent { get; }
		string EventName { get; }
	}
}
