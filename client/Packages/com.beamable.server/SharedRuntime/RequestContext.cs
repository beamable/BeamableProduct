using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api;

namespace Beamable.Server
{
	/// <summary>
	/// This type defines the %Microservice %RequestContext.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Server.Microservice script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	public class RequestContext : IUserContext
	{
		private readonly long _userId;

		public string Cid
		{
			get;
		}

		public string Pid
		{
			get;
		}

		public long Id
		{
			get;
		}

		public int Status
		{
			get;
		}

		public long UserId
		{
			get
			{
				CheckEmptyUser();
				return _userId;
			}
		}

		public string Path
		{
			get;
		}

		public string Method
		{
			get;
		}

		public string Body
		{
			get;
		}

		public HashSet<string> Scopes
		{
			get;
		}

		public bool HasScopes(IEnumerable<string> scopes) => HasScopes(scopes.ToArray());

		public bool HasScopes(params string[] scopes)
		{
			if (Scopes.Contains("*")) return true;
			var missingCount = scopes.Count(required => !Scopes.Contains(required));
			return missingCount == 0;
		}

		public void CheckAdmin()
		{
			if (!HasScopes("*"))
				throw new MissingScopesException(Scopes);
		}

		private void CheckEmptyUser()
		{
			if (IsInvalidUser)
				throw new ArgumentException(
					$"This is an empty UserContext. You cannot make user-specific requests with it. " +
					$"Please call Microservice.AssumeUser and use the returning RequestHandlerData to make the request you are attempting to make.");
		}

		public void RequireScopes(params string[] scopes)
		{
			if (!HasScopes(scopes))
				throw new MissingScopesException(Scopes);
		}

		public bool IsEvent => Path?.StartsWith("event/") ?? false;

		/// <summary>
		/// Informs us whether or not this request context is pointing to any valid user (userId >= 0). If it isn't, customer must call <see cref="Microservice.AssumeUser"/> in 
		/// custom Microservice's code before making requests that access player-specific data.
		/// </summary>
		public bool IsInvalidUser => _userId < 0;

		public RequestContext(string cid,
		                      string pid,
		                      long id,
		                      int status,
		                      long userId,
		                      string path,
		                      string method,
		                      string body,
		                      HashSet<string> scopes = null)
		{
			Cid = cid;
			Pid = pid;
			Id = id;
			_userId = userId;
			Path = path;
			Method = method;
			Status = status;
			Body = body;
			Scopes = scopes ?? new HashSet<string>();
			Scopes.RemoveWhere(string.IsNullOrEmpty);
		}

		public RequestContext(string cid, string pid)
		{
			Cid = cid;
			Pid = pid;
			Id = -1;
			_userId = -1;
			Path = "";
			Method = "";
			Status = 0;
			Body = "";
			Scopes = new HashSet<string>();
		}
	}
}
