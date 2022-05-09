using Beamable.CurrencyHUD;
using UnityEditor;

namespace Beamable.Editor.UI.Common.Inspectors
{
	[CustomEditor(typeof(CurrencyHUDFlow))]
	public class CurrencyHUDInspector : FeatureInspector
	{
		protected override string DocsURL => "https://docs.beamable.com/docs/virtual-currency-feature-overview";
		protected override string Title => "Currency HUD";
		protected override string Description => "This feature is flexible to meet the currency needs of each game's design. " +
		                                         "Currencies are used to buy items (e.g. Gold). " +
		                                         "Currencies are also used to symbolize the player's progress through the game; " +
		                                         "e.g. experience points (XP).";
	}
}
