using NewTestingTool.Attributes;
using NewTestingTool.Core;
using System.Threading.Tasks;

public class ExampleTest : Testable
{
	/// <summary>
	/// TestResult.Passed - Marks automatically a test as passed.
	/// TestResult.Failed - Marks automatically a test as failed.
	/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
	/// </summary>

	[TestStep(1)]
	private TestResult TestMethodWithoutArguments()
	{
		return TestResult.NotSet;
	}
	
	[TestStep(2, "Hello", "World")]
	private async Task<TestResult> TestMethodWithArguments(string arg1, string arg2)
	{
		await Task.Delay(3000);
		return TestResult.Passed;
	}
	
	[TestStep(3, 2, 2)]
	[TestStep(3, 5, 10)]
	private async Task<TestResult> TestMethodWithArgumentsAndMultipleSteps(int arg1, int arg2)
	{
		await Task.Delay(1000);
		var sum = arg1 + arg2;
		return TestResult.NotSet;
	}
	
	[TestStep(4)]
	[TestStep(4)]
	private TestResult SuperTest()
	{
		return TestResult.Failed;
	}
}
