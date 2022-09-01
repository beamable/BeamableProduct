using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Beamable.Editor.NewTestingTool.Models.Lists
{
	internal abstract class TestListModelBase<T, T1> where T : BeamableVisualElement, new()
	{
		public Action<T1> OnSelectionChanged;

		protected TestingEditorModel TestingEditorModel { get; }
		protected const int ListViewItemHeight = 24;

		protected TestListModelBase(TestingEditorModel testingEditorModel)
		{
			TestingEditorModel = testingEditorModel;
		}

		public ExtendedListView CreateListView(IList itemSource)
		{
			var view = new ExtendedListView
			{
				makeItem = CreateListViewElement,
				bindItem = BindListViewElement,
				selectionType = SelectionType.Single,
				itemsSource = itemSource
			};

			view.SetItemHeight(ListViewItemHeight);
			view.BeamableOnItemChosen(ListView_OnItemChosen);
			view.BeamableOnSelectionsChanged(ListView_OnSelectionChanged);
			view.RefreshPolyfill();
			return view;
		}

		protected abstract T BindListViewElementUtil(VisualElement elem, int index);

		private void BindListViewElement(VisualElement elem, int index)
		{
			var visualElement = BindListViewElementUtil(elem, index);
			visualElement.Refresh();
		}

		private T CreateListViewElement() => new T();
		protected abstract void ListView_OnItemChosen(object obj);
		protected abstract void ListView_OnSelectionChanged(IEnumerable<object> objs);
	}
}
