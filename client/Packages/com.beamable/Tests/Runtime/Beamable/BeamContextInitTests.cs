using Beamable;
using Beamable.Api.Payments;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Dependencies;
using Beamable.Platform.Tests;
#if BEAMABLE_PURCHASING
using Beamable.Purchasing;
#endif
using Beamable.Tests.Runtime;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.Beamable
{
	public class BeamContextInitTests : BeamContextTest
	{
		[UnityTest]
		public IEnumerator SuccessfullInit()
		{
			TriggerContextInit();
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(Context.OnReady.IsCompleted);
			Assert.AreEqual(PromiseBase.Unit, Context.OnReady.GetResult());
		}

		[UnityTest]
		public IEnumerator ErrorOutAfterAttempts()
		{
			PromiseBase.SetPotentialUncaughtErrorHandler((promise, err) => { });
			LogAssert.ignoreFailingMessages = true;

			MockPlatformRoute<User> failingMockRequest = null;
			var config = ScriptableObject.CreateInstance<CoreConfiguration>();
			config.ContextRetryDelays = new double[] { .1, .1, .1 };
			config.EnableInfiniteContextRetries = false;

			TriggerContextInit(builder =>
			{
				OnRegister(builder);
				// also swap out core config.
				builder.RemoveIfExists<CoreConfiguration>();

				builder.AddSingleton(config);
			}, context =>
			{
				context.Requester.MockRequest<TokenResponse>(Method.POST, "/basic/auth/token")
						 .WithNoAuthHeader()
						 .WithJsonFieldMatch("grant_type", "guest")
						 .WithResponse(new TokenResponse
						 {
							 access_token = MockBeamContext.ACCESS_TOKEN,
							 refresh_token = "test_refresh",
							 expires_in = 10000,
							 token_type = "test_token"
						 });

				context.Requester.MockRequest<TokenResponse>(Method.POST, "/basic/auth/token")
					   .WithNoAuthHeader()
					   .WithJsonFieldMatch("grant_type", "refresh_token")
					   .WithResponse(new TokenResponse { access_token = MockBeamContext.ACCESS_TOKEN, refresh_token = "refresh_test" });

				failingMockRequest = context.Requester.MockRequest<User>(Method.GET, "/basic/accounts/me")
										 .WithResponse(new RequesterException("", "GET", "url", 401, ""))
										 .WithToken(MockBeamContext.ACCESS_TOKEN)

					;

				context.AddPubnubRequests()
					   .AddSessionRequests();
			});

			var errorWasTriggered = false;
			var apiErrorWasTriggered = false;
			Exception exception = null;

			Context.OnReady.Error(err =>
			{
				errorWasTriggered = true;
				exception = err;
			});

			Context.OnReady.Map<Unit>(x => x).Error(err =>
			{
				apiErrorWasTriggered = true;
			});
			yield return Context.OnReady.ToYielder();
			Assert.IsTrue(apiErrorWasTriggered);
			Assert.IsTrue(errorWasTriggered);
			Assert.IsInstanceOf<BeamContextInitException>(exception);
			var beamException = (BeamContextInitException)exception;

			Assert.AreEqual(config.ContextRetryDelays.Length, failingMockRequest.CallCount);
			Assert.AreEqual(config.ContextRetryDelays.Length, beamException.Exceptions.Length);
			for (var i = 0; i < config.ContextRetryDelays.Length; i++)
			{
				Assert.IsNotNull(beamException.Exceptions[i]);
			}
			Assert.AreEqual(Context, beamException.Ctx);

		}

		[UnityTest]
		public IEnumerator InfiniteRetriesDoNotOverflowErrorBuffer()
		{
			PromiseBase.SetPotentialUncaughtErrorHandler((promise, err) => { });
			LogAssert.ignoreFailingMessages = true;

			var sawIndexOutOfRange = false;
			void TrackLog(string condition, string stackTrace, LogType type)
			{
				if ((condition?.Contains(nameof(IndexOutOfRangeException)) ?? false) ||
				    (stackTrace?.Contains(nameof(IndexOutOfRangeException)) ?? false))
				{
					sawIndexOutOfRange = true;
				}
			}

			Application.logMessageReceived += TrackLog;
			try
			{
				MockPlatformRoute<User> failingMockRequest = null;
				var config = ScriptableObject.CreateInstance<CoreConfiguration>();
				config.ContextRetryDelays = new double[] { .01, .01 };
				config.EnableInfiniteContextRetries = true;

				TriggerContextInit(builder =>
				{
					OnRegister(builder);
					builder.RemoveIfExists<CoreConfiguration>();
					builder.AddSingleton(config);
				}, context =>
				{
					context.Requester.MockRequest<TokenResponse>(Method.POST, "/basic/auth/token")
						   .WithNoAuthHeader()
						   .WithJsonFieldMatch("grant_type", "guest")
						   .WithResponse(new TokenResponse
						   {
							   access_token = MockBeamContext.ACCESS_TOKEN,
							   refresh_token = "test_refresh",
							   expires_in = 10000,
							   token_type = "test_token"
						   });

					context.Requester.MockRequest<TokenResponse>(Method.POST, "/basic/auth/token")
						   .WithNoAuthHeader()
						   .WithJsonFieldMatch("grant_type", "refresh_token")
						   .WithResponse(new TokenResponse
						   {
							   access_token = MockBeamContext.ACCESS_TOKEN,
							   refresh_token = "refresh_test"
						   });

					failingMockRequest = context.Requester.MockRequest<User>(Method.GET, "/basic/accounts/me")
											 .WithResponse(new RequesterException("", "GET", "url", 401, ""))
											 .WithToken(MockBeamContext.ACCESS_TOKEN);

					context.AddPubnubRequests()
						   .AddSessionRequests();
				});

				// The old bug appears only after attempts exceed ContextRetryDelays.Length.
				var requiredAttempts = config.ContextRetryDelays.Length + 3;
				var timeout = Time.realtimeSinceStartup + 2f;
				while (failingMockRequest.CallCount < requiredAttempts &&
				       Time.realtimeSinceStartup < timeout)
				{
					yield return null;
				}

				Assert.GreaterOrEqual(failingMockRequest.CallCount, requiredAttempts,
									  "The retry loop must run past the configured retry-delay array length.");
				Assert.IsFalse(sawIndexOutOfRange,
							   "Infinite BeamContext retries must not overflow the fixed-size error buffer.");
				Assert.IsFalse(Context.OnReady.IsCompleted,
							   "A persistently failing infinite retry should keep OnReady pending.");
			}
			finally
			{
				Application.logMessageReceived -= TrackLog;
				LogAssert.ignoreFailingMessages = false;
			}
		}
	}

#if BEAMABLE_PURCHASING
	public class CommerceInitializationTests : BeamContextTest
	{
		private bool _originalSkipCommerceInitialization;
		private MockPlatformRoute<GetSKUsResponse> _getSkusRoute;

		[SetUp]
		public void SaveConfiguration()
		{
			_originalSkipCommerceInitialization = CoreConfiguration.Instance.SkipCommerceInitialization;
		}

		[TearDown]
		public void RestoreConfiguration()
		{
			CoreConfiguration.Instance.SkipCommerceInitialization = _originalSkipCommerceInitialization;
		}

		protected override void OnRegister(IDependencyBuilder builder)
		{
			base.OnRegister(builder);
			UnityBeamablePurchaserRegister.RegisterServices(builder);
			BeamablePurchaserConfigurationRegister.RegisterServices(builder);
		}

		protected override void OnInit(MockBeamContext ctx)
		{
			base.OnInit(ctx);
			_getSkusRoute = ctx.Requester.MockRequest<GetSKUsResponse>(Method.GET, "/basic/commerce/skus")
				.WithResponse(new GetSKUsResponse
				{
					skus = new SKUDefinitions
					{
						version = 1,
						created = string.Empty,
						definitions = new List<SKU>()
					}
				})
				.WithToken(MockBeamContext.ACCESS_TOKEN);
		}

		[UnityTest]
		public IEnumerator SkippedCommerceInitializationUsesDummyPurchaserWithoutRequestingSkus()
		{
			CoreConfiguration.Instance.SkipCommerceInitialization = true;

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();

			Assert.AreEqual(0, _getSkusRoute.CallCount);
			Assert.IsInstanceOf<DummyPurchaser>(Context.Api.BeamableIAP.GetResult());
		}

#if !BEAMABLE_PURCHASING_IMPLEMENTATION_DISABLED
		[UnityTest]
		public IEnumerator DefaultCommerceInitializationUsesUnityPurchaserAndRequestsSkus()
		{
			CoreConfiguration.Instance.SkipCommerceInitialization = false;

			TriggerContextInit();
			yield return Context.OnReady.ToYielder();

			Assert.AreEqual(1, _getSkusRoute.CallCount);
			Assert.IsInstanceOf<UnityBeamablePurchaser>(Context.Api.BeamableIAP.GetResult());
		}
#endif
	}
#endif

}
