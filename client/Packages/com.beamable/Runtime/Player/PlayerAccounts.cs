using Beamable.Api;
using Beamable.Api.Auth;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Player
{

	[Serializable]
	public class PlayerAccount
	{
		private readonly PlayerAccounts _collection;
		private User _user;

		/// <summary>
		/// The gamerTag for the given player.
		/// GamerTags are associated with a specific realm.
		/// </summary>
		public long gamerTag;

		public string[] deviceIds;

		/// <summary>
		/// The email address associated with this account. 
		/// </summary>
		public OptionalString email = new OptionalString();

		public bool HasEmail => email.HasValue;
		
		public BeamableToken token;
		
		// TODO: add scopes
		// TODO: add email
		// TODO: add deviceId
		// TODO: add all third party associations
		// TODO: switch to this account
		// TODO: erase account

		public PlayerAccount(PlayerAccounts collection, BeamableToken token, User user)
		{
			_collection = collection;
			Update(user);
			this.token = token;
		}

		public async Promise SwitchToAccount()
		{
			await _collection.SwitchToAccount(this);
		}

		public async Promise RemoveFromDevice()
		{
			await _collection.RemoveFromDevice(this);
		}

		public async Promise<RegistrationResult> AddEmail(string email, string password)
		{
			return await _collection.RegisterEmail(email, password, this);
		}

		public async Promise<RegistrationResult> AddDeviceId()
		{
			return await _collection.RegisterDeviceId(this);
		}

		public async Promise<PasswordResetOperation> ForgotPassword()
		{
			return await _collection.ForgotPassword(this);
		}

		public async Promise<PasswordResetConfirmOperation> ConfirmPassword(string code, string newPassword)
		{
			return await _collection.ConfirmPassword(code, newPassword, this);
		}

		internal void Update(User user)
		{
			_user = user;

			if (!string.IsNullOrEmpty(_user?.email))
			{
				email.Set(_user.email);
			}

			deviceIds = user?.deviceIds?.ToArray() ?? new string[]{};
			
			gamerTag = _user?.id ?? 0;
		}
		
		internal void Update(BeamableToken token)
		{
			this.token.type = token.type;
			this.token.accessToken = token.accessToken;
			this.token.refreshToken = token.refreshToken;
			this.token.expiresIn = token.expiresIn;
		}

	}

	[Serializable]
	public class BeamableToken
	{
		public string accessToken;
		public string refreshToken;
		public long expiresIn;
		public string type;

		public static implicit operator BeamableToken(TokenResponse data) =>
			new BeamableToken
			{
				accessToken = data.access_token,
				refreshToken = data.refresh_token,
				expiresIn = data.expires_in,
				type = data.token_type,
			};

		public static BeamableToken FromAccessToken(IAccessToken data) =>
			new BeamableToken
			{
				accessToken = data.Token,
				refreshToken = data.RefreshToken,
				expiresIn = (long)(data.ExpiresAt - DateTime.UtcNow).TotalMilliseconds,
				type = "token",
			};

		public static implicit operator TokenResponse(BeamableToken data) =>
			new TokenResponse
			{
				access_token = data.accessToken,
				refresh_token = data.refreshToken,
				expires_in = data.expiresIn,
				token_type = data.type,
			};
	}
	
	[Serializable]
	public class RegistrationResult
	{
		public PlayerRegistrationError error;
		public PlayerAccount account;
	}

	[Serializable]
	public class PasswordResetOperation
	{
		private readonly PlayerAccounts _collection;
		public PasswordResetError error;
		public PlayerAccount account;

		public PasswordResetOperation(PlayerAccounts collection)
		{
			_collection = collection;
		}
		
		public async Promise<PasswordResetConfirmOperation> Confirm(string code, string newPassword)
		{
			return await _collection.ConfirmPassword(code, newPassword, account);
		}
	}
	
	[Serializable]
	public class PasswordResetConfirmOperation
	{
		public PasswordResetConfirmError error;
		public PlayerAccount account;
	}

	
	[Serializable]
	public class PlayerRecoveryOperation
	{
		public PlayerLoginError error;
		public bool realmAlreadyHasGamerTag;
		public PlayerAccount account;
	}

	public enum PlayerLoginError
	{
		NONE,
		UNKNOWN_CREDENTIALS
	}

	public enum PlayerRegistrationError
	{
		NONE,
		ALREADY_HAS_CREDENTIAL,
		CREDENTIAL_IS_ALREADY_TAKEN
	}

	public enum PasswordResetError
	{
		NONE,
		NO_EXISTING_CREDENTIAL
	}
	
	public enum PasswordResetConfirmError
	{
		NONE,
	}
	
	
	[Serializable]
	public class PlayerAccounts : AbsObservableReadonlyList<PlayerAccount>
	{
		private readonly BeamContext _ctx;
		private readonly IAuthService _authService;
		private readonly AccessTokenStorage _storage;
		private readonly IBeamableRequester _requester;
		private readonly IDependencyProvider _provider;

		public PlayerAccounts(BeamContext ctx, 
		                      IAuthService authService, 
		                      AccessTokenStorage storage, 
		                      IBeamableRequester requester, 
		                      IDependencyProvider provider)
		{
			_ctx = ctx;
			_authService = authService;
			_storage = storage;
			_requester = requester;
			_provider = provider;

			Current = new PlayerAccount(this, BeamableToken.FromAccessToken(_ctx.AccessToken), _ctx.AuthorizedUser);
		}

		/// <summary>
		/// The currently signed in <see cref="PlayerAccount"/>.
		/// There is always a current account, but it may be anonymous.
		/// </summary>
		public PlayerAccount Current;

		public async Promise SwitchToAccount(PlayerAccount account)
		{
			await _ctx.ChangeAuthorizedPlayer(account.token);
			await Refresh();
		}

		public async Promise<PlayerAccount> CreateNewAccount()
		{
			var tokenResponse = await _authService.CreateUser();
			var accessToken = new AccessToken(_storage, _ctx.Cid, _ctx.Pid, tokenResponse.access_token,
			                                  tokenResponse.refresh_token, tokenResponse.expires_in);
			_storage.StoreDeviceRefreshToken(_ctx.Cid, _ctx.Pid, accessToken);
			var user = await _authService.GetUser(tokenResponse);
			await Refresh();
			return this.FirstOrDefault(x => x.gamerTag == user.id);
		}
		
		public async Promise<PlayerRecoveryOperation> RecoverAccountWithEmail(string email, string password)
		{
			TokenResponse res;
			var op = new PlayerRecoveryOperation();
			try
			{
				try
				{
					res = await _authService.Login(email, password);
				}
				catch (PlatformRequesterException ex) when (ex.Error?.error == "UnableToMergeError")
				{
					op.realmAlreadyHasGamerTag = true;
					res = await _authService.Login(email, password, false);
				}
			} catch (PlatformRequesterException) //when (error.Error?.error == "InvalidCredentialsError")
			{
				return new PlayerRecoveryOperation {error = PlayerLoginError.UNKNOWN_CREDENTIALS};
			}

			var user = await _authService.GetUser(res);

			op.account = new PlayerAccount(this, res, user);
			return op;
		}

		public async Promise<PasswordResetConfirmOperation> ConfirmPassword(string code, string newPassword, PlayerAccount account = null)
		{
			if (account == null)
			{
				account = Current;
			}

			var res = new PasswordResetConfirmOperation {account = account};
			var service = GetAuthServiceForAccount(account);

			await service.ConfirmPasswordUpdate(code, newPassword);
			await Refresh();
			return res;
		}

		public async Promise<PasswordResetOperation> ForgotPassword(PlayerAccount account=null)
		{
			if (account == null)
			{
				account = Current;
			}

			var res = new PasswordResetOperation(this) {account = account};

			if (!account.HasEmail)
			{
				res.error = PasswordResetError.NO_EXISTING_CREDENTIAL;
				return res;
			}
			
			var service = GetAuthServiceForAccount(account);
			await service.IssuePasswordUpdate(account.email);

			return res;
		}

		public async Promise UnregisterDeviceId(PlayerAccount account = null)
		{
			if (account == null)
			{
				account = Current;
			}
			
			var service = GetAuthServiceForAccount(account);
			await service.RemoveDeviceId();
		}
		
		public async Promise<RegistrationResult> RegisterDeviceId(PlayerAccount account=null)
		{
			if (account == null)
			{
				account = Current;
			}

			var res = new RegistrationResult {account = account};
			var service = GetAuthServiceForAccount(account);

			var isAvailable = await service.IsThisDeviceIdAvailable();
			if (!isAvailable)
			{
				res.error = PlayerRegistrationError.CREDENTIAL_IS_ALREADY_TAKEN;
				return res;
			}
			var user = await service.RegisterDeviceId();
			account.Update(user);
			await Refresh();
			return res;
		}
		
		public async Promise<RegistrationResult> RegisterEmail(string email, string password, PlayerAccount account=null)
		{
			if (account == null)
			{
				account = Current;
			}
			
			var res = new RegistrationResult {account = account};
			if (account.HasEmail)
			{
				res.error = PlayerRegistrationError.ALREADY_HAS_CREDENTIAL;
				return res;
			}

			var service = GetAuthServiceForAccount(account);
			try
			{
				var user = await service.RegisterDBCredentials(email, password);
				account.Update(user);
			}
			catch (RequesterException ex) when (ex.Status == 400 && ex.RequestError.error == "EmailAlreadyRegisteredError")
			{
				res.error = PlayerRegistrationError.CREDENTIAL_IS_ALREADY_TAKEN;
				return res;
			}

			await Refresh();

			return res;
		}
		
			
		public async Promise RemoveFromDevice(PlayerAccount account)
		{
			_storage.RemoveDeviceRefreshToken(_ctx.Cid, _ctx.Pid, account.token);
			await Refresh();
		}

		public async Promise RemoveAllAccounts()
		{
			// this won't clear the current user, just the other stored ones.
			_storage.ClearDeviceRefreshTokens(_ctx.Cid, _ctx.Pid);
			await Refresh();
		}

		private IAuthService GetAuthServiceForAccount(PlayerAccount account)
		{
			if (account.gamerTag == Current.gamerTag) return _authService;
			var requester = _requester.WithAccessToken(account.token);
			var subScope = _provider.Fork(builder =>
			{
				builder
					.AddSingleton(requester);
			});
			return subScope.GetService<IAuthService>();
		}

		protected override async Promise PerformRefresh()
		{
			await _ctx.OnReady;

			var next = new List<PlayerAccount>();
			var ids = new HashSet<long>();
			
			// get the current user.
			var current = await _authService.GetUser();
			ids.Add(current.id);

			Current.Update(current);
			Current.Update(BeamableToken.FromAccessToken(_authService.Requester.AccessToken));
			next.Add(Current);
			
			var tokens = _storage.RetrieveDeviceRefreshTokens(_ctx.Cid, _ctx.Pid);
	
			var promises = Array.ConvertAll(tokens,
			                                token => _authService.GetUser(token).Map(user => new PlayerAccount(this, token, user)));

			await Promise.Sequence(promises);
			foreach (var p in promises)
			{
				var res = p.GetResult();
				if (!ids.Contains(res.gamerTag))
				{
					next.Add(res);
				}
			}
			
			SetData(next);
		}
	}
}
