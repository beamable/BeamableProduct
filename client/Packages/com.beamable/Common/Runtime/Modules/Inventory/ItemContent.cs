using Beamable.Common.Content;
using Beamable.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Beamable.Common.Inventory
{
	/// <summary>
	/// This type defines a %Beamable %ContentObject subclass for %Item related to the %InventoryService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	/// - See Beamable.Api.Inventory.InventoryService script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	[ContentType("items")]
	[System.Serializable]
	[Agnostic]
	public class ItemContent : ContentObject
	{
		[Tooltip(ContentObject.TooltipIcon1)]
		[FormerlySerializedAs("Icon")]
		[ContentField("icon", FormerlySerializedAs = new[] {"Icon"})]
		public AssetReferenceSprite icon;

		[Tooltip(ContentObject.TooltipClientPermission1)]
		public ClientPermissions clientPermission;
	}
}
