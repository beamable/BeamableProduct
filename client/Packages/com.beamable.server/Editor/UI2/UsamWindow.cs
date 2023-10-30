using Beamable;
using Beamable.Common;
using Beamable.Editor.UI;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using UnityEditor;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2
{
	public class UsamWindow : BeamEditorWindow<UsamWindow>
	{
		private CodeService _codeService;

		static UsamWindow()
		{
			var inspector = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			WindowDefaultConfig = new BeamEditorWindowInitConfig()
			{
				Title = "Usam Editor",
				FocusOnShow = false,
				DockPreferenceTypeName = inspector.AssemblyQualifiedName,
				RequireLoggedUser = true,
			};
		}

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Usam Editor %g",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static async void Init() => _ = await GetFullyInitializedWindow();

		public override bool ShowLoading => true;

		protected override void Build()
		{
			// ActiveContext.ServiceScope.
			var root = this.GetRootVisualContainer();
			root.Clear();
			root.Add(new Label("Ready"));
		}

		public override async Promise OnLoad()
		{
			_codeService = Scope.GetService<CodeService>();
			
			
			await _codeService.OnReady;
		}
	}
}
