using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using System;

namespace Beamable.Server.Api
{
	public class ServerAuthApi : AuthApi, IMicroserviceAuthApi
	{
		public const string BASIC_SERVICE = "/basic/accounts";
		public const string OBJECT_SERVICE = "/object/accounts";

		public IBeamableRequester Requester { get; }
		public RequestContext Context { get; }

		public ServerAuthApi(IBeamableRequester requester, RequestContext context) : base(requester)
		{
			Requester = requester;
			Context = context;
		}

		public Promise<User> GetUser(long userId)
		{
			return Requester.Request<User>(Method.GET, $"{BASIC_SERVICE}", new GetUserRequest
			{
				gamerTag = userId
			});
		}

		public override Promise<User> GetUser(TokenResponse token)
		{
			throw new NotImplementedException("This version of GetUser is not supported in the Microservice environment!\n" +
											  $"To get User data, please use {nameof(IMicroserviceAuthApi)}.{nameof(IMicroserviceAuthApi.GetUser)}(userId) instead.\n" +
											  $"Or, to make calls from the Microservice on behalf of a user with a given Id, " +
											  $"use Microservice.AssumeUser(userId) and use the returned {nameof(RequestHandlerData)}.{nameof(RequestHandlerData.Services)}.");
		}

		private class GetUserRequest
		{
			public long gamerTag;
		}
	}
}
