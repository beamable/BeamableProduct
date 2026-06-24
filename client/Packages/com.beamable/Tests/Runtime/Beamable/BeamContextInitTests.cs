using Beamable;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Platform.Tests;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using System;
using System.Collections;
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

}
