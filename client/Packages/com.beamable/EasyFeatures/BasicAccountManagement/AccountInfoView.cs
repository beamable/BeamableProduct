using Beamable.Avatars;
using Beamable.Common;
using Beamable.EasyFeatures.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountInfoView : MonoBehaviour, IAsyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
			string Email { get; }
			Promise<string> GetCurrentAvatarName(long playerId);
			Promise SetAvatar(string avatarName);
			Promise<string> GetUsername(long playerId);
			Promise SetUsername(string username);
		}

		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public Button ConfirmButton;
		public Button CancelButton;
		public ToggleGroup AvatarsGroup;
		public AvatarToggle AvatarTogglePrefab;
		public TMP_InputField UsernameInputField;
		public TMP_InputField EmailInputField;

		protected IDependencies System;

		private AccountAvatar _avatarToSet;
		private string _currentUsername;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public async Promise EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;
			
			if (!IsVisible)
			{
				return;
			}

			_currentUsername = await System.GetUsername(System.Context.PlayerId);
			UsernameInputField.text = _currentUsername;
			EmailInputField.text = System.Email;
			
			_avatarToSet = null;
			string currentAvatarName = await System.GetCurrentAvatarName(System.Context.PlayerId);
			
			foreach (Transform child in AvatarsGroup.transform)
			{
				Destroy(child.gameObject);
			}
			
			foreach (var avatar in AvatarConfiguration.Instance.Avatars)
			{
				// instantiate toggle for each avatar
				var avatarToggle = Instantiate(AvatarTogglePrefab, AvatarsGroup.transform);
				avatarToggle.Setup(avatar, AvatarsGroup, avatar.Name == currentAvatarName, OnAvatarSelectionChanged);
			}
			
			FeatureControl.SetBackAction(OpenAccountsView);
			FeatureControl.SetHomeAction(OpenAccountsView);
			ConfirmButton.onClick.ReplaceOrAddListener(OnConfirmPressed);
			CancelButton.onClick.ReplaceOrAddListener(OpenAccountsView);
		}

		private void OnAvatarSelectionChanged(AccountAvatar avatar)
		{
			_avatarToSet = avatar;
		}

		private async void OnConfirmPressed()
		{
			if (_avatarToSet != null)
			{
				await System.SetAvatar(_avatarToSet.Name);
			}

			if (!string.IsNullOrWhiteSpace(UsernameInputField.text) && UsernameInputField.text != _currentUsername)
			{
				await System.SetUsername(UsernameInputField.text);
			}
			
			OpenAccountsView();
		}

		private void OpenAccountsView() => FeatureControl.OpenAccountsView();
	}
}
