using NewTestingTool.Attributes;
using System.Threading.Tasks;

public class ExampleTest : Testable
{
	private void Start() => TestController.Instance.InvokeNextTest();

	[TestStep(1)]
	private void TestMethodWithoutArguments()
	{
		TestableDebug.Log($"Result=[Hello World!] Method=[{nameof(TestMethodWithoutArguments)}]");
		TestController.Instance.InvokeNextTest();
	}
	
	[TestStep(2, "Hello", "World")]
	private async Task TestMethodWithArguments(string arg1, string arg2)
	{
		await Task.Delay(0);
		TestableDebug.Log($"Result=[{arg1} {arg2}!] Method=[{nameof(TestMethodWithArguments)}]");
		TestController.Instance.InvokeNextTest();
	}
	
	[TestStep(3, 2, 2)]
	[TestStep(3, 5, 10)]
	private async Task TestMethodWithArgumentsAndMultipleSteps(int arg1, int arg2)
	{
		await Task.Delay(0);
		var sum = arg1 + arg2;
		TestableDebug.Log($"Result=[{sum}] Method=[{nameof(TestMethodWithArgumentsAndMultipleSteps)}]");
		TestController.Instance.InvokeNextTest();
	}
}
