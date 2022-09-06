using Beamable.Api;
using Beamable.BSAT.Attributes;
using Beamable.BSAT.Core;
using Beamable.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Beamable.BSAT.Test.FriendsPSDK.Scripts
{
	public class FriendsPSDKScript : Testable
	{
		private const float TIMEOUT_MS = 3000;

		public string plrCode1;
		public string plrCode2;

		public long plr1Id;
		public long plr2Id;

		private BeamContext ctx1, ctx2;

		[TestRule(1)]
		public async Promise<TestResult> Setup()
		{
			ctx1 = BeamContext.ForPlayer(plrCode1);
			ctx2 = BeamContext.ForPlayer(plrCode2);

			await ctx1.OnReady;
			await ctx2.OnReady;

			await ctx1.Social.OnReady;
			await ctx2.Social.OnReady;

			plr1Id = ctx1.PlayerId;
			plr2Id = ctx2.PlayerId;

			return TestResult.Passed;
		}

		[TestRule(2)]
		public async Promise<TestResult> Invite()
		{
			try
			{
				if (ctx1.Social.IsFriend(plr2Id))
				{
					TestableDebug.LogWarning("Already friends. Unfriending first...");
					await ctx1.Social.Unfriend(plr2Id);
				}

				await ctx1.Social.Invite(plr2Id);

				await Until(
					() => ctx2.Social.ReceivedInvites.Any(
						invite => invite.invitingPlayerId == plr1Id && invite.mailId != 0),
					$"Invite from player {plr1Id} was not received within {TIMEOUT_MS}ms");

				await ctx2.Social.AcceptInviteFrom(plr1Id);
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}

			return TestResult.Passed;
		}

		[TestRule(3)]
		public async Promise<TestResult> Block()
		{
			try
			{
				if (ctx1.Social.IsBlocked(plr2Id))
				{
					TestableDebug.LogWarning("Player already blocked. Unblocking...");
					await ctx1.Social.UnblockPlayer(plr2Id);
				}

				await ctx1.Social.BlockPlayer(plr2Id);

				if (!ctx1.Social.IsBlocked(plr2Id))
				{
					TestableDebug.LogError("Block attempt failed.");
					return TestResult.Failed;
				}
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}

			try
			{
				await ctx2.Social.Invite(plr1Id);
			}
			catch (PlatformRequesterException e)
			{
				if (e.Payload.Contains("BlockedError"))
				{
					return TestResult.Passed;
				}

				TestableDebug.LogError(e);
				return TestResult.Failed;
			}

			return TestResult.Failed;
		}

		private async Promise Until(Func<bool> predicate, string timeoutMessage = "", int intervalMs = 25)
		{
			int timeMs = 0;
			while (timeMs < TIMEOUT_MS)
			{
				if (predicate())
				{
					return;
				}

				await Task.Delay(intervalMs);
				timeMs += intervalMs;
			}

			throw new TimeoutException(timeoutMessage);
		}
	}
}
