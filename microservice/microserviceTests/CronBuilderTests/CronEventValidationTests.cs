using Beamable.Common.Scheduler;
using NUnit.Framework;

namespace microserviceTests.CronBuilderTests;

public class CronEventValidationTests
{
	[TestCase("* * * * * *", true, null)]
	[TestCase("1/2 * * * * *", true, null)]
	[TestCase("4 * * 2-3 * *", true, null)]
	[TestCase("0 30 12 * 4 5", true, null)]
	[TestCase("0 30 12 * 4 5,2", true, null)]
	[TestCase("0 30 12/4 * 4 5,2", true, null)]
	[TestCase("0 30 12/4 * 4 5,2", true, null)]
	[TestCase(null, false, "cron string is null or empty")]
	[TestCase("", false, "cron string is null or empty")]
	[TestCase("* * * * *", false, "must contain six parts")]
	[TestCase("* * * * * * *", false, "must contain six parts")]
	[TestCase("* * * *  * *", false, "must contain six parts")]
	[TestCase("a * * * * *", false, "part=[0] does not look like a cron clause")]
	[TestCase(". * * * * *", false, "part=[0] does not look like a cron clause")]
	[TestCase("1 ^ * * * *", false, "part=[1] does not look like a cron clause")]
	public void SimpleValidation(string cron, bool valid, string message)
	{
		var success = CronValidation.TryValidate(cron, out var msg);
		
		Assert.AreEqual(message, msg);
		Assert.AreEqual(valid, success);
		
	}
	
}
