using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beamable.Common;
using Beamable.Server;
using beamable.tooling.common.Microservice;
using microserviceTests.microservice.Util;
using NUnit.Framework;

namespace microserviceTests.microservice;

/// <summary>
/// Grant on <see cref="IFederatedCampaignVirtualOffer{T}"/> is batch-only: it must route and bind its body
/// the way the backend calls it — POST {federationId}/GrantOffers with {"grants":[...]} — and there is no
/// per-player GrantOffer route.
/// </summary>
[TestFixture]
public class CampaignOfferGrantFederationTests
{
	const string FederationId = "test_offer_store";

	[FederationId(FederationId)]
	public class TestOfferStore : IFederationId { }

	[Microservice("offer_grant_test", EnableEagerContentLoading = false)]
	public class OfferService : Microservice, IFederatedCampaignVirtualOffer<TestOfferStore>
	{
		public Promise<List<CampaignOfferGrantResponse>> GrantOffers(List<CampaignOfferGrantItem> grants) => null;
		public Promise<List<CampaignOfferGrantResponse>> RevokeOffer(List<CampaignOfferRevokeRequest> revokes) => null;
		public Promise<CampaignOfferRedeemResponse> RedeemOffer(string playerId, string grantId, CampaignOfferRedeemRequest request) => null;
		public Promise<CampaignOffersResponse> GetCampaignOffers(string playerId, CampaignOfferFilter filter) => null;
	}

	[SetUp]
	public void Setup() => LoggingUtil.InitTestCorrelator();

	static ServiceMethodCollection Scan() => ServiceMethodHelper.Scan(
		new MicroserviceAttribute("offer_grant_test"),
		new ServiceMethodProvider { instanceType = typeof(OfferService), pathPrefix = "", clientPrefix = "" });

	[Test]
	public void Grant_RoutesOnlyAsBatch_OnLiteralMethodName()
	{
		var paths = Scan().Methods.Select(m => m.Path).ToList();

		Assert.That(paths, Does.Contain($"{FederationId}/GrantOffers"));
		Assert.That(paths, Does.Not.Contain($"{FederationId}/GrantOffer"));
		Assert.That(paths, Does.Contain($"{FederationId}/RevokeOffer"));
		Assert.That(paths, Does.Contain($"{FederationId}/RedeemOffer"));
		Assert.That(paths, Does.Contain($"{FederationId}/GetCampaignOffers"));
	}

	[Test]
	public void Grant_RegistersSingleComponent()
	{
		var components = FederatedComponentGenerator.FindFederatedComponents(typeof(OfferService));

		Assert.That(components.Select(c => c.typeName), Is.EqualTo(new[] { "IFederatedCampaignVirtualOffer" }));
		Assert.That(components.Select(c => c.identity.GetUniqueName()), Is.EqualTo(new[] { FederationId }));
	}

	[Test]
	public void Grant_BindsGrantsFromNamedBody()
	{
		var method = Scan().Methods.Single(m => m.Path == $"{FederationId}/GrantOffers");
		const string body = @"{""grants"":[
			{""playerId"":""1"",""offerId"":""offer.a"",""context"":{""campaignId"":""c1"",""outreachId"":""o1"",""idempotencyKey"":""k1"",""extraDataFed"":{""x"":""y""}}},
			{""playerId"":""2"",""offerId"":""offer.b"",""context"":{""campaignId"":""c1"",""idempotencyKey"":""k2""}}
		]}";
		using var doc = JsonDocument.Parse(body);
		var ctx = new MicroserviceRequestContext("cid", "pid") { BodyElement = doc.RootElement.Clone() };

		var args = new AdaptiveParameterProvider(ctx).GetParameters(method, null);

		Assert.That(args, Has.Length.EqualTo(1));
		var grants = (List<CampaignOfferGrantItem>)args[0];
		Assert.That(grants.Select(g => g.playerId), Is.EqualTo(new[] { "1", "2" }));
		Assert.That(grants.Select(g => g.offerId), Is.EqualTo(new[] { "offer.a", "offer.b" }));
		Assert.That(grants.Select(g => g.context.idempotencyKey), Is.EqualTo(new[] { "k1", "k2" }));
		Assert.That(grants[0].context.outreachId, Is.EqualTo("o1"));
		Assert.That(grants[0].context.extraDataFed["x"], Is.EqualTo("y"));
	}
}
