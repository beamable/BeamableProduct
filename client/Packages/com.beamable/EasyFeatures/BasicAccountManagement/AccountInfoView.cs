using Beamable.Avatars;
using Beamable.EasyFeatures.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class AccountInfoView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			BeamContext Context { get; set; }
		}

		public AccountManagementFeatureControl FeatureControl;
		public int EnrichOrder;

		public Button ConfirmButton;
		public Button CancelButton;
		public ToggleGroup AvatarsGroup;
		public AvatarToggle AvatarTogglePrefab;

		protected IDependencies System;

		public bool IsVisible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var context = managedPlayers.GetSinglePlayerContext();
			System = context.ServiceProvider.GetService<IDependencies>();
			System.Context = context;
			
			if (!IsVisible)
			{
				return;
			}

			foreach (Transform child in AvatarsGroup.transform)
			{
				Destroy(child.gameObject);
			}

			foreach (var avatar in AvatarConfiguration.Instance.Avatars)
			{
				// instantiate toggle for each avatar
				var avatarToggle = Instantiate(AvatarTogglePrefab, AvatarsGroup.transform);
				avatarToggle.Setup(avatar, AvatarsGroup, OnAvatarSelectionChanged);
			}
			
			FeatureControl.SetBackAction(OpenAccountsView);
			FeatureControl.SetHomeAction(OpenAccountsView);
			ConfirmButton.onClick.ReplaceOrAddListener(OnConfirmPressed);
			CancelButton.onClick.ReplaceOrAddListener(OpenAccountsView);
		}

		private void OnAvatarSelectionChanged(bool isSelected, AccountAvatar avatar)
		{
			if (!isSelected)
				return;
			
			// set avatar
			Debug.LogWarning($"CHOSEN AVATAR: {avatar.Name}");
		}

		private void OnConfirmPressed()
		{
			// set new account data
			
			OpenAccountsView();
		}

		private void OpenAccountsView() => FeatureControl.OpenAccountsView();
	}
}
