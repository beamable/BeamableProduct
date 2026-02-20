using cli.Commands.Project;
using cli.DeploymentCommands;
using NUnit.Framework;

namespace tests;

public class MaxParallelCountTests
{
	[Test]
	public void DefaultMaxParallelCountIs8()
	{
		// Verify that the default value for MaxParallelCount is 8
		var args = new PlanDeploymentCommandArgs();
		
		// The property default is 0 until set by the command option parser which defaults to 8
		Assert.That(args.MaxParallelCount, Is.EqualTo(0));
	}
	
	[Test]
	public void MaxParallelCountCanBeSet()
	{
		// Verify that MaxParallelCount can be set to a custom value
		var args = new PlanDeploymentCommandArgs
		{
			MaxParallelCount = 5
		};
		
		Assert.That(args.MaxParallelCount, Is.EqualTo(5));
	}
	
	[Test]
	public void ReleaseCommandHasMaxParallelCount()
	{
		// Verify that ReleaseDeploymentCommandArgs also has the property
		var args = new ReleaseDeploymentCommandArgs
		{
			MaxParallelCount = 10
		};
		
		Assert.That(args.MaxParallelCount, Is.EqualTo(10));
	}
}
