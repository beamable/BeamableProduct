using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Leaderboards;
using Beamable.Server;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests.microservice
{
   [Microservice("simple")]
   public class SimpleMicroserviceNonLegacy : Microservice
   {
      public static MicroserviceFactory<SimpleMicroserviceNonLegacy> Factory => () => new SimpleMicroserviceNonLegacy();

      [ClientCallable]
      public string MethodWithRegularString_AsParameter(string str)
      {
         return str;
      }
   }

   [StorageObject("simple")]
   public class SimpleStorageObject : MongoStorageObject
   {

   }
   [Microservice("simple_no_updates", DisableAllBeamableEvents = true)]
   public class SimpleMicroserviceWithNoEvents : Microservice
   {
	   public static MicroserviceFactory<SimpleMicroserviceWithNoEvents> Factory => () => new SimpleMicroserviceWithNoEvents();
	   [ClientCallable]
	   public async Task<string> GetContent(string id)
	   {
		   var content = await Services.Content.GetContent(id);
		   return "Echo: " + content.Id;
	   }
   }
   [Microservice("simple", UseLegacySerialization = true)]
   public class SimpleMicroservice : Microservice
   {
      public static MicroserviceFactory<SimpleMicroservice> Factory => () => new SimpleMicroservice();

      [ClientCallable]
      public int Sum(int[] numbers)
      {
         if (numbers == null) return 0;

         var sum = 0;
         for (var i = 0; i < numbers.Length; i++)
         {
            sum += numbers[i];
         }

         return sum;
      }

      [ClientCallable]
      public int TwoArrays(int[] arr1, int[] arr2)
      {
         return Sum(arr1) + Sum(arr2);
      }

      [ClientCallable]
      public int Add(int a, int b)
      {
         return a + b;
      }

      [ClientCallable(requiredScopes: new []{"someScope"})]
      public bool AdminOnly()
      {
         return true;
      }

      [ClientCallable]
      public async Task<int> Delay(int ms)
      {
         await Task.Delay(ms);
         return ms;
      }

      [ClientCallable]
      public async Task<string> DelayThenGetEmail(int ms, long dbid)
      {
	      await Task.Delay(ms);
	      var getUser = Services.Auth.GetUser(dbid);
	      var output = await getUser;
	      return output.email;
      }

      [ClientCallable]
      public async Promise<int> PromiseTestMethod()
      {
         return await Promise<int>.Successful(1);
      }

      [ClientCallable]
      public Promise PromiseTypelessTestMethod()
      {
         Promise pr = new Promise();
         pr.CompleteSuccess();
         return pr;
      }

      [ClientCallable]
      public string MethodWithJSON_AsParameter(string jsonString)
      {
         return jsonString;
      }

      [ClientCallable]
      public string MethodWithRegularString_AsParameter(string str)
      {
         return str;
      }

      [ClientCallable]
      public Vector2Int MethodWithVector2Int_AsParameter(Vector2Int vec)
      {
         return vec;
      }

      [ClientCallable]
      public string MethodWithExceptionThrow(string msg)
      {
         throw new MicroserviceException(401, "UnauthorizedUser", "test");
      }

      // TODO: Add a test for an empty arg array, or a null

      [ClientCallable]
      public async Task<string> InventoryTest(ItemRef itemRef)
      {
         var items = await Services.Inventory.GetItems(itemRef);
         var x = items.FirstOrDefault();

         return x.ItemContent.Id;
      }

      [AdminOnlyCallable]
      public async Task LeaderboardCreateTest(string boardId, LeaderboardRef templateBoardRef)
      {
         var template = await Services.Content.GetContent(templateBoardRef);
         await Services.Leaderboards.CreateLeaderboard(boardId, template);
      }

      [ClientCallable]
      public async Task LeaderboardCreateFromTemplateCallableTest(string boardId, string leaderboardContentId)
      {
         var link = new LeaderboardLink {Id = leaderboardContentId};
         var template = await link.Resolve();
         await Services.Leaderboards.CreateLeaderboard(boardId, template);
      }

      [ClientCallable]
      public async Task LeaderboardCreateFromCodeCallableTest(string boardId)
      {
         await Services.Leaderboards.CreateLeaderboard(boardId,
            new OptionalInt(),
            new OptionalLong(),
            new OptionalBoolean(),
            new OptionalCohortSettings(),
            new OptionalListString(),
            new OptionalClientPermissions{HasValue = true, Value = new ClientPermissions{writeSelf = true}},
            new OptionalLong());
      }

      [ClientCallable]
      public async Promise<int> ListLeaderboardIds()
      {
         var res = await Services.Leaderboards.ListLeaderboards();
         return res.ids.Count;
      }

      [ClientCallable]
      public async Promise<int> ListLeaderboardIdsWithSkip(int skip)
      {
         var res = await Services.Leaderboards.ListLeaderboards(skip);
         return res.ids.Count;
      }

      [ClientCallable]
      public async Promise<int> ListLeaderboardIdsWithLimit(int limit)
      {
         var res = await Services.Leaderboards.ListLeaderboards(limit:limit);
         return res.ids.Count;
      }

      [ClientCallable]
      public async Promise<int> ListLeaderboardIdsWithSkipAndLimit(int skip, int limit)
      {
         var res = await Services.Leaderboards.ListLeaderboards(skip, limit:limit);
         return res.ids.Count;
      }

      [ClientCallable]
      public async Promise<int> GetPlayerLeaderboardViews(int dbid)
      {
         var res = await Services.Leaderboards.GetPlayerLeaderboards(dbid);
         return res.lbs.Count;
      }

      [ClientCallable]
      public async Task RemovePlayerEntry(string leaderboardId, long dbid)
      {
	      await Services.Leaderboards.RemovePlayerEntry(leaderboardId, dbid);
      }

      [ClientCallable]
      public async Task<string> GetUserEmail(int dbid)
      {
         return (await Services.Auth.GetUser(dbid)).email;
      }

      [ClientCallable]
      public async Task<string> GetContent(string id)
      {
         var content = await Services.Content.GetContent(id);
         return "Echo: " + content.Id;
      }

      [ClientCallable]
      public async Task TestAssumeUser(long otherId, bool force)
      {
         var ctx = AssumeUser(otherId, force);
         await ctx.Requester.Request<EmptyResponse>(Method.GET, "x");
      }

      [ClientCallable]
      public async Task<User> GetUserViaAccessToken(TokenResponse tokenResponse)
      {
         try
         {
            return await Services.Auth.GetUser(tokenResponse);
         }
         catch (Exception ex)
         {
            Assert.IsTrue(ex is NotImplementedException);
            throw;
         }
      }
   }
}
