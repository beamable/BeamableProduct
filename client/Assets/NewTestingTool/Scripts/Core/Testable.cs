using UnityEngine;

namespace Beamable.NewTestingTool.Core
{
	public abstract class Testable : MonoBehaviour { }

	public enum TestResult
	{
		NotSet,
		Passed,
		Failed,
	}
}
