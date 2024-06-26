using Beamable.Api.Autogenerated.Models;
using Beamable.Common.Content;
using Beamable.Common.Scheduler;
using NUnit.Framework;

namespace microserviceTests.CronBuilderTests;

public class JobConversionTests : CommonTest
{
	[TestCase]
	public void CanDeserializeJobWithoutSource()
	{
		var job = new JobDefinition
		{
			id = "test",
			name = "test",
			owner = "test",
			source = new OptionalString(),
			jobAction = new HttpCall
			{
				body = "test",
				contentType = "test",
				method = "post",
				uri = "test"
			},
			retryPolicy = new OptionalJobRetryPolicy(new JobRetryPolicy
			{
				maxRetryCount = 1,
				retryDelayMs = 1,
				useExponentialBackoff = false
			}),
			triggers = new IOneOf_CronTriggerOrExactTrigger[]{}
		};

		var result = BeamScheduler.Utility.Convert(job);
		Assert.That(result.source, Is.Null);
	}
}
