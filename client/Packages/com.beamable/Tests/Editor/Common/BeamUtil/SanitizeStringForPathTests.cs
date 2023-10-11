using Beamable.Common.Util;
using NUnit.Framework;

namespace Beamable.Editor.Tests.Common.BeamUtilTests
{
	public class SanitizeStringForPathTests
	{
		[TestCase("player1", "player1")]
		[TestCase("n/a", "n-a")]
		[TestCase(null, null)]
		[TestCase("", "")]
		public void Sanitize_Unchanged(string input, string expected)
		{
			var result = BeamUtil.SanitizeStringForPath(input);
			Assert.AreEqual(expected, result);
		}
	}
}
