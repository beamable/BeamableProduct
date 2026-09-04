#include "BeamCampaignIntentData.h"

#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"

namespace BeamCampaignIntent
{
	/** Keys that land in a typed field, so they must not be duplicated into Extras. */
	static const TSet<FString>& ReservedKeys()
	{
		static const TSet<FString> Keys = {
			TEXT("campaignId"), TEXT("nodeId"), TEXT("gamerTag"), TEXT("accountId"), TEXT("cidPid"),
			TEXT("deeplink"), TEXT("deepLink"), TEXT("deep_link"),
			TEXT("outreachId"), TEXT("beam_outreach"), TEXT("trackId"),
			TEXT("offers"), TEXT("campaignData"),
			TEXT("beam_offer_grant"), TEXT("beam_offer_grants"),
			// Fields ParseNotification already lifted onto FBMNNotificationData.
			TEXT("userInfo"), TEXT("dataPayload"), TEXT("id"), TEXT("actionId"), TEXT("wasLaunch"),
		};
		return Keys;
	}

	static FString ValueToString(const TSharedPtr<FJsonValue>& Value)
	{
		if (!Value.IsValid()) return FString();
		switch (Value->Type)
		{
		case EJson::String:  return Value->AsString();
		case EJson::Number:  { const double D = Value->AsNumber();
		                       return FMath::IsNearlyEqual(D, FMath::RoundToDouble(D))
		                           ? FString::Printf(TEXT("%lld"), static_cast<int64>(D))
		                           : FString::SanitizeFloat(D); }
		case EJson::Boolean: return Value->AsBool() ? TEXT("true") : TEXT("false");
		case EJson::Object:
		case EJson::Array:
			{
				// Keep structured passthrough usable rather than dropping it.
				FString Out;
				const TSharedRef<TJsonWriter<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>> Writer = TJsonWriterFactory<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>::Create(&Out);
				FJsonSerializer::Serialize(Value.ToSharedRef(), FString(), Writer);
				return Out;
			}
		default: return FString();
		}
	}

	/** Reads a field from the first bucket that has a non-empty value for it. */
	static FString FirstNonEmpty(const TArray<TSharedPtr<FJsonObject>>& Buckets, std::initializer_list<const TCHAR*> Keys)
	{
		for (const TSharedPtr<FJsonObject>& Bucket : Buckets)
		{
			if (!Bucket.IsValid()) continue;
			for (const TCHAR* Key : Keys)
			{
				if (const TSharedPtr<FJsonValue> Value = Bucket->TryGetField(Key))
				{
					const FString Text = ValueToString(Value);
					if (!Text.IsEmpty()) return Text;
				}
			}
		}
		return FString();
	}

	/** Serializes a JSON value back to a compact string, so raw passthrough survives the trip. */
	static FString ToJsonString(const TSharedPtr<FJsonValue>& Value)
	{
		if (!Value.IsValid()) return FString();
		if (Value->Type == EJson::String) return Value->AsString(); // already a JSON string (Android)
		return ValueToString(Value);
	}

	static FBeamCampaignPushOffer ReadOffer(const TSharedPtr<FJsonObject>& Bag)
	{
		FBeamCampaignPushOffer Out;
		if (!Bag.IsValid()) return Out;

		Bag->TryGetStringField(TEXT("itemId"), Out.ItemId);

		if (const TSharedPtr<FJsonValue> Value = Bag->TryGetField(TEXT("value")))
		{
			// "string | number" --- remember which arrived so the funnel can re-emit it faithfully.
			if (Value->Type == EJson::Number)
			{
				Out.bValueIsNumber = true;
				Out.ValueAsNumber  = Value->AsNumber();
			}
			Out.ValueAsString = ValueToString(Value);
		}

		if (const TSharedPtr<FJsonValue> Custom = Bag->TryGetField(TEXT("customData")))
			Out.CustomDataJson = ToJsonString(Custom);

		return Out;
	}

