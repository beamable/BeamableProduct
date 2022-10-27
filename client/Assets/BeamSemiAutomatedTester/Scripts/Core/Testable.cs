using UnityEngine;

namespace Beamable.BSAT.Core
{
	public abstract class Testable : MonoBehaviour { }

	public enum TestResult
	{
		NotSet,
		Passed,
		Failed,
	}
}
