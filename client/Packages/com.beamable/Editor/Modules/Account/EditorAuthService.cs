using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;

namespace Beamable.Editor.Modules.Account
{
	public interface IEditorAuthApi : IAuthService
	{
		Promise<EditorUser> GetUserForEditor();
	}

	public class EditorAuthService : AuthService, IEditorAuthApi
	{
		public EditorAuthService(IBeamableRequester requester) : base(requester, new DefaultDeviceIdResolver(), new DefaultAuthSettings
		{
			PasswordResetCodeType = CodeType.PIN
		})
		{

		}

		// This API call will only work if made by editor code.
		public Promise<EditorUser> GetUserForEditor()
		{
			return Requester.Request<EditorUser>(Method.GET, $"{ACCOUNT_URL}/admin/me", useCache: true);
		}
	}

	[System.Serializable]
	public class EditorUser : User
	{
		public const string ADMIN_ROLE = "admin";
		public const string DEVELOPER_ROLE = "developer";
		public const string TESTER_ROLE = "tester";

		public string roleString;

		public bool IsAtLeastAdmin => string.Equals(roleString, ADMIN_ROLE);
		public bool IsAtLeastDeveloper => IsAtLeastAdmin || string.Equals(roleString, DEVELOPER_ROLE);
		public bool IsAtLeastTester => IsAtLeastDeveloper || string.Equals(roleString, TESTER_ROLE);
		public bool HasNoRole => !IsAtLeastTester;

		public bool CanPushContent => IsAtLeastDeveloper;

		public EditorUser()
		{

		}

		public EditorUser(User user)
		{
			id = user.id;
			email = user.email;
			language = user.language;
			scopes = user.scopes;
			thirdPartyAppAssociations = user.thirdPartyAppAssociations;
			deviceIds = user.deviceIds;
		}

	}
}
