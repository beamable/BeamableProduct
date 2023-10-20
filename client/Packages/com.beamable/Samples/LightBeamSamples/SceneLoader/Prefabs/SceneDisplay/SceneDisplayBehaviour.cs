using Beamable.Runtime.LightBeams;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneDisplayBehaviour : MonoBehaviour
{
	[Header("Scene References")]
	public TMP_Text titleLabel;
	public TMP_Text aboutLabel;
	public Button playButton;

	public void Configure(LightBeamRuntimeScene scene)
	{
		titleLabel.SetText(scene.title);
		aboutLabel.SetText(scene.about);
		playButton.HandleClicked(() =>
		{
			SceneManager.LoadSceneAsync(scene.sceneName);
		});
	}
}
