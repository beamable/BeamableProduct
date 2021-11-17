using Beamable.Editor.Realms;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Common.Models;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class RealmButtonVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<RealmButtonVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{
				name = "custom-text",
				defaultValue = "nada"
			};

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as RealmButtonVisualElement;
			}
		}

		private RealmModel Model
		{
			get;
			set;
		}

		private Button _realmButton;
		private Label _realmLabel;

		public RealmButtonVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(RealmButtonVisualElement)}/{nameof(RealmButtonVisualElement)}")
		{
		}

		public override void Refresh()
		{
			base.Refresh();
			Model = new RealmModel();
			Model.Initialize();
			_realmButton = Root.Q<Button>("realmButton");
			_realmButton.clickable.clicked += () =>
			{
				OnButtonClicked(_realmButton.worldBound);
			};

			_realmLabel = _realmButton.Q<Label>();
			if (Model.Current == null)
			{
				_realmLabel.text = "Select realm";
			}
			else
			{
				_realmLabel.text = Model.Current.DisplayName;

				RealmView currentRealmView = (RealmView)Model.Current;
				if (currentRealmView.IsProduction)
				{
					_realmButton.AddToClassList("production");
				}

				if (currentRealmView.IsStaging)
				{
					_realmButton.AddToClassList("staging");
				}
			}

			Model.OnElementChanged -= HandleRealmChanged;
			Model.OnElementChanged += HandleRealmChanged;
		}

		private void HandleRealmChanged(ISearchableElement view)
		{
			RealmView realm = (RealmView)view;

			_realmLabel.text = realm.DisplayName;
			if (realm.IsProduction)
			{
				_realmButton.AddToClassList("production");
			}
			else
			{
				_realmButton.RemoveFromClassList("production");
			}

			if (realm.IsStaging)
			{
				_realmButton.AddToClassList("staging");
			}
			else
			{
				_realmButton.RemoveFromClassList("staging");
			}
		}

		private void OnButtonClicked(Rect visualElementBounds)
		{
			var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

			var content = new SearchabledDropdownVisualElement("Switching Realm");
			content.Model = Model;
			var wnd = BeamablePopupWindow.ShowDropdown("Select Realm", popupWindowRect, new Vector2(200, 300), content);

			content.OnElementSelected += (realm) =>
			{
				EditorAPI.Instance.Then(beamable =>
				{
					beamable.SwitchRealm((RealmView)realm).Then(_ =>
					{
						wnd.Close();
					});
				});
			};
			content.Refresh();
		}
	}
}
