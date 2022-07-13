using NewTestingTool.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

internal class RegisteredTest
{
	public int Order { get; }
	public List<RegisteredMethodTest> RegisteredMethodTests { get; }
	
	public RegisteredTest(int order)
	{
		Order = order;
		RegisteredMethodTests = new List<RegisteredMethodTest>();
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
			TestableDebug.Log($"Invoking test: Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{MethodInfo.Name}]");
		MethodInfo.Invoke(Testable, Arguments);
	}
}

public class TestController : ComponentSingleton<TestController>
{
	[SerializeField] private bool displayLogs = true;

	private List<RegisteredTest> _registeredTests = new List<RegisteredTest>();

	private int _currentOrderIndex = 0;
	private int _currentCaseIndex = 0;

	private bool _isSetup;
	
	private void Awake() => Init();
	private void Init()
	{
		if (!TryGetTestable(out var testable, out var errorLog))
		{
			TestableDebug.LogError(errorLog);
			return;
		}

		if (!TryGetTestableMethods(testable, out var methodInfos, out errorLog))
		{
			TestableDebug.LogError(errorLog);
			return;
		}
		
		foreach (var methodInfo in methodInfos)
		{
			var customAttributesData = GetCustomAttributesData(methodInfo);
			foreach (var customAttributeData in customAttributesData)
				RegisterTest(testable, methodInfo, customAttributeData);
		}

		_registeredTests = _registeredTests.OrderBy(x => x.Order).ToList();
		_isSetup = true;
		
		if (displayLogs)
			TestableDebug.Log($"\"TestController\" setup correctly.");
	}
	private bool TryGetTestable(out Testable result, out string errorLog)
	{
		result = null;
		errorLog = string.Empty;
		
		var testables = FindObjectsOfType<Testable>()
		                .Select(x => x.GetComponent<Testable>())
		                .ToList();

		if (!testables.Any())
			errorLog = $"Cannot find any \"Testable\" class. Inherit from the \"Testable\" class to access the functionality of the test tool";
		else if (testables.Count != 1)
			errorLog = $"There can only be [1] \"Testable\" class at a time. Found [{testables.Count}].";
		else
			result = testables[0];

		return result != null;
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
	private void RegisterTest(Testable testable, MethodInfo methodInfo, CustomAttributeData customAttributeData)
	{
		var arguments = customAttributeData.ConstructorArguments;
		var order = (int)arguments[0].Value;
		var registeredMethodTest = new RegisteredMethodTest(testable, methodInfo, arguments.Skip(1).Select(x => x.Value).ToArray());
				
		if (_registeredTests.All(x => x.Order != order))
			_registeredTests.Add(new RegisteredTest(order));
		_registeredTests.First(x => x.Order == order).RegisteredMethodTests.Add(registeredMethodTest);
	}
	
	public void InvokeNextTest()
	{
		if (!_isSetup)
		{
			TestableDebug.LogError("\"TestController\" is not properly setup!");
			return;
		}
		
		if (_currentOrderIndex < _registeredTests.Count)
		{
			if (_currentCaseIndex < _registeredTests[_currentOrderIndex].RegisteredMethodTests.Count)
			{
				_currentCaseIndex++;
				_registeredTests[_currentOrderIndex].RegisteredMethodTests[_currentCaseIndex-1].InvokeTest(displayLogs, _currentOrderIndex, _currentCaseIndex-1);
			}
			else
			{
				_currentOrderIndex++;
				_currentCaseIndex = 0;
				InvokeNextTest();
			}
		}
		else
			TestableDebug.Log("ALL TESTS COMPLETED");
	}
	public void ResetTests()
	{
		_currentOrderIndex = 0;
		_currentCaseIndex = 0;
	}
}
