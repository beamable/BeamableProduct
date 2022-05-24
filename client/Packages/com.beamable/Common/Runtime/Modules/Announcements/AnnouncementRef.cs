using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.Announcements
{
	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/content-code#contentlink-vs-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	[Agnostic]
	public class AnnouncementLink : ContentLink<AnnouncementContent> { }

	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/content-code#contentlink-vs-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[Agnostic]
	[System.Serializable]
	public class AnnouncementRef : AnnouncementRef<AnnouncementContent> { }

	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/content-code#contentlink-vs-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	public class AnnouncementRef<TContent> : ContentRef<TContent> where TContent : AnnouncementContent, new() { }

	[System.Serializable]
	[Agnostic]
	public class AnnouncementAttachment : ISerializationCallbackReceiver
	{
		[Tooltip("This should be the contentId of the attachment. Either an item id, or a currency id.")]
		[MustBeCurrencyOrItem]
		public string symbol;

		[Tooltip("If the attachment is a currency, how much currency? If the attachment is an item, this should be 1.")]
		[MustBePositive]
		public int count = 1;

		[Tooltip("Must specify the type of the attachment symbol. If you referenced an item in the symbol, this should be \"items\", otherwise it should be \"currency\"")]
		[MustBeOneOf("currency", "items")]
		// TODO: [MustMatchReference(nameof(symbol))]
		public string type;
		
		// TODO TD985946 Instead of validating those string values we should have a dropdown with already valid options
		public void OnBeforeSerialize()
		{
			if (symbol == null)
			{
				return;
			}
			
			var allowedValues = new [] {"currency", "items"};
			var idParts = symbol.Split('.').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
			if (!string.IsNullOrWhiteSpace(type))
			{
				if (idParts.Length > 0 && allowedValues.Contains(type))
				{
					symbol = $"{type}.{idParts.Last()}";
				}
			}
		}

		public void OnAfterDeserialize()
		{
			// do nothing
		}
	}
}
