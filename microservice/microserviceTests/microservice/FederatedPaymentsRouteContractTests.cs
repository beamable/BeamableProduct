using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Common;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice;

/// <summary>
/// Guards the payments federation route contract.
///
/// <para>Two failure modes make these worth pinning. A method added to
/// <see cref="IFederatedPayments{T}"/> without a route entry throws only when a service starts, which
/// is late and far from the change that caused it. And a path part that drifts from what the platform
/// requests fails <i>silently</i> — the platform gets a 404 it reports as a federation error, with
/// nothing pointing at the mismatch.</para>
/// </summary>
/// <summary>
/// Federation id used only to close the generic on <see cref="IFederatedPayments{T}"/> for reflection.
/// The platform's own DummyThirdParty is internal, and this test is about the interface surface
/// rather than any particular id.
/// </summary>
[FederationId("payments-route-test")]
public class PaymentsRouteTestFederation : IFederationId
{
}

public class FederatedPaymentsRouteContractTests
{
	/// <summary>
	/// The path parts the Beamable platform requests, mirroring its <c>FederatedPaymentPaths</c>.
	/// Written out literally rather than derived, so a change on either side has to be made twice
	/// and deliberately.
	/// </summary>
	private static readonly Dictionary<string, string> ExpectedPaths = new()
	{
		[nameof(IFederatedPayments<PaymentsRouteTestFederation>.BeginPayment)] = "BeginPayment",
		[nameof(IFederatedPayments<PaymentsRouteTestFederation>.VerifyPayments)] = "VerifyPayments",
		[nameof(IFederatedPayments<PaymentsRouteTestFederation>.VerifyReceipt)] = "VerifyReceipt",
		[nameof(IFederatedPayments<PaymentsRouteTestFederation>.FulfillPayment)] = "FulfillPayment",
	};

	private static IEnumerable<MethodInfo> PaymentMethods =>
		typeof(IFederatedPayments<PaymentsRouteTestFederation>).GetMethods();

	[Test]
	public void EveryInterfaceMethod_HasARoute()
	{
		foreach (var method in PaymentMethods)
		{
			Assert.IsTrue(
				ServiceMethodHelper.FederatedMethodPaths.ContainsKey(method.Name),
				$"IFederatedPayments.{method.Name} has no entry in " +
				$"{nameof(ServiceMethodHelper)}.{nameof(ServiceMethodHelper.FederatedMethodPaths)}, " +
				"so a service implementing it will throw on startup.");
		}
	}

	[Test]
	public void RoutesMatchThePlatformContract()
	{
		foreach (var (methodName, expectedPath) in ExpectedPaths)
		{
			Assert.IsTrue(
				ServiceMethodHelper.FederatedMethodPaths.TryGetValue(methodName, out var actual),
				$"Missing route for IFederatedPayments.{methodName}.");
			Assert.AreEqual(expectedPath, actual,
				$"Route for IFederatedPayments.{methodName} does not match what the platform requests.");
		}
	}

	[Test]
	public void PaymentRoutesAreDistinct()
	{
		// Federated routes are registered by path alone, with no HTTP verb, so two payment operations
		// sharing a path part would silently collide instead of being rejected.
		var paths = ExpectedPaths.Values.ToList();
		Assert.AreEqual(paths.Count, paths.Distinct().Count(),
			"Payment operations must each have their own path part.");
	}

	[Test]
	public void InterfaceSurfaceIsFullyCovered()
	{
		// Catches an interface method added without also being added to ExpectedPaths above — i.e.
		// added to the SDK without anyone agreeing a route with the platform.
		var declared = PaymentMethods.Select(m => m.Name).OrderBy(n => n).ToList();
		var expected = ExpectedPaths.Keys.OrderBy(n => n).ToList();
		Assert.AreEqual(expected, declared,
			"IFederatedPayments methods and the expected route list have diverged.");
	}
}