	/**
	 * Reads `offers`, which is a real array on iOS and a JSON *string* on Android. Both are accepted;
	 * this branch is the single place the platform divergence is reconciled.
	 */
	static void ReadOffers(const TArray<TSharedPtr<FJsonObject>>& Buckets, TArray<FBeamCampaignPushOffer>& Out)
	{
		Out.Reset();
		for (const TSharedPtr<FJsonObject>& Bucket : Buckets)
		{
			if (!Bucket.IsValid()) continue;
			const TSharedPtr<FJsonValue> Value = Bucket->TryGetField(TEXT("offers"));
			if (!Value.IsValid()) continue;

			const TArray<TSharedPtr<FJsonValue>>* Array = nullptr;

			if (Value->Type == EJson::Array)
			{
				Array = &Value->AsArray();
			}
			else if (Value->Type == EJson::String)
			{
				TArray<TSharedPtr<FJsonValue>> Parsed;
				const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Value->AsString());
				if (FJsonSerializer::Deserialize(Reader, Parsed))
				{
					for (const TSharedPtr<FJsonValue>& Entry : Parsed)
						if (Entry.IsValid() && Entry->Type == EJson::Object)
							Out.Add(ReadOffer(Entry->AsObject()));
				}
				return;
			}

			if (Array)
			{
				for (const TSharedPtr<FJsonValue>& Entry : *Array)
					if (Entry.IsValid() && Entry->Type == EJson::Object)
						Out.Add(ReadOffer(Entry->AsObject()));
				return;
			}
		}
	}
}

FBeamCampaignIntentData UBeamCampaignIntentLibrary::ParseFromNotification(const FBMNNotificationData& Notification)
{
	FBeamCampaignIntentData Intent = ParseFromRawJson(Notification.RawJson);

	// ParseNotification already resolved the deep link across every spelling and nesting; trust it
	// when the campaign fields alone did not produce one.
	if (Intent.Deeplink.IsEmpty()) Intent.Deeplink = Notification.DeepLink;

	// Title and body are lifted onto the notification itself, but a campaign screen wants them too.
	if (!Notification.Title.IsEmpty()) Intent.Extras.Add(TEXT("title"), Notification.Title);
	if (!Notification.Body.IsEmpty())  Intent.Extras.Add(TEXT("body"), Notification.Body);

	return Intent;
}

FBeamCampaignIntentData UBeamCampaignIntentLibrary::ParseFromRawJson(const FString& RawJson)
{
	using namespace BeamCampaignIntent;

	FBeamCampaignIntentData Intent;
	if (RawJson.IsEmpty()) return Intent;

	TSharedPtr<FJsonObject> Root;
	const TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(RawJson);
	if (!FJsonSerializer::Deserialize(Reader, Root) || !Root.IsValid()) return Intent;

	// Look in the top level first, then the two places a platform may have nested the payload.
	TArray<TSharedPtr<FJsonObject>> Buckets;
	Buckets.Add(Root);
	const TSharedPtr<FJsonObject>* Nested = nullptr;
	if (Root->TryGetObjectField(TEXT("userInfo"), Nested) && Nested)    Buckets.Add(*Nested);
	if (Root->TryGetObjectField(TEXT("dataPayload"), Nested) && Nested) Buckets.Add(*Nested);

	Intent.CampaignId = FirstNonEmpty(Buckets, {TEXT("campaignId")});
	Intent.NodeId     = FirstNonEmpty(Buckets, {TEXT("nodeId")});
	Intent.GamerTag   = FirstNonEmpty(Buckets, {TEXT("gamerTag")});
	Intent.AccountId  = FirstNonEmpty(Buckets, {TEXT("accountId")});
	Intent.CidPid     = FirstNonEmpty(Buckets, {TEXT("cidPid")});
	Intent.TrackId    = FirstNonEmpty(Buckets, {TEXT("trackId")});
	Intent.Deeplink   = FirstNonEmpty(Buckets, {TEXT("deeplink"), TEXT("deepLink"), TEXT("deep_link")});

	// iOS spells it outreachId, Android beam_outreach. Both natives accept outreachId on the way out.
	Intent.OutreachId = FirstNonEmpty(Buckets, {TEXT("outreachId"), TEXT("beam_outreach")});

	ReadOffers(Buckets, Intent.Offers);

	for (const TSharedPtr<FJsonObject>& Bucket : Buckets)
	{
		if (!Bucket.IsValid()) continue;
		if (const TSharedPtr<FJsonValue> Value = Bucket->TryGetField(TEXT("campaignData")))
		{
			Intent.CampaignDataJson = ToJsonString(Value);
			if (!Intent.CampaignDataJson.IsEmpty()) break;
		}
	}

	// Grants: a single id, or a comma-joined list. Either may be the only one present.
	Intent.OfferGrantId = FirstNonEmpty(Buckets, {TEXT("beam_offer_grant")});
	const FString GrantList = FirstNonEmpty(Buckets, {TEXT("beam_offer_grants")});
	if (!GrantList.IsEmpty())
	{
		TArray<FString> Parts;
		GrantList.ParseIntoArray(Parts, TEXT(","), true);
		for (FString& Part : Parts)
		{
			Part.TrimStartAndEndInline();
			if (!Part.IsEmpty()) Intent.OfferGrantIds.Add(Part);
		}
	}
	if (Intent.OfferGrantId.IsEmpty() && Intent.OfferGrantIds.Num() > 0) Intent.OfferGrantId = Intent.OfferGrantIds[0];

	// Everything else, coerced to string. Earlier buckets win so the top level stays authoritative.
	for (const TSharedPtr<FJsonObject>& Bucket : Buckets)
	{
		if (!Bucket.IsValid()) continue;
		for (const TPair<FString, TSharedPtr<FJsonValue>>& Pair : Bucket->Values)
		{
			if (ReservedKeys().Contains(Pair.Key)) continue;
			if (Intent.Extras.Contains(Pair.Key)) continue;
			const FString Text = ValueToString(Pair.Value);
			if (!Text.IsEmpty()) Intent.Extras.Add(Pair.Key, Text);
		}
	}

	return Intent;
}

