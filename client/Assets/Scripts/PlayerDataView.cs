using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameExample
{
	public class PlayerDataView : MonoBehaviour
	{
		[SerializeField]private TextMeshProUGUI text;
		public Button Button;
		

		public void UpdateData(PlayerData data)
		{
			text.text = $"{data.gamesStarted} games started\n" +
			            $"{data.gamePoints} game points\n" +
			            $"isThatThingEnabled: {data.isThatThingEnabled}" +
			            $"Updated at {DateTime.Now}";
		}
	}
}
