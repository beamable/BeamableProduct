
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Auth
{


	public class AuthApi : IAuthApi
	{
		protected const string TOKEN_URL = "/basic/auth/token";
		public const string ACCOUNT_URL = "/basic/accounts";

		private IBeamableRequester _requester;
		private readonly IAuthSettings _settings;

		public AuthApi(IBeamableRequester requester, IAuthSettings settings = null)
		{
			_requester = requester;
			_settings = settings ?? new DefaultAuthSettings();
		}

		public Promise<User> GetUser()
		{
			return _requester.Request<User>(Method.GET, $"{ACCOUNT_URL}/me", useCache: true);
		}

		public virtual Promise<User> GetUser(TokenResponse token)
		{
			var tokenizedRequester = _requester.WithAccessToken(token);
			return tokenizedRequester.Request<User>(Method.GET, $"{ACCOUNT_URL}/me", useCache: true);
		}

		public Promise<bool> IsEmailAvailable(string email)
		{
			var encodedEmail = _requester.EscapeURL(email);
			return _requester.Request<AvailabilityResponse>(Method.GET, $"{ACCOUNT_URL}/available?email={encodedEmail}", null, false)
			   .Map(resp => resp.available);
		}

		public Promise<bool> IsThirdPartyAvailable(AuthThirdParty thirdParty, string token)
		{
			return _requester.Request<AvailabilityResponse>(Method.GET, $"{ACCOUNT_URL}/available/third-party?thirdParty={thirdParty.GetString()}&token={token}", null, false)
			   .Map(resp => resp.available);
		}

		public Promise<TokenResponse> CreateUser()
		{
			var req = new CreateUserRequest
			{
				grant_type = "guest"
			};
			return _requester.Request<TokenResponse>(Method.POST, TOKEN_URL, req, false);
			//return _requester.RequestForm<TokenResponse>(TOKEN_URL, form, false);
		}

		[Serializable]
		private class CreateUserRequest
		{
			public string grant_type;
		}

		public Promise<TokenResponse> LoginRefreshToken(string refreshToken)
		{
			var req = new LoginRefreshTokenRequest
			{
				grant_type = "refresh_token",
				refresh_token = refreshToken
			};
			return _requester.Request<TokenResponse>(Method.POST, TOKEN_URL, req, includeAuthHeader: false);
		}

		[Serializable]
		private class LoginRefreshTokenRequest
		{
			public string grant_type;
			public string refresh_token;
		}


		public Promise<TokenResponse> Login(
		   string username,
		   string password,
		   bool mergeGamerTagToAccount = true,
		   bool customerScoped = false
		)
		{
			var body = new LoginRequest
			{
				username = username,
				grant_type = "password",
				password = password,
				customerScoped = customerScoped
			};

			return _requester.Request<TokenResponse>(Method.POST, TOKEN_URL, body, includeAuthHeader: mergeGamerTagToAccount);
		}

		[Serializable]
		private class LoginRequest
		{
			public string grant_type;
			public string username;
			public string password;
			public bool customerScoped;
		}

		public Promise<TokenResponse> LoginThirdParty(AuthThirdParty thirdParty, string thirdPartyToken, bool includeAuthHeader = true)
		{
			var req = new LoginThirdPartyRequest
			{
				grant_type = "third_party",
				third_party = thirdParty.GetString(),
				token = thirdPartyToken
			};
			return _requester.Request<TokenResponse>(Method.POST, TOKEN_URL, req, includeAuthHeader);
		}

		[Serializable]
		private class LoginThirdPartyRequest
		{
			public string grant_type;
			public string third_party;
			public string token;
		}



		public Promise<User> RegisterDBCredentials(string email, string password)
		{
			var req = new RegisterDBCredentialsRequest
			{
				email = email,
				password = password
			};
			return _requester.Request<User>(Method.POST, $"{ACCOUNT_URL}/register", req);
		}

		[Serializable]
		private class RegisterDBCredentialsRequest
		{
			public string email, password;
		}

		public Promise<User> RemoveThirdPartyAssociation(AuthThirdParty thirdParty, string token)
		{
			return _requester.Request<User>(Method.DELETE, $"{ACCOUNT_URL}/me/third-party?thirdParty={thirdParty.GetString()}&token={token}", null, true);
		}

		public Promise<User> RegisterThirdPartyCredentials(AuthThirdParty thirdParty, string accessToken)
		{
			var req = new RegisterThirdPartyCredentialsRequest
			{
				thirdParty = thirdParty.GetString(),
				token = accessToken
			};
			return _requester.Request<User>(Method.PUT, $"{ACCOUNT_URL}/me", req);
		}

		[Serializable]
		private class RegisterThirdPartyCredentialsRequest
		{
			public string thirdParty;
			public string token;
		}




		public Promise<EmptyResponse> IssueEmailUpdate(string newEmail)
		{
			var req = new IssueEmailUpdateRequest
			{
				newEmail = newEmail
			};
			return _requester.Request<EmptyResponse>(Method.POST, $"{ACCOUNT_URL}/email-update/init", req);
		}

		[Serializable]
		private class IssueEmailUpdateRequest
		{
			public string newEmail;
		}

		public Promise<EmptyResponse> ConfirmEmailUpdate(string code, string password)
		{
			var req = new ConfirmEmailUpdateRequest
			{
				code = code,
				password = password
			};
			return _requester.Request<EmptyResponse>(Method.POST, $"{ACCOUNT_URL}/email-update/confirm", req);
		}

		[Serializable]
		private class ConfirmEmailUpdateRequest
		{
			public string code, password;
		}

		public Promise<EmptyResponse> IssuePasswordUpdate(string email)
		{
			var req = new IssuePasswordUpdateRequest
			{
				email = email,
				codeType = _settings.PasswordResetCodeType.Serialize()
			};
			return _requester.Request<EmptyResponse>(Method.POST, $"{ACCOUNT_URL}/password-update/init", req);
		}

		[Serializable]
		private class IssuePasswordUpdateRequest
		{
			public string email;
			public string codeType;
		}

		public Promise<EmptyResponse> ConfirmPasswordUpdate(string code, string newPassword)
		{
			var req = new ConfirmPasswordUpdateRequest
			{
				code = code,
				newPassword = newPassword
			};
			return _requester.Request<EmptyResponse>(Method.POST, $"{ACCOUNT_URL}/password-update/confirm", req);
		}

		[Serializable]
		private class ConfirmPasswordUpdateRequest
		{
			public string code, newPassword;
		}

		public Promise<CustomerRegistrationResponse> RegisterCustomer(string email, string password, string projectName, string customerName, string alias)
		{
			var request = new CustomerRegistrationRequest(email, password, projectName, customerName, alias);
			return _requester.Request<CustomerRegistrationResponse>(Method.POST, "/basic/realms/customer", request, false);
		}

		public Promise<CurrentProjectResponse> GetCurrentProject()
		{
			return _requester.Request<CurrentProjectResponse>(Method.GET, "/basic/realms/project", null, useCache: true);
		}

		public IBeamableRequester Requester => _requester;
	}

	/// <summary>
	/// This type defines the %UserBundle which combines %User and %TokenResponse.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
	/// - See Beamable.Api.Auth.AuthService script reference
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class UserBundle
	{
		/// <summary>
		/// The <see cref="User"/>
		/// </summary>
		public User User;

		/// <summary>
		/// The stored <see cref="TokenResponse"/> that the <see cref="User"/> last used to sign in.
		/// </summary>
		public TokenResponse Token;

		public override bool Equals(object obj)
		{
			return Equals(obj as UserBundle);
		}

		public override int GetHashCode()
		{
			return User.id.GetHashCode();
		}

		public bool Equals(UserBundle other)
		{
			if (other == null) return false;

			return other.User.id == User.id;
		}
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %User feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
	/// - See Beamable.Api.Auth.AuthService script reference
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[Serializable]
	public class User
	{
		/// <summary>
		/// The unique id of the player, sometimes called a "dbid".
		/// </summary>
		public long id;

		/// <summary>
		/// If the player has associated an email with their account, the email will appear here. Null otherwise.
		/// The email can be associated with the <see cref="IAuthApi.RegisterDBCredentials"/> method
		/// </summary>
		public string email;

		/// <summary>
		/// If the player has chosen a language for their account, the language code will appear here. EN by default.
		/// </summary>
		public string language;

		/// <summary>
		/// Scopes are permissions that the player has over the Beamable ecosystem.
		/// Most players will have no scopes.
		/// Players with the role of "tester" will have some "read" based scopes,
		/// Players with the role of "developer" will have most all scopes except those relating to team management, and
		/// Players with the role of "admin" will have single scope with the value of "*", which indicates ALL scopes.
		/// </summary>
		public List<string> scopes;

		/// <summary>
		/// If the player has associated any third party accounts with their account, those will appear here.
		/// The values of the strings will be taken from the <see cref="AuthThirdPartyMethods.GetString"/> method.
		/// Third parties can be associated with the <see cref="IAuthApi.RegisterThirdPartyCredentials"/> method.
		/// </summary>
		public List<string> thirdPartyAppAssociations;

		/// <summary>
		/// If the player has associated any device Ids with their account, those will appear here.
		/// </summary>
		public List<string> deviceIds;

		/// <summary>
		/// Check if the player has registered an email address with their account.
		/// </summary>
		/// <returns>true if the email address has been provided, false otherwise.</returns>
		public bool HasDBCredentials()
		{
			return !string.IsNullOrEmpty(email);
		}

		/// <summary>
		/// Check if a specific <see cref="AuthThirdParty"/> exists in the player's <see cref="thirdPartyAppAssociations"/> list.
		/// </summary>
		/// <param name="thirdParty">The <see cref="AuthThirdParty"/> to check the player for</param>
		/// <returns>true if the third party has been associated with the player account, false otherwise.</returns>
		public bool HasThirdPartyAssociation(AuthThirdParty thirdParty)
		{
			return thirdPartyAppAssociations != null && thirdPartyAppAssociations.Contains(thirdParty.GetString());
		}

		/// <summary>
		/// Check if any credentials have been associated with this account, whether email, device ids or third party apps.
		/// </summary>
		/// <returns>true if any credentials exist, false otherwise</returns>
		public bool HasAnyCredentials()
		{
			return HasDBCredentials() || (thirdPartyAppAssociations != null && thirdPartyAppAssociations.Count > 0)
				|| (deviceIds != null && deviceIds.Count > 0);
		}

		/// <summary>
		/// Check if a specific scope exists for the player's permissions.
		/// If the user is an Admin, and has the * scope, then every scope check will return true.
		/// This method reads the scope data from the <see cref="scopes"/> list
		/// </summary>
		/// <param name="scope">The scope you want to check</param>
		/// <returns>true if the scope exists or if the user is an admin, false otherwise</returns>
		public bool HasScope(string scope)
		{
			return scopes.Contains(scope) || scopes.Contains("*");
		}
	}

	/// <summary>
	/// This type defines the functionality for the %TokenResponse for the %AuthService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature documentation
	/// - See Beamable.Api.Auth.AuthService script reference
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[Serializable]
	public class TokenResponse
	{
		/// <summary>
		/// The token that will become the <see cref="IAccessToken.Token"/> value.
		/// </summary>
		public string access_token;

		/// <summary>
		/// There are two different types of tokens that Beamable may issue. The possible values are "access", or "refresh"
		/// </summary>
		public string token_type;

		/// <summary>
		/// The number of milliseconds from when the <see cref="TokenResponse"/> was sent by the Beamable servers, to when the
		/// token will be expired. This value informs the <see cref="IAccessToken.ExpiresAt"/> property
		/// </summary>
		public long expires_in;

		/// <summary>
		/// The token that will become the <see cref="IAccessToken.RefreshToken"/> value
		/// </summary>
		public string refresh_token;
	}


	[Serializable]
	public class AvailabilityRequest
	{
		public string email;
	}

	[Serializable]
	public class AvailabilityResponse
	{
		public bool available;
	}

	/// <summary>
	/// The available set of third party apps that Beamable can associate with player accounts.
	/// Note that the serialized state of these values should use the <see cref="AuthThirdPartyMethods.GetString"/> method.
	/// </summary>
	public enum AuthThirdParty
	{
		Facebook,
		FacebookLimited,
		Apple,
		Google,
		GameCenter,
		GameCenterLimited,
		Steam,
		GoogleGamesServices
	}

	public static class AuthThirdPartyMethods
	{
		/// <summary>
		/// Convert the given <see cref="AuthThirdParty"/> into a string format that can be sent to Beamable servers.
		/// Also, the Beamable servers treat these strings as special code names for the various third party apps.
		/// If you need to refer to a third party in Beamable's APIs, you should use this function to get the correct string value.
		/// </summary>
		/// <param name="thirdParty">The <see cref="AuthThirdParty"/> to convert to a string</param>
		/// <returns>The string format of the enum</returns>
		public static string GetString(this AuthThirdParty thirdParty)
		{
			switch (thirdParty)
			{
				case AuthThirdParty.Facebook:
					return "facebook";
				case AuthThirdParty.FacebookLimited:
					return "facebooklimited";
				case AuthThirdParty.Apple:
					return "apple";
				case AuthThirdParty.Google:
					return "google";
				case AuthThirdParty.GameCenter:
					return "gamecenter";
				case AuthThirdParty.GameCenterLimited:
					return "gamecenterlimited";
				case AuthThirdParty.Steam:
					return "steam";
				case AuthThirdParty.GoogleGamesServices:
					return "googlegamesservices";
				default:
					return null;
			}
		}
	}

	[System.Serializable]
	public class CustomerRegistrationRequest
	{
		public string email;
		public string password;
		public string projectName;
		public string customerName;
		public string alias;

		public CustomerRegistrationRequest(string email, string password, string projectName, string customerName, string alias)
		{
			this.email = email;
			this.password = password;
			this.projectName = projectName;
			this.customerName = customerName;
			this.alias = alias;
		}
	}

	[System.Serializable]
	public class CustomerRegistrationResponse
	{
		public long cid;
		public string pid;
		public TokenResponse token;
	}

	public class CurrentProjectResponse
	{
		public string cid, pid, projectName;
	}

	public interface IAuthSettings
	{
		CodeType PasswordResetCodeType { get; }
	}

	public class DefaultAuthSettings : IAuthSettings
	{
		public CodeType PasswordResetCodeType { get; set; } = CodeType.PIN;
	}

	public enum CodeType
	{
		UUID, PIN
	}

	public static class CodeTypeExtensions
	{
		public static string Serialize(this CodeType type)
		{
			switch (type)
			{
				case CodeType.PIN: return "PIN";
				default: return "UUID";
			}
		}
	}
}
