using Beamable.Common;
using Beamable.Editor.UI;
using UnityEditor;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow : BeamEditorWindow<AccountWindow>
	{
		
		static AccountWindow()
		{
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Beam Account",
				FocusOnShow = false,
				DockPreferenceTypeName = null,
				RequireLoggedUser = true,
				RequirePid = true
			};
		}
		
		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Account",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		
		
		protected override void Build()
		{
			
		}
	}
}
