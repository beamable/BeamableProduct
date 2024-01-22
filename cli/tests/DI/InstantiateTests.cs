using Beamable.Common.Dependencies;
using NUnit.Framework;
using System;

namespace tests.DI;

public class InstantiateTests
{
	[Test]
	public void TestConstructorFailure()
	{
		var builder = new DependencyBuilder();
		builder.AddSingleton<FailsOnConstructor>();

		var provider = builder.Build();

		var ex = Assert.Throws<IndexOutOfRangeException>(
			() => provider.GetService<FailsOnConstructor>());

		// this assertion makes sure the stack trace isn't starting with the re-capture/throw in the DI framework, 
		//  but instead, is coming from the actual site of failure.
		Console.WriteLine("STACK:" + ex.StackTrace);
		Assert.That(ex.StackTrace.StartsWith("   at tests.DI.InstantiateTests.FailsOnConstructor..ctor()"));
	}

	[Test]
	public void ArgFailsOnConstructor()
	{
		var builder = new DependencyBuilder();
		builder.AddSingleton<FailsOnConstructor>(p => throw new NotImplementedException("haha"));
		builder.AddSingleton<UsesFailingDep>();

		var provider = builder.Build();

		var ex = Assert.Throws<NotImplementedException>(
			() => provider.GetService<UsesFailingDep>());

		// this assertion makes sure the stack trace isn't starting with the re-capture/throw in the DI framework, 
		//  but instead, is coming from the actual site of failure.
		Console.WriteLine("STACK:" + ex.StackTrace);
		Assert.That(ex.StackTrace.StartsWith("   at tests.DI.InstantiateTests.<>c.<ArgFailsOnConstructor>b__1_0(IDependencyProvider p)"));
	}

	class FailsOnConstructor
	{
		public FailsOnConstructor()
		{
			var array = new int[2];
			var x = array[4]; // THROW IndexOutOfRangeException
		}
	}

	class UsesFailingDep
	{
		public UsesFailingDep(FailsOnConstructor service)
		{

		}
	}
}
