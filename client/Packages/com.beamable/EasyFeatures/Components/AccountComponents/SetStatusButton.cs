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
		private const string DND_CLASS = "doNotDisturb";
		private const string AWAY_CLASS = "away";
		private const string INVISIBLE_CLASS = "invisible";
		private const string ONLINE_CLASS = "online";
		
		public Button Button;
		public BussElement PresenceDot;
		public TextMeshProUGUI StatusText;

		public void Setup(PresenceStatus playerStatus, UnityAction onButtonPress)
		{
			SetStatus(playerStatus, out var status);
			StatusText.text = status;
			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(onButtonPress);
		}

		private void SetStatus(PresenceStatus status, out string statusName)
		{
			switch (status)
			{
				case PresenceStatus.Visible:
					PresenceDot.SetClass(ONLINE_CLASS, true);
					PresenceDot.SetClass(INVISIBLE_CLASS, false);
					PresenceDot.SetClass(AWAY_CLASS, false);
					PresenceDot.SetClass(DND_CLASS, false);
					statusName = "Online";
					break;
				case PresenceStatus.Invisible:
					PresenceDot.SetClass(ONLINE_CLASS, false);
					PresenceDot.SetClass(INVISIBLE_CLASS, true);
					PresenceDot.SetClass(AWAY_CLASS, false);
					PresenceDot.SetClass(DND_CLASS, false);
					statusName = "Invisible";
					break;
				case PresenceStatus.Away:
					PresenceDot.SetClass(ONLINE_CLASS, false);
					PresenceDot.SetClass(INVISIBLE_CLASS, false);
					PresenceDot.SetClass(AWAY_CLASS, true);
					PresenceDot.SetClass(DND_CLASS, false);
					statusName = "Away";
					break;
				case PresenceStatus.Dnd:
					PresenceDot.SetClass(ONLINE_CLASS, false);
					PresenceDot.SetClass(INVISIBLE_CLASS, false);
					PresenceDot.SetClass(AWAY_CLASS, false);
					PresenceDot.SetClass(DND_CLASS, true);
					statusName = "Do Not Disturb";
					break;
				default:
					PresenceDot.SetClass(ONLINE_CLASS, false);
					PresenceDot.SetClass(INVISIBLE_CLASS, false);
					PresenceDot.SetClass(AWAY_CLASS, false);
					PresenceDot.SetClass(DND_CLASS, false);
					statusName = "UNKNOWN STATUS";
					break;
			}
		}
	}
}
