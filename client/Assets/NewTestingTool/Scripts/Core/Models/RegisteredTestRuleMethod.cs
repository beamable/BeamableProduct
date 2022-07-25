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
		public TestResult TestResult
		{
			get => _testResult;
			set => _testResult = value;
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
		public object[] Arguments => TestHelper.ConvertStringToObject(_arguments);
		
		[SerializeField] private TestResult _testResult;
		[SerializeField] private string _testableName;
		[SerializeField] private string _methodName;
		[SerializeField] private string[] _arguments;

		public RegisteredTestRuleMethod(Testable testable, MethodInfo methodInfo, object[] arguments)
		{
			_testResult = TestResult.NotSet;
			_testableName = testable.GetType().Name;
			_methodName = methodInfo.Name;
			_arguments = TestHelper.ConvertObjectToString(arguments);
		}
		public async Task<TestResult> InvokeTest(bool displayLogs, int orderIndex, int caseIndex)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{TestableType.Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{TestableDebug.WrapWithColor(MethodInfo.Name, Color.yellow)}]");
			
			var obj = Object.FindObjectOfType(TestableType);
			TestResult = TestHelper.IsAsyncMethod(MethodInfo)
				? await (Task<TestResult>)MethodInfo.Invoke(obj, Arguments)
				: (TestResult)MethodInfo.Invoke(obj, Arguments);
			
			return TestResult;
		}
		public void Reset() => TestResult = TestResult.NotSet;
	}
}
