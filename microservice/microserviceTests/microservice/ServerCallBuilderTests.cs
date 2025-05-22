using Beamable.Common.Scheduler;
using Beamable.Server;
using Beamable.Server.Api.Scheduler;
using NUnit.Framework;

namespace microserviceTests.microservice;

public class ServerCallBuilderTests
{
	/// <summary>
	/// Given simple microservice scheduling
	/// When the *deployment* is local and *useLocal* is true
	/// The routing key should include the local prefix
	/// </summary>
	[Test]
	public void GetServiceMethodInfo_Local_UseLocalTrue()
	{
		var testArgs = new TestArgs { NamePrefix = "machine_1a2b3c4d" };
		var attr = new MicroserviceAttribute("MyService");
		var context = new SchedulerContext(testArgs, attr);
		var builder = new ServiceCallBuilder<SimpleMicroservice>(useLocal: true, context);

		ServiceAction action = builder.Run(x => x.PromiseServerTestMethod);

		Assert.AreEqual(
			$"micro_MyService:machine_1a2b3c4d",
			action.routingKey.Value,
			"Routing key should map service name to local machine prefix."
		);
	}

	/// <summary>
	/// Given simple microservice scheduling
	/// When the *deployment* is local and *useLocal* is false
	/// The routing key should be completely empty
	/// </summary>
	[Test]
	public void GetServiceMethodInfo_Local_UseLocalFalse()
	{
		var testArgs = new TestArgs { NamePrefix = "machine_1a2b3c4d" };
		var attr = new MicroserviceAttribute("MyService");
		var context = new SchedulerContext(testArgs, attr);
		var builder = new ServiceCallBuilder<SimpleMicroservice>(useLocal: false, context);

		ServiceAction action = builder.Run(x => x.PromiseServerTestMethod);

		Assert.IsFalse(
			action.routingKey.HasValue,
			$"Routing key should be absent when useLocal is false. routingKey={action.routingKey.Value}"
		);
	}

	/// <summary>
	/// Given simple microservice scheduling
	/// When the *deployment* is remote and *useLocal* is true
	/// The routing key should be completely empty
	/// </summary>
	[Test]
	public void GetServiceMethodInfo_Cloud_UseLocalTrue()
	{
		var testArgs = new TestArgs { NamePrefix = "" };
		var attr = new MicroserviceAttribute("MyService");
		var context = new SchedulerContext(testArgs, attr);
		var builder = new ServiceCallBuilder<SimpleMicroservice>(useLocal: true, context);

		ServiceAction action = builder.Run(x => x.PromiseServerTestMethod);

		Assert.IsFalse(
			action.routingKey.HasValue,
			$"Routing key should be absent when service is remote. routingKey='{action.routingKey.Value}'"
		);
	}

	/// <summary>
	/// Given simple microservice scheduling
	/// When the *deployment* is remote and *useLocal* is false
	/// The routing key should be completely empty
	/// </summary>
	[Test]
	public void GetServiceMethodInfo_Cloud_UseLocalFalse()
	{
		var testArgs = new TestArgs { NamePrefix = "" };
		var attr = new MicroserviceAttribute("MyService");
		var context = new SchedulerContext(testArgs, attr);
		var builder = new ServiceCallBuilder<SimpleMicroservice>(useLocal: false, context);

		ServiceAction action = builder.Run(x => x.PromiseServerTestMethod);

		Assert.IsFalse(
			action.routingKey.HasValue,
			$"Routing key should be absent when service is remote. routingKey={action.routingKey.Value}"
		);
	}
}
