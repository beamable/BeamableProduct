using NewTestingTool.Attributes;
using NewTestingTool.Core;
using System.Threading.Tasks;

/// <summary>
/// TestResult.Passed - Marks automatically a test as passed.
/// TestResult.Failed - Marks automatically a test as failed.
/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
/// </summary>

public class TestableTemplate : Testable
{
	[TestRule(0)]
	public TestResult Test()
	{
		return TestResult.Passed;
	}
	
	[TestRule(1, "Number", 3)]
	public TestResult TestWithArguments(string text, int number)
	{
		TestableDebug.Log($"{text}=[{number}]");
		return TestResult.Passed;
	}

	[TestRule(2)]
	public async Task<TestResult> TestAsync()
	{
		await Task.Delay(100);
		return TestResult.Passed;
	}
	
	[TestRule(3, "Hello", "Test!")]
	public async Task<TestResult> TestWithArgumentsAsync(string arg1, string arg2)
	{
		TestableDebug.Log($"{arg1} {arg2}");
		await Task.Delay(100);
		return TestResult.Passed;
	}
}
