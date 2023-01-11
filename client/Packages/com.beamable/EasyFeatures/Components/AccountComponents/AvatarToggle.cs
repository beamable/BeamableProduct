using Beamable.Avatars;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AvatarToggle : MonoBehaviour
	{
		public Image AvatarImage;
		public Toggle Toggle;
		
		public void Setup(AccountAvatar avatar, ToggleGroup group, UnityAction<bool, AccountAvatar> onSelectionChanged)
		{
			AvatarImage.sprite = avatar.Sprite;
			Toggle.onValueChanged.AddListener(isOn => onSelectionChanged?.Invoke(isOn, avatar));
			Toggle.group = group;
		}
	}
}
