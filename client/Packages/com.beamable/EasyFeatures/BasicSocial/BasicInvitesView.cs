using Beamable.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class BasicInvitesView : MonoBehaviour, IAsyncBeamableView
	{
		public TextMeshProUGUI PlayerIdText;
		public TMP_InputField PlayerIdInputField;
		public Toggle PendingListToggle;
		public Toggle SentListToggle;
		public FriendsListPresenter PendingListPresenter;
		public FriendsListPresenter SentListPresenter;

		protected BeamContext Context;
		
		private FriendsListPresenter _currentListView;
		
		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}
		public int GetEnrichOrder()
		{
			return 0;
		}

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			Context = managedPlayers.GetSinglePlayerContext();

			await Context.Social.OnReady;

			PlayerIdText.text = $"#{Context.PlayerId}";
			PendingListPresenter.gameObject.SetActive(false);
			SentListPresenter.gameObject.SetActive(false);
			
			PendingListToggle.onValueChanged.AddListener(isOn => TabPicked(isOn, PendingListPresenter));
			SentListToggle.onValueChanged.AddListener(isOn => TabPicked(isOn, SentListPresenter));
			PlayerIdInputField.onEndEdit.AddListener(SendInvite);
			
			OpenTab(PendingListPresenter);
		}

		private async void SendInvite(string playerId)
		{
			if (!long.TryParse(playerId, out long id))
			{
				Debug.LogError($"Provided id '{playerId}' is invalid");
				return;
			}
			
			await Context.Social.Invite(id);
		}

		private void TabPicked(bool isOn, FriendsListPresenter list)
		{
			if (!isOn)
			{
				return;
			}
			
			OpenTab(list);
		}

		private async Promise OpenTab(FriendsListPresenter tab)
		{
			if (_currentListView != null)
			{
				_currentListView.gameObject.SetActive(false);
			}

			_currentListView = tab;
			_currentListView.gameObject.SetActive(true);

			
			
			await _currentListView.Setup(null);
		}
	}
}
