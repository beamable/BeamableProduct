
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Auth
{


   public class AuthApi : IAuthApi
   {
      protected const string TOKEN_URL = "/basic/auth/token";
      protected const string ACCOUNT_URL = "/basic/accounts";

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
      public User User;
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
      public long id;
      public string email;
      public string language;
      public List<string> scopes;
      public List<string> thirdPartyAppAssociations;
      public bool HasDBCredentials()
      {
         return !string.IsNullOrEmpty(email);
      }

      public bool HasThirdPartyAssociation(AuthThirdParty thirdParty)
      {
         return thirdPartyAppAssociations != null && thirdPartyAppAssociations.Contains(thirdParty.GetString());
      }

      public bool HasAnyCredentials()
      {
         return HasDBCredentials() || (thirdPartyAppAssociations != null && thirdPartyAppAssociations.Count > 0);
      }

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
      public string access_token;
      public string token_type;
      public long expires_in;
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

   public enum AuthThirdParty
   {
      Facebook,
      FacebookLimited,
      Apple,
      Google,
      GameCenter,
      GameCenterLimited,
      Steam
   }

   public static class AuthThirdPartyMethods
   {
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
      public string cid, pid;
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
