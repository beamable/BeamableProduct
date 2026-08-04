using Beamable.Common.Api.Auth;
using UnityEngine;

namespace Beamable.AccountManagement
{
	public class ThirdPartyLoginArgument : MonoBehaviour
	{
		public AuthThirdParty ThirdParty;

		[Tooltip("Attempt the login without showing any UI, using a credential the player has already " +
				 "granted on this device. Only Google Sign-In on Android supports this; other providers " +
				 "report that no credential is available and do nothing.")]
		public bool Silent;
	}
}