FString UBeamCampaignIntentLibrary::BuildOfferTrackRequestJson(const FBeamCampaignIntentData& Intent, const FBeamCampaignPushOffer& Offer)
{
	FString Out;
	const TSharedRef<TJsonWriter<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>> Writer = TJsonWriterFactory<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>::Create(&Out);
	Writer->WriteObjectStart();

	Writer->WriteValue(TEXT("campaignId"), Intent.CampaignId);
	Writer->WriteValue(TEXT("nodeId"), Intent.NodeId);
	Writer->WriteValue(TEXT("gamerTag"), Intent.GamerTag);
	Writer->WriteValue(TEXT("accountId"), Intent.AccountId);
	Writer->WriteValue(TEXT("cidPid"), Intent.CidPid);
	Writer->WriteValue(TEXT("deeplink"), Intent.Deeplink);
	Writer->WriteValue(TEXT("outreachId"), Intent.OutreachId);
	Writer->WriteValue(TEXT("trackId"), Intent.TrackId);

	// Both natives take a nullable offer, so omit the member entirely rather than sending a blank one.
	if (!Offer.ItemId.IsEmpty())
	{
		Writer->WriteObjectStart(TEXT("offer"));
		Writer->WriteValue(TEXT("itemId"), Offer.ItemId);

		// Re-emit a number as a number: quoting it would change the funnel's payload type.
		if (Offer.bValueIsNumber) Writer->WriteValue(TEXT("value"), Offer.ValueAsNumber);
		else if (!Offer.ValueAsString.IsEmpty()) Writer->WriteValue(TEXT("value"), Offer.ValueAsString);

		if (!Offer.CustomDataJson.IsEmpty())
			Writer->WriteRawJSONValue(TEXT("customData"), Offer.CustomDataJson);

		Writer->WriteObjectEnd();
	}

	Writer->WriteObjectEnd();
	Writer->Close();
	return Out;
}

FString UBeamCampaignIntentLibrary::BuildAuthConfigJson(const FString& AccessToken, const FString& RefreshToken, int64 ExpiresAtEpochMs, const FString& Cid, const FString& Pid, const FString& Host)
{
	FString Out;
	const TSharedRef<TJsonWriter<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>> Writer = TJsonWriterFactory<TCHAR, TCondensedJsonPrintPolicy<TCHAR>>::Create(&Out);
	Writer->WriteObjectStart();
	Writer->WriteValue(TEXT("accessToken"), AccessToken);
	Writer->WriteValue(TEXT("refreshToken"), RefreshToken);
	Writer->WriteValue(TEXT("accessTokenExpiresAt"), static_cast<double>(ExpiresAtEpochMs));
	Writer->WriteValue(TEXT("cid"), Cid);
	Writer->WriteValue(TEXT("pid"), Pid);
	Writer->WriteValue(TEXT("host"), Host);
	Writer->WriteObjectEnd();
	Writer->Close();
	return Out;
}
