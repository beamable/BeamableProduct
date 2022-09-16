using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class FriendSlotPresenter : MonoBehaviour
	{
		public Image AvatarImage;
		public TextMeshProUGUI UsernameText;
		public TextMeshProUGUI DescriptionText;
		public Button ConfirmButton;
		public TextMeshProUGUI ConfirmButtonText;
		
		public struct ViewData
		{
			public string PlayerName;
			public string Description;
			public Sprite Avatar;
		}
		
		public class PoolData : PoolableScrollView.IItem
		{
			public ViewData ViewData { get; set; }
			public int Index { get; set; }
			public float Height { get; set; }
		}

		public void Setup(PoolData item, Action onConfirmPressed, string buttonText = "Confirm")
		{
			AvatarImage.sprite = item.ViewData.Avatar;
			UsernameText.text = item.ViewData.PlayerName;
			DescriptionText.text = item.ViewData.Description;
			
			ConfirmButton.onClick.ReplaceOrAddListener(() => onConfirmPressed?.Invoke());
			ConfirmButton.gameObject.SetActive(onConfirmPressed != null);
			ConfirmButtonText.text = buttonText;
		}
	}
}
