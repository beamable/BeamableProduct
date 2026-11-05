using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beamable.Common;
using Beamable.Server;
using Beamable.Server.Common;
using beamable.tooling.common.Microservice;
using microserviceTests.microservice.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice;

/// <summary>
/// <see cref="IFederatedVirtualOffer{T}"/> must route and bind the way the backend calls it: POST
/// {federationId}/{Method} with a body whose fields are the method's parameter names.
/// </summary>
[TestFixture]
public class OfferFederationTests
{
	const string FederationId = "test_offer_store";

	[FederationId(FederationId)]
	public class TestOfferStore : IFederationId { }

	[Microservice("offer_test", EnableEagerContentLoading = false)]
	public class OfferService : Microservice, IFederatedVirtualOffer<TestOfferStore>
	{
		public Promise<OfferGrantBatchResponse> GrantOffers(List<OfferGrantItem> grants) => null;
		public Promise<OfferRevokeBatchResponse> RevokeOffer(List<OfferGrantRef> revokes) => null;
		public Promise<OfferRedeemResponse> RedeemOffer(OfferGrantRef grant, Dictionary<string, string> @params) => null;
		public Promise<List<OfferAvailability>> GetOffers(List<OfferGrantRef> grants) => null;
	}

	[SetUp]
	public void Setup() => LoggingUtil.InitTestCorrelator();

	static ServiceMethodCollection Scan() => ServiceMethodHelper.Scan(
		new MicroserviceAttribute("offer_test"),
		new ServiceMethodProvider { instanceType = typeof(OfferService), pathPrefix = "", clientPrefix = "" });

	static object[] Bind(string path, string body)
	{
		var method = Scan().Methods.Single(m => m.Path == $"{FederationId}/{path}");
		using var doc = JsonDocument.Parse(body);
		var ctx = new MicroserviceRequestContext("cid", "pid") { BodyElement = doc.RootElement.Clone() };
		return new AdaptiveParameterProvider(ctx).GetParameters(method, null);
	}

	[Test]
	public void Methods_RouteOnTheirLiteralNames()
	{
		var paths = Scan().Methods.Select(m => m.Path).ToList();

		Assert.That(paths, Is.SupersetOf(new[]
		{
			$"{FederationId}/GrantOffers", $"{FederationId}/RevokeOffer", $"{FederationId}/RedeemOffer",
			$"{FederationId}/GetOffers"
		}));
	}

	[Test]
	public void Service_RegistersOneComponent()
	{
		var components = FederatedComponentGenerator.FindFederatedComponents(typeof(OfferService));

		Assert.That(components.Select(c => c.typeName), Is.EqualTo(new[] { "IFederatedVirtualOffer" }));
		Assert.That(components.Select(c => c.identity.GetUniqueName()), Is.EqualTo(new[] { FederationId }));
	}

	[Test]
	public void GrantOffers_BindsTheGrantsTheBackendPosts()
	{
		var args = Bind("GrantOffers", @"{""grants"":[
			{""grantId"":""g1"",""playerId"":""1"",""offerId"":""offer.a"",""expiresAtUnixSeconds"":1700000000,
			 ""metadata"":{""beam_campaign"":""c1"",""offer_x"":""y""}},
			{""grantId"":""g2"",""playerId"":""2"",""offerId"":""offer.b"",""expiresAtUnixSeconds"":1700000001}
		]}");

		var grants = (List<OfferGrantItem>)args.Single();
		Assert.That(grants.Select(g => g.grantId), Is.EqualTo(new[] { "g1", "g2" }));
		Assert.That(grants.Select(g => g.playerId), Is.EqualTo(new[] { "1", "2" }));
		Assert.That(grants[0].expiresAtUnixSeconds, Is.EqualTo(1700000000));
		Assert.That(grants[0].metadata[OfferContract.MetadataCampaign], Is.EqualTo("c1"));
		Assert.That(grants[0].metadata["offer_x"], Is.EqualTo("y"));
	}

	[Test]
	public void RedeemOffer_BindsTheGrantAndItsParams()
	{
		var args = Bind("RedeemOffer", @"{
			""grant"":{""grantId"":""g1"",""playerId"":""1"",""offerId"":""offer.a"",
			  ""offer"":{""offerId"":""offer.a"",""title"":""A""},
			  ""metadata"":{""m"":""1""},""providerMetadata"":{""ref"":""r1""}},
			""params"":{""choice"":""gold""}
		}");

		Assert.That(args, Has.Length.EqualTo(2));
		var grant = (OfferGrantRef)args[0];
		Assert.That(grant.grantId, Is.EqualTo("g1"));
		Assert.That(grant.offer.title, Is.EqualTo("A"));
		Assert.That(grant.providerMetadata["ref"], Is.EqualTo("r1"));
		Assert.That(((Dictionary<string, string>)args[1])["choice"], Is.EqualTo("gold"));
	}

	/// <summary>The backend reads the outcome by name, so an ordinal must never reach it.</summary>
	[TestCase(OfferRedeemOutcome.Settled, "Settled")]
	[TestCase(OfferRedeemOutcome.Refused, "Refused")]
	[TestCase(OfferRedeemOutcome.Ambiguous, "Ambiguous")]
	public void RedeemOutcome_TravelsByName(OfferRedeemOutcome outcome, string expected)
	{
		var json = JsonConvert.SerializeObject(
			new OfferRedeemResponse { grantId = "g1", outcome = outcome }, UnitySerializationSettings.Instance);

		Assert.That(json, Does.Contain($"\"outcome\":\"{expected}\""));
	}

	/// <summary>
	/// Grant and revoke answer an envelope the backend reads by field name: the items, a throttled item's own
	/// wait, and the pace asked for the whole provider.
	/// </summary>
	[Test]
	public void BatchAnswers_TravelAsTheEnvelopeTheBackendReads()
	{
		var grant = JObject.Parse(JsonConvert.SerializeObject(new OfferGrantBatchResponse
		{
			grants = new List<OfferGrantResponse> { new OfferGrantResponse { grantId = "g1", throttled = true, retryAfterMs = 30000 } },
			maxInFlight = 4
		}, UnitySerializationSettings.Instance));
		var revoke = JObject.Parse(JsonConvert.SerializeObject(
			new OfferRevokeBatchResponse { retryAfterMs = 5000 }, UnitySerializationSettings.Instance));

		Assert.That(grant["grants"]![0]!["throttled"]!.Value<bool>(), Is.True);
		Assert.That(grant["grants"]![0]!["retryAfterMs"]!.Value<long>(), Is.EqualTo(30000));
		Assert.That(grant["maxInFlight"]!.Value<int>(), Is.EqualTo(4));
		Assert.That(revoke["revokes"], Is.Not.Null);
		Assert.That(revoke["retryAfterMs"]!.Value<long>(), Is.EqualTo(5000));
	}
}
