using System;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;

namespace Beamable.Server.Api
{
   public class ServerAuthApi : AuthApi, IMicroserviceAuthApi
   {
      public const string BASIC_SERVICE = "/basic/accounts";
      public const string OBJECT_SERVICE = "/object/accounts";

      public RequestContext Context { get; }

      // BatchAccounts stat values arrive in their own JSON type, so they deserialize into object; keep a
      // date-shaped string stat as the string it is instead of letting Newtonsoft turn it into a DateTime.
      private static readonly JsonSerializerSettings BatchAccountsJson =
         new JsonSerializerSettings(UnitySerializationSettings.Instance) { DateParseHandling = DateParseHandling.None };

      public ServerAuthApi(IRequester requester, RequestContext context) : base(requester)
      {
         _requester = requester;
         Context = context;
      }

      public Promise<User> GetUser(long gamerTag)
      {
         return Requester.Request<User>(Method.GET, $"{BASIC_SERVICE}", new GetUserRequest
         {
            gamerTag = gamerTag
         });
      }

      public async Promise<BatchAccountsResponse> BatchAccounts(BatchAccountsRequest request)
      {
         var ids = request.playerIds.Distinct().ToList();
         var merged = new BatchAccountsResponse();
         for (var i = 0; i < ids.Count; i += BatchAccountsRequest.MaxPlayersPerRequest)
         {
            var page = await Requester.Request<BatchAccountsResponse>(Method.POST, "/api/accounts/batch",
               new BatchAccountsRequest
               {
                  playerIds = ids.GetRange(i, Math.Min(BatchAccountsRequest.MaxPlayersPerRequest, ids.Count - i)),
                  filter = request.filter,
                  includeAccount = request.includeAccount,
                  stats = request.stats
               },
               parser: json => JsonConvert.DeserializeObject<BatchAccountsResponse>(json, BatchAccountsJson));
            merged.players.AddRange(page.players);
            merged.filteredOut.AddRange(page.filteredOut);
            merged.notFound.AddRange(page.notFound);
         }

         return merged;
      }

      public Promise<AccountId> GetAccountId() => 
	      Requester.Request(Method.GET, $"{BASIC_SERVICE}/admin/me",parser: resp =>
	      {
		      var r = (ArrayDict)Json.Deserialize(resp);
		      return new AccountId() { Id = (long)r["id"] };
	      });

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
