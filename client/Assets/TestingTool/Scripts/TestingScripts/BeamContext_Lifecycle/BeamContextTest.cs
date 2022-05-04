using Beamable;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class BeamContextTest : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI infoText;
	[SerializeField] private bool lookInParent;

	private void Start()
	{
		Assert.IsNotNull(infoText, $"Info text of {nameof(BeamContextTest)} is not assigned in object '{name}'");

		if (lookInParent)
		{
			infoText.text = "Initializing other BeamContext...";
			
			var context = BeamContext.InParent(this);
			context.OnReady.Then(_ =>
			{
				infoText.text = $"Player ID: {context.PlayerId}";
			});
		}
		else
		{
			infoText.text = "Initializing default BeamContext...";
			
			BeamContext.Default.OnReady.Then(_ =>
			{
				infoText.text = $"Player ID: {BeamContext.Default.PlayerId}";
			});
		}
	}
}
