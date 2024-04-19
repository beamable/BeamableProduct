using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Modules.Generics;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI3.Components
{
	public class SamLogsVisualElement : BeamableVisualElement
	{
		private readonly string _beamoId;
		private ListView _view;

		public SamLogsVisualElement(string beamoId) : base(
			$"{Constants.Directories.BEAMABLE_SERVER_PACKAGE_EDITOR}/UI3/Components/{nameof(SamLogsVisualElement)}/{nameof(SamLogsVisualElement)}")
		{
			_beamoId = beamoId;
		}

		public override void Refresh()
		{
			base.Refresh();

			var model = Provider.GetService<SamLogModel>();
			var logs = model.GetLogsForService(_beamoId);

			_view = CreateListView(logs);
			Root.Add(_view);
			
		}
		
		private ListView CreateListView(SamServiceLogs logs)
		{
			var view = new ListView()
			{
				makeItem = CreateListViewElement,
				bindItem = BindListViewElement,
				selectionType = SelectionType.Single,
				itemsSource = logs.messages
			};
			view.SetItemHeight(24);
			// view.BeamableOnSelectionsChanged(ListView_OnSelectionChanged);
			view.RefreshPolyfill();
			return view;
		}
		
		Label CreateListViewElement()
		{
			Label contentVisualElement = new Label();
			return contentVisualElement;
		}

		void BindListViewElement(VisualElement elem, int index)
		{
			if (index < 0)
				return;

			var consoleLogVisualElement = (Label)elem;
			consoleLogVisualElement.text = (_view.itemsSource[index] as SamLogMessage).message;
			// consoleLogVisualElement.Refresh();
			// consoleLogVisualElement.SetNewModel(_listView.itemsSource[index] as LogMessage);
			consoleLogVisualElement.EnableInClassList("oddRow", index % 2 != 0);
			consoleLogVisualElement.RemoveFromClassList("unity-list-view__item");
			consoleLogVisualElement.RemoveFromClassList("unity-listview_item");
			consoleLogVisualElement.RemoveFromClassList("unity-collection-view__item");
			consoleLogVisualElement.MarkDirtyRepaint();
		}
	}
}
