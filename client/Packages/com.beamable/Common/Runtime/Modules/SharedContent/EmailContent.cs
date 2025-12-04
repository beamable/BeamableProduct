// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.Content.Validation;
using UnityEngine;
#pragma warning disable CS0618

namespace Beamable.Common.Content
{
	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview/#contentlink-and-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	[Agnostic]
	public class EmailLink : ContentLink<EmailContent> { }

	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview/#contentlink-and-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	[Agnostic]
	public class EmailRef : EmailRef<EmailContent>
	{
		public EmailRef(string id) : base(id)
		{
			
		}

		public EmailRef()
		{
			
		}
	}	
	/// <summary>
	/// This type defines a methodology for resolving a reference to a %Beamable %ContentObject.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview/#contentlink-and-contentref">ContentLink vs ContentRef</a> documentation
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	public class EmailRef<TContent> : ContentRef<TContent> where TContent : EmailContent, new()
	{
		public EmailRef(string id) : base(id)
		{
			
		}

		public EmailRef()
		{
			
		}
	}
	/// <summary>
	/// This type defines a %Beamable %ContentObject subclass for the %MailService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// - See Beamable.Api.Mail.MailService script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[ContentType("emails")]
	[System.Serializable]
	[Agnostic]
	public class EmailContent : ContentObject
	{
		private const int MIN_LINES = 10;
		private const int MAX_LINES = 20;

		[Tooltip(ContentObject.TooltipSubject1)]
		[CannotBeBlank]
		public string subject;

		[Tooltip(ContentObject.TooltipBody1)]
		[TextAreaAttribute(MIN_LINES, MAX_LINES)]
		public string body;
	}
}
