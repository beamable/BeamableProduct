using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Common.Shop;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Common.Announcements
{
	/// <summary>
	/// This type defines a %Beamable %ContentObject subclass for the %AnnouncementsService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// - See Beamable.Api.Announcements.AnnouncementsService script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	[ContentType("announcements")]
	[System.Serializable]
	[Agnostic]
	public class AnnouncementContent : ContentObject
	{
		[Tooltip("The category of the announcement")]
		[CannotBeBlank]
		public string channel = "main";

		[Tooltip("The title of the announcement")]
		[CannotBeBlank]
		public string title = "title";

		[Tooltip("A summary of the announcement")]
		[CannotBeBlank]
		public string summary = "summary";

		[Tooltip("A main body of the announcement")]
		[TextArea(10, 10)]
		[CannotBeBlank]
		public string body = "body";

		[Tooltip(ContentObject.TooltipOptional0 +
				 "The startDate specifies when the announcement becomes available for players to see. If no startDate is specified, the announcement will become visible immediately " +
				 ContentObject.TooltipStartDate2)]
		[FormerlySerializedAs("start_date")]
		[MustBeDateString]
		[ContentField("start_date")]
		public OptionalString startDate;

		[Tooltip(ContentObject.TooltipOptional0 +
				 "The endDate specifies when the announcement stops being available for players to see. If no endDate is specified, the announcement will be visible forever " +
				 ContentObject.TooltipEndDate2)]
		[FormerlySerializedAs("end_date")]
		[MustBeDateString]
		[ContentField("end_date")]
		public OptionalString endDate;

		[Tooltip(ContentObject.TooltipAttachment1)]
		public List<AnnouncementAttachment> attachments;

		[Tooltip(ContentObject.TooltipOptional0 +
				 "If specified, stat requirements will limit the audience of this announcement based on player stats")]
		[ContentField("stat_requirements")]
		public OptionalStats statRequirements;

		[Tooltip(ContentObject.TooltipOptional0 + "If specified, the client data")]
		public OptionalSerializableDictionaryStringToString clientData;
	}
}
