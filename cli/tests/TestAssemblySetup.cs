using cli;
using NUnit.Framework;
using System.IO;

namespace tests;

[SetUpFixture]
public class TestAssemblySetup
{
	[OneTimeSetUp]
	public void WriteBeamRootFile()
	{
		// Write a .beamroot sentinel file in the test output directory so that
		// ConfigService.TryToFindBeamableFolder stops traversing upward and does
		// not accidentally pick up a .beamable folder that exists higher in the
		// repo hierarchy (e.g. at the repo root).
		var beamRootPath = Path.Combine(Directory.GetCurrentDirectory(), ConfigService.BEAM_ROOT_FILE);
		File.WriteAllText(beamRootPath, string.Empty);
	}
}
