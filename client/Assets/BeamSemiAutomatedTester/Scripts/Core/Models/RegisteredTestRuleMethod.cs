using Beamable.BSAT.Core.Models.Descriptors;
using Beamable.BSAT;
using Beamable.BSAT.Extensions;
using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.BSAT.Core.Models
{
	[Serializable]
	public class RegisteredTestRuleMethod
	{
		public event Action OnTestResultChanged;

		public TestRuleMethodDescriptor TestRuleMethodDescriptor => _testRuleMethodDescriptor;
		public string Title 
		{
			get => _testRuleMethodDescriptor.Title;
			set => _testRuleMethodDescriptor.Title = value;
		}
		public string Description 
		{
			get => _testRuleMethodDescriptor.Description;
			set => _testRuleMethodDescriptor.Description = value;
		}
		public TestResult TestResult
		{
			get => _testResult;
			set
			{
				_testResult = value;
				OnTestResultChanged?.Invoke();
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
		public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
		
		private MethodInfo _methodInfo;
		public object[] Arguments => TestHelper.ConvertStringToObject(_argumentsRaw);
		public string[] ArgumentsRaw => _argumentsRaw;
		public bool IsTask => TestHelper.IsAsyncMethod(MethodInfo);
		public bool IsPromise => TestHelper.IsPromiseMethod(MethodInfo);
		
		[SerializeField] private string _testableName;
		[SerializeField] private string _methodName;
		[SerializeField] private TestResult _testResult;
		[SerializeField, HideInInspector] private TestRuleMethodDescriptor _testRuleMethodDescriptor;
		[SerializeField] private string[] _argumentsRaw;

		public RegisteredTestRuleMethod(ref Testable testable, MethodInfo methodInfo, object[] arguments, TestRuleMethodDescriptor testRuleMethodDescriptor)
		{
			_testableName = testable.GetType().AssemblyQualifiedName;
			_methodName = methodInfo.Name;
			_argumentsRaw = TestHelper.ConvertObjectToString(arguments);
			_testRuleMethodDescriptor = testRuleMethodDescriptor;
		}
		public async Promise<TestResult> InvokeTest(bool displayLogs, int orderIndex, int caseIndex)
		{
			if (displayLogs)
				TestableDebug.Log($"Invoking test: Testable=[{TestableType.Name}] Order=[{orderIndex+1}] Case=[{caseIndex+1}] Method=[{MethodInfo.Name.WrapWithColor(Color.yellow)}]");

#if UNITY_6000_0_OR_NEWER
			var obj = Object.FindFirstObjectByType(TestableType);
#else
			var obj = Object.FindObjectOfType(TestableType);
#endif
			return IsPromise
				? await (Promise<TestResult>)MethodInfo.Invoke(obj, Arguments)
				: IsTask
					? await (Task<TestResult>)MethodInfo.Invoke(obj, Arguments)
					: (TestResult)MethodInfo.Invoke(obj, Arguments);
		}
		public void Reset() => TestResult = TestResult.NotSet;

		public ArrayDict GenerateReport()
		{
			return new ArrayDict
			{
				{ "TestResult", TestResult},
				{ "TimeStamp", ElapsedTime.ToString("g") },
				{ "Arguments", ArgumentsRaw }
			};
		}
	}
}
