using Beamable.Api;
using Beamable.Api.Caches;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Stats;
using Beamable.Common.Dependencies;
using Beamable.Platform.Tests;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using EmptyResponse = Beamable.Common.Api.EmptyResponse;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Beamable.Tests.Runtime
{
	public class PlayerStatsSetTests : BeamContextTest
	{
		private const float SET_TIMEOUT_SECONDS = 5f;

		protected override void OnRegister(IDependencyBuilder builder)
		{
			base.OnRegister(builder);

			// the real registration builds the stats service out of the concrete PlatformRequester,
			// which the mock context removes, so build it out of the mock requester instead.
			builder.RemoveIfExists<StatsService>();
			builder.AddScoped(provider => new StatsService(
								  provider.GetService<IPlatformService>(),
								  provider.GetService<IBeamableRequester>(),
								  provider,
								  UnityUserDataCache<Dictionary<string, string>>.CreateInstance,
								  new OfflineCache(provider.GetService<IRuntimeConfigProvider>(), useOfflineCache: false)));
		}

		[UnityTest]
		public IEnumerator Set_FailsWithTheRequestError()
		{
			// the failing stat write produces promise errors that nothing in the SDK handles,
			// so don't let the default uncaught promise handler fail the test.
			PromiseBase.SetPotentialUncaughtErrorHandler((promise, err) => { });
			LogAssert.ignoreFailingMessages = true;

			var expectedError = new RequesterException("", "POST", "/object/stats", 503, "{}");

			TriggerContextInit(OnRegister, ctx =>
			{
				OnInit(ctx);
				MockStatReads(ctx);
				MockStatWrite(ctx).WithResponse(expectedError);
			});
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			Exception caught = null;
			var set = Context.Stats.Set("test-key", "test-value");
			set.Error(err => caught = err);

			yield return WaitForCompletion(set);

			Assert.IsTrue(set.IsCompleted, "Set() never completed, so the caller can never learn that the stat write failed.");
			Assert.IsTrue(set.IsFailed);
			Assert.AreSame(expectedError, caught);
		}

		[UnityTest]
		public IEnumerator Set_CompletesWhenTheRequestSucceeds()
		{
			MockPlatformRoute<EmptyResponse> writeRoute = null;

			TriggerContextInit(OnRegister, ctx =>
			{
				OnInit(ctx);
				MockStatReads(ctx);
				writeRoute = MockStatWrite(ctx).WithResponse(new EmptyResponse());
			});
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);

			var set = Context.Stats.Set("test-key", "test-value");

			yield return WaitForCompletion(set);

			Assert.IsTrue(set.IsCompleted, "Set() never completed.");
			Assert.IsFalse(set.IsFailed);
			Assert.IsTrue(writeRoute.Called);
		}

		private static void MockStatReads(MockBeamContext ctx)
		{
			// the player has no stats yet, but every requested object id needs an entry in the
			// response, otherwise the stat refresh fails before the write is ever attempted.
			ctx.Requester.MockRequest<BatchReadStatsResponse>(Method.GET)
			   .WithURIPrefix("/basic/stats/client/batch")
			   .WithResponse(new BatchReadStatsResponse
			   {
				   results = new List<BatchReadEntry>
				   {
					   new BatchReadEntry
					   {
						   id = MockBeamContext.DEFAULT_DBID,
						   stats = new List<StatEntry>()
					   }
				   }
			   })
			   .WithToken(MockBeamContext.ACCESS_TOKEN);
		}

		private static MockPlatformRoute<EmptyResponse> MockStatWrite(MockBeamContext ctx)
		{
			return ctx.Requester.MockRequest<EmptyResponse>(Method.POST)
					  .WithURIPrefix($"/object/stats/client.public.player.{MockBeamContext.DEFAULT_DBID}")
					  .WithToken(MockBeamContext.ACCESS_TOKEN);
		}

		private static IEnumerator WaitForCompletion(Promise promise)
		{
			var startTime = Time.realtimeSinceStartup;
			while (!promise.IsCompleted && Time.realtimeSinceStartup - startTime < SET_TIMEOUT_SECONDS)
			{
				yield return null;
			}
		}
	}
}
