using Beamable;
using Beamable.Common;
using NewTestingTool.Attributes;

namespace NewTestingTool.Tests.FriendsPSDK.Scripts
{
	public class FriendsPSDKTest : Testable
	{
		public string plrCode1;
		public string plrCode2;

		public long plr1Id;
		public long plr2Id;

		[TestRule(1)]
		public async Promise<TestResult> Setup()
		{
			var ctx1 = BeamContext.ForPlayer(plrCode1);
			var ctx2 = BeamContext.ForPlayer(plrCode2);

			await ctx1.OnReady;
			await ctx2.OnReady;

			plr1Id = ctx1.PlayerId;
			plr2Id = ctx2.PlayerId;

			return TestResult.Passed;
		}

		[TestRule(2)]
		public TestResult Invite()
		{
			return TestResult.NotSet;
		}

	}
}
