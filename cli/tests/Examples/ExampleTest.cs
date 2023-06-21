using NUnit.Framework;
using System;
using System.IO;

namespace tests.Examples;

[NonParallelizable]
public class ExampleTest
{
	protected string WorkingDir => Path.Combine(_originalWorkingDir, "testRuns", TestId);
	protected string TestId { get; private set; }
	protected int x = 0;
	private string _originalWorkingDir;

	[SetUp]
	public void Setup()
	{
		TestId = Guid.NewGuid().ToString();

		_originalWorkingDir = Directory.GetCurrentDirectory();
		
		Directory.CreateDirectory(WorkingDir);
		Directory.SetCurrentDirectory(WorkingDir);

		// TODO: create a unique directory for the current test.
		x = 0;
	}

	[TearDown]
	public void Teardown()
	{
		Directory.SetCurrentDirectory(_originalWorkingDir);
	}

	protected void Run(string command)
	{
		Cli.RunWithParams(command);
	}
	
}
