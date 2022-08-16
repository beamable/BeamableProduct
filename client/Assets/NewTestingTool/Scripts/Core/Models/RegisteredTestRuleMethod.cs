using NewTestingTool.Core;
using NewTestingTool.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTestRuleMethod
	{
		public event Action<TestResult> OnTestResultChanged;
		
		public string Title 
		{
			get => _testDescriptor.Title;
			set => _testDescriptor.Title = value;
		}
		public string Description 
		{
			get => _testDescriptor.Description;
			set => _testDescriptor.Description = value;
		}
		public TestResult TestResult
		{
			get => _testResult;
			set
			{
				_testResult = value;
				OnTestResultChanged?.Invoke(TestResult);
			}
		}
		public Type TestableType
		{
			get
			{
				if (_testableType == null)
					_testableType = Type.GetType(_testableName);
				return _testableType;
			}
		}
		private Type _testableType;
		public MethodInfo MethodInfo
		{
			get
			{
				if (_methodInfo == null)
					_methodInfo = TestableType.GetRuntimeMethods().FirstOrDefault(x => x.Name == _methodName);
				return _methodInfo;
			}
		}
		private MethodInfo _methodInfo;
		public object[] Arguments => TestHelper.ConvertStringToObject(_argumentsRaw);
		public string[] ArgumentsRaw => _argumentsRaw;
		public bool IsTask => TestHelper.IsAsyncMethod(MethodInfo);
		
		[SerializeField] private TestResult _testResult;
		[SerializeField] private string _testableName;
		[SerializeField] private string _methodName;
		[SerializeField] private string[] _argumentsRaw;

		private TestDescriptor _testDescriptor;

		public RegisteredTestRuleMethod(ref Testable testable, MethodInfo methodInfo, object[] arguments, TestDescriptor testDescriptor)
		{
			_testResult = TestResult.NotSet;
			_testableName = testable.GetType().Name;
			_methodName = methodInfo.Name;
			_argumentsRaw = TestHelper.ConvertObjectToString(arguments);
			_testDescriptor = testDescriptor;
		}
		public async Task<TestResult> InvokeTest(bool displayLogs, int orderIndex, int caseIndex)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{TestableType.Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{TestableDebug.WrapWithColor(MethodInfo.Name, Color.yellow)}]");
			
			var obj = Object.FindObjectOfType(TestableType);
			TestResult = IsTask 
				? await (Task<TestResult>)MethodInfo.Invoke(obj, Arguments)
				: (TestResult)MethodInfo.Invoke(obj, Arguments);
			
			return TestResult;
		}
		public void Reset() => TestResult = TestResult.NotSet;
	}
}
