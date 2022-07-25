using NewTestingTool.Attributes;
using System.Threading.Tasks;

public class ExampleTest : Testable
{
	/// <summary>
	/// TestResult.Passed - Marks automatically a test as passed.
	/// TestResult.Failed - Marks automatically a test as failed.
	/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
	/// </summary>
	
	[TestRule(1)]
	private TestResult TestMethodWithoutArguments()
	{
		return TestResult.NotSet;
	}
	
	[TestRule(2, "Hello", "World")]
	private async Task<TestResult> TestMethodWithArguments(string arg1, string arg2)
	{
		await Task.Delay(2000);
		return TestResult.Passed;
	}
	
	[TestRule(3, 2, 2)]
	[TestRule(3, 5, 10)]
	private async Task<TestResult> TestMethodWithArgumentsAndMultipleSteps(int arg1, int arg2)
	{
		await Task.Delay(1000);
		var sum = arg1 + arg2;
		return TestResult.NotSet;
	}
	
	[TestRule(4)]
	[TestRule(4)]
	private TestResult SuperTest()
	{
		return TestResult.Failed;
	}
}
