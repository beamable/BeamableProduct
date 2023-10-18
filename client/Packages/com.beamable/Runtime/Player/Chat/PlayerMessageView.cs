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

		public void SetData(string player, string text)
		{
			playerName.text = $"{player}: ";
			message.text = text;
		}
	}
}
