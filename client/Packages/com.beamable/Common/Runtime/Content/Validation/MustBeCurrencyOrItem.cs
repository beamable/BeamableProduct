// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

using Beamable.Common.Inventory;

namespace Beamable.Common.Content.Validation
{
	/// <summary>
	/// This type defines part of the %Beamable %ContentObject validation process.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.Validation.ValidationAttribute script reference
	/// 
	/// ![img beamable-logo]
	///
	/// </summary>
	public class MustBeCurrencyOrItem : MustReferenceContent
	{
		public MustBeCurrencyOrItem(bool allowNull = false) : base(allowNull, allowedTypes: new[] { typeof(CurrencyContent), typeof(ItemContent) })
		{

		}
	}
}
