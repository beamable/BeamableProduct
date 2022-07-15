using NewTestingTool.Attributes;
using NewTestingTool.Core;
using System.Threading.Tasks;

public class ExampleTest : Testable
{
	[TestStep(1)]
	private void TestMethodWithoutArguments()
	{
		TestController.Instance.MarkTestResult(TestResult.Passed);
	}
	
	[TestStep(2, "Hello", "World")]
	private async Task TestMethodWithArguments(string arg1, string arg2)
	{
		await Task.Delay(1000);
		TestController.Instance.MarkTestResultManually();
	}
	
	[TestStep(3, 2, 2)]
	[TestStep(3, 5, 10)]
	private async Task TestMethodWithArgumentsAndMultipleSteps(int arg1, int arg2)
	{
		await Task.Delay(1000);
		var sum = arg1 + arg2;
		TestController.Instance.MarkTestResult(TestResult.Failed);
	}
	
	[TestStep(4)]
	[TestStep(4)]
	private void SuperTest()
	{
		TestController.Instance.MarkTestResultManually();
	}
}
