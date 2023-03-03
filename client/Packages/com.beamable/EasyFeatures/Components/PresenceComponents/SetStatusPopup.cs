using Beamable.Common.Api.Presence;
using System;
using UnityEngine;

namespace Beamable.EasyFeatures.Components
{
	public class SetStatusPopup : SlidingOverlay
	{
		public SetStatusButton StatusButtonPrefab;
		public Transform ButtonsRoot;

		public void Setup(Action<PresenceStatus> onStatusChanged, OverlaysController overlaysController)
		{
			base.Setup(overlaysController);

			StatusButtonPrefab.gameObject.SetActive(false);
			var statuses = (PresenceStatus[])Enum.GetValues(typeof(PresenceStatus));

			foreach (var status in statuses)
			{
				PlayerPresence presence = new PlayerPresence {online = true, status = status.ToString()};
				var button = Instantiate(StatusButtonPrefab, ButtonsRoot);
				button.gameObject.SetActive(true);
				button.Setup(presence, () => onStatusChanged?.Invoke(status));
			}

			gameObject.SetActive(true);
		}
	}
}
