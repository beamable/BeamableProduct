using Beamable.Common.Api.Auth;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class AuthMethodButton : MonoBehaviour
	{
		public RectTransform RectTransform;
		public BussElement IconBussElement;
		public CanvasGroup Group;

		private static readonly Dictionary<AuthThirdParty, string> AuthMethodToBussClass =
			new Dictionary<AuthThirdParty, string>
			{
				{AuthThirdParty.Facebook, "facebook"},
				{AuthThirdParty.FacebookLimited, "facebook"},
				{AuthThirdParty.Apple, "apple"},
				{AuthThirdParty.GameCenter, "gameCenter"},
				{AuthThirdParty.GameCenterLimited, "gameCenter"},
				{AuthThirdParty.Google, "googlePlay"},
				{AuthThirdParty.GoogleGamesServices, "googlePlay"},
				{AuthThirdParty.Steam, "steam"}
			};

		private const string EMAIL_CLASS = "email";
		private const string INACTIVE_CLASS = "inactive";

		public void SetupEmail(bool isActive, bool interactable, float size = 0)
		{
			IconBussElement.SetClass(EMAIL_CLASS, true);
			IconBussElement.SetClass(INACTIVE_CLASS, !isActive);
			SetInteractable(interactable);
			
			if (size > 0)
			{
				RectTransform.sizeDelta = new Vector2(size, size);
			}
		}

		public void SetupThirdParty(AuthThirdParty thirdParty, bool isActive, bool interactable, float size = 0)
		{
			if (!AuthMethodToBussClass.TryGetValue(thirdParty, out string bussClass))
			{
				throw new ArgumentException($"Unhandled third party type: '{thirdParty}'");
			}
			
			IconBussElement.SetClass(bussClass, true);
			IconBussElement.SetClass(INACTIVE_CLASS, !isActive);
			SetInteractable(interactable);

			if (size > 0)
			{
				RectTransform.sizeDelta = new Vector2(size, size);
			}
		}

		private void SetInteractable(bool interactable)
		{
			Group.interactable = interactable;
			Group.blocksRaycasts = interactable;
		}
	}
}
