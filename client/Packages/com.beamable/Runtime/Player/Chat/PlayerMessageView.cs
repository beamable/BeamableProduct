using TMPro;
using UnityEngine;

namespace Beamable.Player
{
	public class PlayerMessageView : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI playerName;

		[SerializeField]
		private TextMeshProUGUI message;

		public void SetData(string player, string message)
		{
			this.playerName.text = $"{player}: ";
			this.message.text = message;
		}
	}
}
