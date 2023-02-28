using Beamable.Avatars;
using Beamable.UI.Buss;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AvatarToggle : MonoBehaviour
	{
		private const string SELECTED_CLASS = "selected";
		
		public Image AvatarImage;
		public Toggle Toggle;
		public BussElement BackgroundBussElement;
		
		public void Setup(AccountAvatar avatar, ToggleGroup group, bool isSelected, UnityAction<AccountAvatar> onSelectionChanged)
		{
			AvatarImage.sprite = avatar.Sprite;
			Toggle.isOn = isSelected;
			Toggle.group = group;
			
			BackgroundBussElement.SetClass(SELECTED_CLASS, isSelected);
			
			Toggle.onValueChanged.AddListener(isOn =>
			{
				BackgroundBussElement.SetClass(SELECTED_CLASS, isOn);

				if (isOn)
				{
					onSelectionChanged?.Invoke(avatar);
				}
			});
		}
	}
}
