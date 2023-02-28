using Beamable.Common.Api.Presence;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Beamable.UI.Buss;
using UnityEngine.Events;

namespace Beamable.EasyFeatures.Components
{
	public class SetStatusButton : MonoBehaviour
	{
		public Button Button;
		public BussElement PresenceDot;
		public TextMeshProUGUI StatusText;

		public void Setup(PlayerPresence playerPresence, UnityAction onButtonPress)
		{
			playerPresence.SetPresenceDotClasses(PresenceDot, true);
			StatusText.text = playerPresence.GetName(true);;
			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(onButtonPress);
		}
	}
}
