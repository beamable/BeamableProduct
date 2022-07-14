using NewTestingTool.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

internal class RegisteredTest
{
	public int Order { get; }
	public List<RegisteredMethodTest> RegisteredMethodTests { get; } = new List<RegisteredMethodTest>();

	public RegisteredTest(int order)
	{
		Order = order;
	}
}

internal class RegisteredMethodTest
{
	private Testable Testable { get; }
	private MethodInfo MethodInfo { get; }
	private object[] Arguments { get; }

	public RegisteredMethodTest(Testable testable, MethodInfo methodInfo, object[] arguments)
	{
		Testable = testable;
		MethodInfo = methodInfo;
		Arguments = arguments;
	}
	public void InvokeTest(bool displayLogs = false, int orderIndex = 0, int caseIndex = 0)
	{
		if (displayLogs)
			TestableDebug.Log($"Invoking test: Testable=[{Testable.GetType().Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{MethodInfo.Name}]");
		MethodInfo.Invoke(Testable, Arguments);
	}
}

public class TestController : ComponentSingleton<TestController>
{
	[SerializeField] private bool automaticallyStart = true;
	[SerializeField] private bool displayLogs = true;

	private List<List<RegisteredTest>> Tests { get; } = new List<List<RegisteredTest>>();

	private int _currentTestIndex, _currentOrderIndex, _currentCaseIndex;
	private bool _isSetup;

	private void Awake() => Init();
	private void Start()
	{
		if (automaticallyStart)
			InvokeNextTest();
	}
	
	private void Init()
	{
		if (!TryGetTestables(out var testables, out var errorLog))
		{
			TestableDebug.LogError(errorLog);
			return;
		}

		for (var index = 0; index < testables.Count; index++)
		{
			var testable = testables[index];
			if (!TryGetTestableMethods(testable, out var methodInfos, out errorLog))
			{
				TestableDebug.LogError(errorLog);
				return;
			}

			var registeredTests = RegisterTests(testable, methodInfos);
			Tests.Add(registeredTests);
		}

		_isSetup = true;
		
		if (displayLogs)
			TestableDebug.Log($"\"TestController\" setup correctly.");
	}
	private List<RegisteredTest> RegisterTests(Testable testable, IEnumerable<MethodInfo> methodInfos)
	{
		var registeredTests = new List<RegisteredTest>();
		foreach (var methodInfo in methodInfos)
		{
			var customAttributesData = GetCustomAttributesData(methodInfo);
			foreach (var customAttributeData in customAttributesData)
			{
				RegisterTest(testable, methodInfo, customAttributeData, ref registeredTests);
			}
		}
		return registeredTests.OrderBy(x => x.Order).ToList();
	}
	private bool TryGetTestables(out List<Testable> results, out string errorLog)
	{
		results = null;
		errorLog = string.Empty;
		
		var testables = FindObjectsOfType<Testable>().ToList();

		if (!testables.Any())
			errorLog = $"Cannot find any \"Testable\" class. Inherit from the \"Testable\" class to access the functionality of the test tool";
		else
			results = testables;

		return results != null;
	}
	private bool TryGetTestableMethods(Testable testable, out IEnumerable<MethodInfo> methodInfos, out string errorLog)
	{
		errorLog = string.Empty;
		methodInfos = testable.GetType()
		                      .GetRuntimeMethods()
		                      .Where(x => x.GetCustomAttributes(typeof(TestStepAttribute), false).Length > 0)
		                      .ToArray();

		if (!methodInfos.Any())
		{
			methodInfos = null;
			errorLog = $"Cannot find any \"Testable\" methods. Use \"TestStep()\" attribute over the method to register it as a test.";
		}
		return methodInfos != null;
	}
	private IEnumerable<CustomAttributeData> GetCustomAttributesData(MethodInfo methodInfo)
	{
		return methodInfo.CustomAttributes.Where(x => x.AttributeType == typeof(TestStepAttribute))
		                 .ToArray();
	}
	private void RegisterTest(Testable testable, MethodInfo methodInfo, CustomAttributeData customAttributeData, ref List<RegisteredTest> registeredTests)
	{
		var arguments = customAttributeData.ConstructorArguments;
		var order = (int)arguments[0].Value;
		var registeredMethodTest = new RegisteredMethodTest(testable, methodInfo, arguments.Skip(1).Select(x => x.Value).ToArray());
				
		if (registeredTests.All(x => x.Order != order))
			registeredTests.Add(new RegisteredTest(order));
		registeredTests.First(x => x.Order == order).RegisteredMethodTests.Add(registeredMethodTest);
	}

	public void InvokeNextTest()
	{
		while (true)
		{
			if (!_isSetup)
			{
				TestableDebug.LogError("\"TestController\" is not properly setup!");
				return;
			}

			if (_currentTestIndex < Tests.Count)
			{
				var registeredTests = Tests[_currentTestIndex];
				if (_currentOrderIndex < registeredTests.Count)
				{
					var registeredMethodTests = Tests[_currentTestIndex][_currentOrderIndex].RegisteredMethodTests;
					if (_currentCaseIndex < registeredMethodTests.Count)
					{
						_currentCaseIndex++;
						registeredMethodTests[_currentCaseIndex - 1].InvokeTest(displayLogs, _currentOrderIndex, _currentCaseIndex - 1);
					}
					else
					{
						_currentOrderIndex++;
						_currentCaseIndex = 0;
						continue;
					}
				}
				else
				{
					_currentTestIndex++;
					_currentOrderIndex = 0;
					_currentCaseIndex = 0;
					continue;
				}
			}
			else
				TestableDebug.Log("ALL TESTS COMPLETED");

			break;
		}
	}

	public void ResetTests()
	{
		_currentTestIndex = 0;
		_currentOrderIndex = 0;
		_currentCaseIndex = 0;
	}
}
