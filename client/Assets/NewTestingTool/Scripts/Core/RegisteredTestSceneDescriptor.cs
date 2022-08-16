using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class TestDescriptorsWrapper
{
	public string TestableName => _testableName;
	
	[SerializeField] private string _testableName; 
	[SerializeField] private List<TestDescriptor> _testDescriptors = new List<TestDescriptor>();

	public TestDescriptorsWrapper(string testableName)
	{
		_testableName = testableName;
	}
	
	public TestDescriptor GetTestDescriptor(MethodInfo methodInfo) 
		=> GetTestDescriptor(methodInfo.Name);
	public TestDescriptor GetTestDescriptor(string methodName) => 
		_testDescriptors.FirstOrDefault(x => x.MethodName == methodName) ?? CreateTestDescriptor(methodName);
	private TestDescriptor CreateTestDescriptor(string methodName)
	{
		var testDescriptor = new TestDescriptor(methodName);
		_testDescriptors.Add(testDescriptor);
		return testDescriptor;
	}
}

public class RegisteredTestSceneDescriptor : ScriptableObject
{
	[SerializeField] private List<TestDescriptorsWrapper> _testDescriptorWrappers = new List<TestDescriptorsWrapper>();

	public TestDescriptor GetTestDescriptor(Testable testable, MethodInfo methodInfo)
		=> GetTestDescriptor(testable.GetType().Name, methodInfo.Name);
	public TestDescriptor GetTestDescriptor(string testableName, string methodName)
	{
		var testDescriptor = _testDescriptorWrappers.FirstOrDefault(x => x.TestableName == testableName) ??
		                     CreateTestDescriptor(testableName);
		return testDescriptor.GetTestDescriptor(methodName);
	}
	private TestDescriptorsWrapper CreateTestDescriptor(string testableName)
	{
		var testDescriptor = new TestDescriptorsWrapper(testableName);
		_testDescriptorWrappers.Add(testDescriptor);
		return testDescriptor;
	}
}
