using System;
using System.Collections;
using Beamable.Common;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice;

public class FederationDictionarySerializationTests
{
	private static IEnumerable DictionaryCases()
	{
		var fields = new[]
		{
			(typeof(MessageRailRecipient), "data"),
			(typeof(CampaignOfferAmount), "properties"),
			(typeof(CampaignOfferItem), "localizations"),
			(typeof(CampaignOfferItem), "properties"),
			(typeof(CampaignOfferReason), "properties"),
			(typeof(CampaignOfferGrantContext), "extraDataFed"),
			(typeof(CampaignOfferRedeemRequest), "params")
		};
		foreach (var (type, field) in fields)
		foreach (var state in new[] { "missing", "null", "empty", "populated" })
			yield return new TestCaseData(type, field, state)
				.SetName($"{type.Name}_{field}_{state}");
	}

	[TestCaseSource(nameof(DictionaryCases))]
	public void FederationJsonPreservesDictionary(Type type, string name, string state)
	{
		var value = state switch
		{
			"null" => "null",
			"populated" when name == "localizations" =>
				"{\"en\":{\"title\":\"Weekly Bundle\",\"description\":\"Gold and gems\"},\"pt\":{\"title\":\"Pacote\",\"description\":\"Ouro\"}}",
			"populated" => "{\"beam_offer_grants\":\"g1\",\"empty\":\"\"}",
			_ => "{}"
		};
		var input = state == "missing" ? "{}" : $"{{\"{name}\":{value}}}";
		var settings = UnitySerializationSettings.Instance;
		var instance = JsonConvert.DeserializeObject(input, type, settings);
		var field = type.GetField(name);
		Assert.That(field, Is.Not.Null, "The public field is part of the federation API.");
		var dictionary = (IDictionary)field.GetValue(instance);
		if (state == "null")
			Assert.That(dictionary, Is.Null);
		else
			Assert.That(dictionary, Has.Count.EqualTo(state == "populated" ? 2 : 0));

		var output = JObject.Parse(JsonConvert.SerializeObject(instance, settings));
		Assert.That(output.Property(name), Is.Not.Null, "The transport must include the field, including null.");
		Assert.That(JToken.DeepEquals(output[name], JToken.Parse(value)), Is.True, output.ToString());
		var readback = JsonConvert.DeserializeObject(output.ToString(), type, settings);
		var roundTrip = JObject.Parse(JsonConvert.SerializeObject(readback, settings));
		Assert.That(JToken.DeepEquals(roundTrip[name], output[name]), Is.True);
	}

	[Test]
	public void RecipientReadsBackendWireFixture()
	{
		// BeamableShared.Tests/Federation/MessageRailWireShapeTest.Recipient_CarriesItsOwnData.
		const string json = "{\"gamerTag\":5,\"outreachId\":\"o-5\",\"openTrackingUrl\":\"\",\"data\":{\"beam_offer_grants\":\"g1\"}}";
		var recipient = JsonConvert.DeserializeObject<MessageRailRecipient>(json, UnitySerializationSettings.Instance);
		Assert.That(recipient.gamerTag, Is.EqualTo(5));
		Assert.That(recipient.outreachId, Is.EqualTo("o-5"));
		Assert.That(recipient.openTrackingUrl, Is.Empty);
		Assert.That(recipient.data["beam_offer_grants"], Is.EqualTo("g1"));
		Assert.That(JToken.DeepEquals(JToken.Parse(json),
			JToken.Parse(JsonConvert.SerializeObject(recipient, UnitySerializationSettings.Instance))), Is.True);
	}
}
