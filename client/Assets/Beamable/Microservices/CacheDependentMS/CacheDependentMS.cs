using System;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Leaderboards;
using Beamable.Server;
using Beamable.Server.Api.Groups;
using Beamable.Server.Api.Leaderboards;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.Server
{
   /// <summary>
   /// This is example code that explains how to setup a Microservice that caches data during its initialization to be reused in requests over its lifetime.
   ///
   /// Explains the steps necessary to setup a pre-computed data cache that is built on C#MS initialization.
   ///
   /// Potential Use Cases:
   ///  - If your use case requires you to use some configuration/ContentObjects and live user data, you could use this to cache it.
   ///  - If your use case requires talking to a third-party service for some data and using this data to service requests, you can use this to cache it.
   ///  - Etc...
   ///
   /// Things to Note:
   ///  - We use Microsoft's Dependency Injection for setting up the C#MS application internally.
   ///  - The IServiceBuilder and IServiceProvider are wrappers to provide you some utilities when working with Microsoft's DI.
   ///  - Currently, there is an issue that causes Singleton services to be disposed before they should be. The workaround is shown in the sample below.   ///
   /// </summary>
   [Microservice("CacheDependentMS")]
   public class CacheDependentMS : Microservice
   {

      // /// <summary>
      // /// Initialized global Cache instance --- This will eventually become unnecessary and managed by Beamable.
      // /// </summary>
      // private static readonly Cache CachedData = new Cache();
      //
      // /// <summary>
      // /// This is the declaration of the class you wish to use to cache information. It can be any class you wish and does NOT have to be an internal class.
      // /// </summary>
      // public class Cache
      // {
      //    /// <summary>
      //    /// Just an example of cached data.
      //    /// </summary>
      //    public LeaderBoardView CachedView;
      // }

      /// <summary>
      /// This is the method that declares existing services (classes managed by the Dependency Injection system).
      /// In it you should use the service builder to declare Singleton, Scoped (per-Http-Request) or Transient (per-dependent-service) services.
      /// </summary>
      /// <param name="serviceBuilder"></param>
      // [ConfigureServices]
      // public static void ConfigureServicesForCacheDependentMS(IServiceBuilder serviceBuilder)
      // {
      //    // Adds the statically initialized instance of our cache class "Cache" as s singleton.
      //    // serviceBuilder.AddSingleton(_ => CachedData);
      //
      //    // Once the issue is fixed, you'll be able to simply say this.
      //    // serviceBuilder.AddSingleton<Cache>();
      // }

      [ClientCallable]
      public async Task Add(int a)
      {
	      if (Context.UserId <= 0) return;
	      var collection = await Storage.CollectionFromAliveStorage<Data>("data");

	      await collection.InsertOneAsync(new Data{a = a});
      }

      [ClientCallable]
      public async Task<List<int>> ReadAll()
      {
	      var collection = await Storage.CollectionFromAliveStorage<Data>("data");
	      var raw= await collection.Find(d => true).ToListAsync();
	      return raw.Select(d => d.a).ToList();
      }

      [ClientCallable]
      public async Task Write(int a, int b, string c)
      {
	      var collection = await Storage.CollectionFromAliveStorage<Doodad>("doodads");
	      await collection.InsertOneAsync(new Doodad
	      {
		      a = a, b = b, c = c
	      });
      }

      [ClientCallable]
      public async Promise<List<string>> GetAll()
      {
	      var collection = await Storage.CollectionFromAliveStorage<Doodad>("doodads");

	      var res = await collection.FindAsync(x => true);
	      var all = await res.ToListAsync();
	      return all.Select(x => $"{x.a},{x.b},{x.c}").ToList();
      }

      public class Doodad
      {
	      public ObjectId id;
	      public int a, b;
	      public string c;
      }

      /// <summary>
      /// This is the async method that you can use to affect the C#MS's initialization. You can have any of these as you wish. They'll be executed in declaration or you can use
      /// <see cref="InitializeServicesAttribute.ExecutionOrder"/> to make the order explicit. These are executed after you have access to our platform's services but before the C#MS is
      /// receiving any player-traffic.
      /// <para/>
      /// You can use the <paramref name="serviceInitializer"/> to access any of our MicroserviceApi interfaces as well as our <see cref="IStorageObjectConnectionProvider"/>.
      /// These can be used normally as you use in Client/AdminOnlyCallable methods, with a single caveat:
      /// <para/>
      ///  - If you are calling an endpoint that gets data relative to a specific-player you must go through the AssumeUser("userId") flow and use <see cref="RequestHandlerData.Services"/> returned.
      /// <para/>
      /// Things to note:
      /// <para/>
      ///  - You must return await a <see cref="Promise{Unit}"/>
      ///  - Any exception thrown during this Promise-chain will result in the C#MS failing to start.
      ///  - If you wish to handle an exception without failing to start, you must wrap it with your own try/catch block.
      ///  - If you prefer to use async/await instead of the callback syntax, you can.
      /// </summary>
      // [InitializeServices]
      // public static /* async */ Promise<Unit> InitializeServicesForCacheDependentMS(IServiceInitializer serviceInitializer)
      // {
      //    // Declares returning promise that'll be completed after we do what we want to ensure the C#MS is ready to receive traffic.
      //    var promise = new Promise<Unit>();
      //
      //    // Gets our service that'll be used as a Cache.
      //    var cache = serviceInitializer.GetServiceAsCache<Cache>(); // This call has a internal guard that guarantee's the Cache service was registered as a singleton.
      //    // var cache = serviceInitializer.GetService<Cache>(); // This call does the same as above, but does not have the guard.
      //
      //    // Gets our leaderboard service that we will use to cache this stuff.
      //    var leaderboardService = serviceInitializer.GetService<IMicroserviceLeaderboardsApi>();
      //
      //    // We could also be doing something similar to this to talk to a StorageObject
      //    // serviceInitializer.GetService<IStorageObjectConnectionProvider>().GetDatabase<MongoStorageObjectSubclass>().GetCollection<DocumentName>()...
      //
      //    // Or this to make a request from Player-Specific endpoint.
      //    // serviceInitializer.GetService<CacheDependentMS>().AssumeUser("ImpersonatedUserId").Services ...
      //
      //    // Promise Callback version.
      //    // In this example, we are just getting a generic board's view and cache'ing it in our Cache singleton service.
      //    leaderboardService.GetBoard(new LeaderboardRef() {Id = "leaderboards.New_LeaderboardContent"}, 0, int.MaxValue)
      //       // When the leaderboard service returns the board's view, we complete our promise and return it
      //       .Then(view =>
      //       {
      //          cache.CachedView = view;
      //          BeamableLogger.Log($"[Callback] Cache-ing LeaderboardView {view}");
      //          promise.CompleteSuccess(new Unit());
      //       });
      //
      //    return promise;
      //
      //
      //    // // Async/Await version (you no longer need the promise variable at the first line)
      //    //
      //    // cache.CachedView = await leaderboardService.GetBoard(new LeaderboardRef() {Id = "leaderboards.New_LeaderboardContent"}, 0, int.MaxValue);
      //    // BeamableLogger.Log($"[Async] Cache-ing LeaderboardView {cache.CachedView}");
      //    // return new Unit();
      // }
      //
      //
      // /// <summary>
      // /// This is the recommended way to get the Cache instance. For forward compatibility with the fix for the singleton issue, you should NOT use the static readonly cache directly.
      // /// </summary>
      // private readonly Cache _instance;
      //
      // /// <summary>
      // /// Constructor with declared service dependencies. This is how you tell the Dependency Injection framework that this Microservice depends on the Cache service.
      // /// </summary>
      // public CacheDependentMS(Cache injectedCache)
      // {
      //    _instance = injectedCache;
      // }
      //
      // /// <summary>
      // /// Just a trivial example of a ClientCallable that access the cached data.
      // /// </summary>
      // /// <returns></returns>
      // [ClientCallable]
      // public LeaderBoardView GetCachedView()
      // {
      //    return _instance.CachedView;
      // }
   }
}
