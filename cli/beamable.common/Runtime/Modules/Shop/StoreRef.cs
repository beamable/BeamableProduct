using Beamable.Common.Content;
#pragma warning disable CS0618

namespace Beamable.Common.Shop
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
	public class StoreLink : ContentLink<StoreContent> { }

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
	public class StoreRef : ContentRef<StoreContent>
	{
		public StoreRef()
		{
			
		}

		public StoreRef(string id) : base(id)
		{
			
		}
	}
}
