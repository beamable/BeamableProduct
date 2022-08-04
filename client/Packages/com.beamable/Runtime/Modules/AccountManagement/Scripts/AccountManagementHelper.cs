using Beamable.Common;
using Beamable.Common.Api.Auth;

namespace Beamable.AccountManagement
{
	public static class AccountManagementHelper
	{
		public static Promise<bool> IsEmailRegistered(string email)
		{
			return API.Instance.FlatMap(de => de.AuthService.IsEmailAvailable(email)
			                                    .Map(available => !available));
		}

		public static Promise<User> AttachThirdPartyToCurrentUser(IBeamableAPI de,
		                                                          AuthThirdParty thirdParty,
		                                                          string accessToken)
		{
			return de.AuthService.RegisterThirdPartyCredentials(thirdParty, accessToken)
			         .Then(de.UpdateUserData);
		}

		public static Promise<User> RemoveThirdPartyFromCurrentUser(IBeamableAPI de,
		                                                            AuthThirdParty thirdParty,
		                                                            string accessToken)
		{
			return de.AuthService.RemoveThirdPartyAssociation(thirdParty, accessToken)
			         .Then(de.UpdateUserData);
		}

		public static Promise<User> AttachEmailToCurrentUser(IBeamableAPI de, string email, string password)
		{
			return de.AuthService.RegisterDBCredentials(email, password).Then(de.UpdateUserData);
		}

		public static Promise<Unit> LoginToNewUser(IBeamableAPI de)
		{
			return de.AuthService.CreateUser().FlatMap(de.ApplyToken);
		}
	}
}
