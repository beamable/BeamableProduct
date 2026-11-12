using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Common.Inventory;
using UnityEngine;

// Staged by the agent to give the 5.1.1 MustReferenceContent picker an obvious,
// top-level test surface (the built-in CurrencyContent only carries the attribute on a
// field buried inside a nested CurrencyReward list).
//
// After this compiles, the Content Manager should offer a new "smoke_ref" content type.
// Create one and inspect it:
//
//   currency   (CurrencyRef) -> [MustReferenceContent] picker. Expect a dropdown that
//                               filters to CurrencyContent (currency.gems / currency.coins).
//                               Test: valid pick, clear the pick, confirm wrong types are
//                               not offered.
//
//   currencyId (string)      -> [MustReferenceContent] on a string field. Type a bogus id
//                               (e.g. "currency.does_not_exist"). 5.1.1 change: the invalid
//                               value must be FLAGGED by validation and NOT silently
//                               auto-rewritten/cleared. Confirm your typed text stays put.
namespace BeamQA
{
	[ContentType("smoke_ref")]
	public class SmokeReferenceContent : ContentObject
	{
		[Tooltip("Picker should filter to CurrencyContent.")]
		[MustReferenceContent]
		public CurrencyRef currency;

		[Tooltip("Invalid ids should be flagged, not auto-rewritten.")]
		[MustReferenceContent(true, typeof(CurrencyContent))]
		public string currencyId;
	}
}
