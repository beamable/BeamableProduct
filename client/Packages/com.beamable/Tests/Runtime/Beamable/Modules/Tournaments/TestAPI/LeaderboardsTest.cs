using System.Collections;
using Beamable;
using Beamable.AccountManagement;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Leaderboards;
using Beamable.Platform.Tests;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using Packages.Beamable.Runtime.Tests.Beamable;
using UnityEngine.TestTools;

namespace Tests.Runtime.Beamable.Modules.Tournaments.TestAPI
{
    public class LeaderboardsTest : BeamableTest
    {
        [UnityTest]
        public IEnumerator WhenEmailInvalid_SignalsInvalid()
        {
            yield return MockApi.LeaderboardService.SetScore(new LeaderboardRef(), 0).AsYield();
        }
    }
}