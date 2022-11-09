using Beamable.Avatars;
using Beamable.Common;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class FriendInfoPopup : MonoBehaviour
	{
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI GamertagText;
		public Image AvatarImage;
		public TextMeshProUGUI DescriptionText;
		public Button DeleteButton;
		public Button BlockButton;
		public Button MessageButton;
		public Button CloseButton;

		protected Action<long> OnDeleteButton;
		protected Action<long> OnBlockButton;
		protected Action<long> OnMessageButton;

		public async Promise Setup(long playerId, Action<long> onDeleteButton, Action<long> onBlockButton, Action<long> onMessageButton)
		{
			var Context = BeamContext.Default;
			
			var stats = await Context.Api.StatsService.GetStats("client", "public", "player", playerId);
			if (!stats.TryGetValue("alias", out string playerName))
			{
				playerName = playerId.ToString();
			}

			Sprite avatar = null;
			if (stats.TryGetValue("avatar", out string avatarName))
			{
				var accountAvatar = AvatarConfiguration.Instance.Avatars.Find(av => av.Name == avatarName);
				if (accountAvatar != null)
				{
					avatar = accountAvatar.Sprite;
				}
			}

			UsernameText.text = playerName;
			GamertagText.text = $"#{playerId}";
			AvatarImage.sprite = avatar;
			if (avatar == null)
			{
				AvatarImage.color = Color.clear;
			}

			OnDeleteButton = onDeleteButton;
			OnBlockButton = onBlockButton;
			OnMessageButton = onMessageButton;
			
			DeleteButton.onClick.ReplaceOrAddListener(() => DeleteButtonPressed(playerId));
			BlockButton.onClick.ReplaceOrAddListener(() => BlockButtonPressed(playerId));
			MessageButton.onClick.ReplaceOrAddListener(() => MessageButtonPressed(playerId));
			CloseButton.onClick.ReplaceOrAddListener(ClosePopup);
			
			gameObject.SetActive(true);
		}

		public void ClosePopup() => gameObject.SetActive(false);

		private void MessageButtonPressed(long playerId)
		{
			OnMessageButton?.Invoke(playerId);
		}

		private void BlockButtonPressed(long playerId)
		{
			OnBlockButton?.Invoke(playerId);
			ClosePopup();
		}

		private void DeleteButtonPressed(long playerId)
		{
			OnDeleteButton?.Invoke(playerId);
			ClosePopup();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				gameObject.SetActive(false);
			}
		}
	}
}
