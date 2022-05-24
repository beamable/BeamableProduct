using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.Groups
{

	/// <summary>
	/// This type defines a %Beamable %ContentObject subclass for the %GroupsService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// - See Beamable.Api.Groups.GroupsService script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	[ContentType("donations")]
	[System.Serializable]
	[Agnostic]
	public class GroupDonationsContent : ContentObject, ISerializationCallbackReceiver
	{
		[Tooltip(ContentObject.TooltipRequestCooldown1)]
		[MustBeNonNegative]
		public long requestCooldownSecs;

		// TODO: This really "should" be a list of currency ref but refs don't serialize to just strings currently.
		[Tooltip(ContentObject.TooltipAllowedCurrency1)]
		[MustBeCurrency]
		public List<string> allowedCurrencies;
		
		// TODO TD985946 Instead of validating those string values we should have a dropdown with already valid options
		public void OnBeforeSerialize()
		{
			if (allowedCurrencies != null)
			{
				for (int i = 0; i < allowedCurrencies.Count; i++)
				{
					if (allowedCurrencies[i] != null && !allowedCurrencies[i].Contains('.') && !string.IsNullOrWhiteSpace(allowedCurrencies[i]))
					{
						allowedCurrencies[i] = $"currency.{allowedCurrencies[i]}";
					}
				}
			}
		}

		public void OnAfterDeserialize()
		{
			// do nothing
		}
	}

}
