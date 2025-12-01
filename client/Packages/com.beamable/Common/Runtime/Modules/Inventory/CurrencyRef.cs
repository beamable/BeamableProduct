// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using UnityEngine;
#pragma warning disable CS0618

namespace Beamable.Common.Inventory
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
	public class CurrencyLink : ContentLink<CurrencyContent> { }

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
	public class CurrencyRef : CurrencyRef<CurrencyContent>
	{
		public CurrencyRef() { }

		public CurrencyRef(string id) : base(id)
		{
		}

		public static implicit operator string(CurrencyRef data) => data.GetId();
		public static implicit operator CurrencyRef(string data) => new CurrencyRef { Id = data };
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
	public class CurrencyRef<TContent> : ContentRef<TContent> where TContent : CurrencyContent, new()
	{
		public CurrencyRef(string id) : base(id){}
		public CurrencyRef(){}
	}

	[System.Serializable]
	[Agnostic]
	public class CurrencyAmount
	{
		[Tooltip(ContentObject.TooltipAmount1)]
		public int amount;

		[Tooltip(ContentObject.TooltipCurrency1)]
		[MustReferenceContent]
		public CurrencyRef symbol;
	}
}
