using Beamable.Common;
using Beamable.Runtime.LightBeams;
using Beamable.Server.Clients;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootBoxPage : MonoBehaviour, ILightComponent
{
	public Button claimButton;
	public TextMeshProUGUI timeText;

	private double _startTimeLeft;
	private double _startTime;

	public async Promise OnInstantiated(LightBeam beam)
	{
		var client = new LootBoxServiceClient();
		_startTimeLeft = await client.GetTimeLeft();
		_startTime = Time.realtimeSinceStartupAsDouble;

		claimButton.onClick.AddListener(Claim);
	}

	private async void Claim()
	{
		var client = new LootBoxServiceClient();
		_startTimeLeft = 120;
		await client.Claim();
		_startTimeLeft = await client.GetTimeLeft();
		_startTime = Time.realtimeSinceStartupAsDouble;
	}

	void Update()
	{
		var now = Time.realtimeSinceStartupAsDouble;
		var delta = now - _startTime;

		var timeLeft = _startTimeLeft - (delta);
		timeLeft = timeLeft < 0 ? 0 : timeLeft;
		var isClaimable = timeLeft <= .001;

		timeText.text = timeLeft.ToString("00");

		claimButton.interactable = isClaimable;
	}
}
