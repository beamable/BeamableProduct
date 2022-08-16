using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class TestDescriptor
{
	public string MethodName => _methodName;
	public string Title
	{
		get => _title;
		set => _title = value;
	}
	public string Description
	{
		get => _description;
		set => _description = value;
	}

	[SerializeField] private string _methodName;
	[SerializeField] private string _title;
	[SerializeField] private string _description;

	public TestDescriptor(string methodName)
	{
		_methodName = methodName;
	}
	public TestDescriptor(MethodInfo methodInfo)
	{
		_methodName = methodInfo.Name;
	}
}
