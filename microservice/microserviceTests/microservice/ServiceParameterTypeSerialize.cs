using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice;

public class ServiceParameterTypeSerialize
{
	private const string EXAMPLE_JSON =
		"{\"current_player_name\":\"NAME_NOT_SET\",\"min_high_offer\":0,\"special_offer_seen\":false,\"current_league\":[5,55,1],\"das\":false}";
	private static readonly object[] JsonTestCases =
	{
		new object[] {null, null},
		new object[] {"\"Test\"", "Test"},
		new object[] {EXAMPLE_JSON,EXAMPLE_JSON}
	};

	[Test]
	[TestCaseSource(nameof(JsonTestCases))]
	public void DeserializeStringParametersTest(string sourceValue, string? expectedValue)
	{
		var result = ServiceMethodHelper.DeserializeStringParameter(sourceValue);
		
		Assert.AreEqual(expectedValue, result);
	}
}
