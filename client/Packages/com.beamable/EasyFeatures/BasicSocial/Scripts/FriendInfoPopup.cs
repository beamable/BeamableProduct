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

		public async Promise Setup(long playerId, Action<long> onDeleteButton, Action<long> onBlockButton, Action<long> onMessageButton)
		{
			var Context = BeamContext.Default;
			
			var stats = await Context.Api.Stats.GetStats("client", "public", "player", playerId);
			if (!stats.TryGetValue("alias", out string playerName))
			{
				playerName = playerId.ToString();
			}

			Sprite avatar = AvatarConfiguration.Instance.Default.Sprite;
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
			
			DeleteButton.onClick.ReplaceOrAddListener(() => onDeleteButton?.Invoke(playerId));
			BlockButton.onClick.ReplaceOrAddListener(() => onBlockButton?.Invoke(playerId));
			MessageButton.onClick.ReplaceOrAddListener(() => onMessageButton?.Invoke(playerId));
			CloseButton.onClick.ReplaceOrAddListener(() => gameObject.SetActive(false));
			
			gameObject.SetActive(true);
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
