using System;
using UnityEngine.UI;

[Serializable]
public class SelectorButtonBehaviour : Button
{
	public Image selectedImage;

	public void SetState(bool state)
	{
		selectedImage.gameObject.SetActive(state);
	}
}
