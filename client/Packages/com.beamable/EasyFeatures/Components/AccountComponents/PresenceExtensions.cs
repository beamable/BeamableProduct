using Beamable.Common.Api.Presence;
using Beamable.UI.Buss;
using System;

namespace Beamable.EasyFeatures.Components
{
	public static class PresenceExtensions
	{
		private const string ONLINE_CLASS = "online";
		private const string AWAY_CLASS = "away";
		private const string DND_CLASS = "doNotDisturb";
		private const string INVISIBLE_CLASS = "invisible";
		private const string OFFLINE_CLASS = "offline";

		public static string GetName(this PlayerPresence presence, bool isCurrentPlayer)
		{
			if (!presence.online)
				return "Offline";

			switch (presence.Status)
			{
				case PresenceStatus.Visible:
					return "Online";
				case PresenceStatus.Away:
					return "Away";
				case PresenceStatus.Invisible:
					return isCurrentPlayer ? "Invisible" : "Offline";
				case PresenceStatus.Dnd:
					return "Do Not Disturb";
				default:
					throw new ArgumentException($"Unexpected presence status: '{presence.Status}'");
			}
		}

		public static void SetPresenceDotClasses(this PlayerPresence presence,
		                                         BussElement presenceDotElement,
		                                         bool isCurrentPlayer)
		{
			if (!presence.online)
			{
				SetPresenceClass(OFFLINE_CLASS, presenceDotElement);
				return;
			}

			switch (presence.Status)
			{
				case PresenceStatus.Visible:
					SetPresenceClass(ONLINE_CLASS, presenceDotElement);
					break;
				case PresenceStatus.Invisible:
					if (isCurrentPlayer)
					{
						SetPresenceClass(INVISIBLE_CLASS, presenceDotElement);
					}
					else
					{
						SetPresenceClass(OFFLINE_CLASS, presenceDotElement);
					}
					break;
				case PresenceStatus.Away:
					SetPresenceClass(AWAY_CLASS, presenceDotElement);
					break;
				case PresenceStatus.Dnd:
					SetPresenceClass(DND_CLASS, presenceDotElement);
					break;
				default:
					SetPresenceClass(string.Empty, presenceDotElement); // remove all presence classes
					break;
			}
		}

		private static void SetPresenceClass(string bussClass, BussElement element)
		{
			element.SetClass(ONLINE_CLASS, ONLINE_CLASS == bussClass);
			element.SetClass(OFFLINE_CLASS, OFFLINE_CLASS == bussClass);
			element.SetClass(INVISIBLE_CLASS, INVISIBLE_CLASS == bussClass);
			element.SetClass(AWAY_CLASS, AWAY_CLASS == bussClass);
			element.SetClass(DND_CLASS, DND_CLASS == bussClass);
		}
	}
}
