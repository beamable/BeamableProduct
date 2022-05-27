using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicParty
{
	public class PartyMessagePopup : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _messageText;

		public void Setup(string message, float duration = 3f)
		{
			_messageText.text = message;
			StartCoroutine(CloseAfterDelay(duration));
		}

		private IEnumerator CloseAfterDelay(float delay)
		{
			yield return new WaitForSeconds(delay);
			Destroy(gameObject);
		}
	}
}
