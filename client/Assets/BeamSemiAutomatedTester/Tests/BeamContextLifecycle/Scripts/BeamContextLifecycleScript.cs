using Beamable.BSAT.Attributes;
using Beamable.BSAT.Core;
using Beamable.Common;
using System;

/// <summary>
/// TestResult.Passed - Marks automatically a test as passed.
/// TestResult.Failed - Marks automatically a test as failed.
/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
/// </summary>

namespace Beamable.BSAT.Test.BeamContextLifecycle
{
	public class BeamContextLifecycleScript : Testable
	{
		[TestRule(0)]
		public async Promise<TestResult> DefaultBeamContextInitTest()
		{
			try
			{
				var context = BeamContext.Default;
				await context.OnReady;
				return TestResult.Passed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}

		[TestRule(1)]
		public async Promise<TestResult> OtherBeamContextInitTest()
		{
			try
			{
				var defaultContext = BeamContext.Default;
				var otherContext = BeamContext.InParent(this);
				await defaultContext.OnReady;
				await otherContext.OnReady;

				bool areDifferent = defaultContext.PlayerId != otherContext.PlayerId;

				return areDifferent ? TestResult.Passed : TestResult.Failed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
	}
}
