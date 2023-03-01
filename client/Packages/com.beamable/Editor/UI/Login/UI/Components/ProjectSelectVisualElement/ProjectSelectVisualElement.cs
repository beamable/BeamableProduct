using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
namespace Beamable.Editor.Login.UI.Components
{
	public class ProjectSelectVisualElement : LoginBaseComponent
	{

		public ProjectSelectVisualElement() : base(nameof(ProjectSelectVisualElement))
		{
		}
		private VisualElement _projectListView;
		private RealmView _selected;
		private FormConstraint _gameConstraint;

		public override string GetMessage()
		{
			return "Please select your game.";
		}

		public override void Refresh()
		{
			base.Refresh();
			var primaryButton = Root.Q<PrimaryButtonVisualElement>();
			primaryButton.Button.clickable.clicked += PrimaryButton_OnClicked;
			_gameConstraint = FormConstraint.Logical("Select a game", () => _selected == null);
			primaryButton.AddGateKeeper(_gameConstraint);
			_gameConstraint.Check();
			_projectListView = Root.Q<VisualElement>("realmList");

			SetGamesList(Model.Games);
			if (Model.Games.Count == 1)
			{
				PickGame(Model.Games[0]);
			}
		}

		private void PrimaryButton_OnClicked()
		{
			PickGame(_selected);
		}

		void PickGame(RealmView realm)
		{
			Model.Customer.SetPid(realm.Pid);
			Model.SetGame(realm.FindRoot());
			Manager.AttemptProjectSelect(Model, realm);
		}

		void SelectGame(RealmView realm, RealmVisualElement element)
		{
			_selected = realm;
			_gameConstraint.Check();
		}

		public void SetGamesList(List<RealmView> games)
		{
			_projectListView.Clear();
			foreach (var game in games)
			{
				RealmVisualElement realmVisualElement = new RealmVisualElement { Realm = game };
				realmVisualElement.OnSelected += selectedGame =>
				{
					_projectListView.Query<RealmVisualElement>().Build().ToList().ForEach(element =>
				{
					element.RemoveFromClassList("highLight");
				});
					realmVisualElement.AddToClassList("highLight");
					SelectGame(selectedGame, realmVisualElement);

				};
				realmVisualElement.Refresh();
				_projectListView.Add(realmVisualElement);
			}
		}
	}
}
