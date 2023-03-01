using Beamable.Common;
using Beamable.Common.Api.Realms;
using Beamable.Editor.Modules.Account;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.Login.UI.Model
{
	public class LoginModel
	{
		public CustomerModel Customer { get; } = new CustomerModel();
		public List<RealmView> Games { get; set; } = new List<RealmView>();

		public bool StartedWithConfiguration { get; private set; } = false;
		public bool StartedWithUser { get; private set; } = false;
		public string LastError { get; private set; }

		public bool ReadLegalCopy { get; set; }

		public event Action<string> OnError;
		public event Action OnErrorCleared;

		public CustomerView CurrentCustomer { get; private set; }

		public EditorUser CurrentUser { get; private set; }
		public RealmView CurrentGame { get; private set; }
		public RealmView CurrentRealm { get; private set; }
		public event Action<EditorUser> OnCurrentUserChanged;
		public event Action<RealmView> OnGameChanged;
		public event Action<LoginModel> OnStateChanged;

		public LoginModel()
		{

		}

		public void SetError(string error)
		{
			LastError = error;
			if (string.IsNullOrEmpty(LastError))
			{
				OnErrorCleared?.Invoke();
			}
			else
			{
				OnError?.Invoke(LastError);
			}
		}

		public void Destroy()
		{
			BeamEditorContext.Default.OnUserChange -= OnUserChanged;
			BeamEditorContext.Default.OnRealmChange -= SetRealm;
		}

		private void SetUser(EditorUser user)
		{
			CurrentUser = user;
			OnCurrentUserChanged?.Invoke(CurrentUser);
			OnStateChanged?.Invoke(this);
		}

		private void SetRealm(RealmView realm)
		{
			CurrentRealm = realm;
			OnGameChanged?.Invoke(realm);
			OnStateChanged?.Invoke(this);
		}

		public void SetGame(RealmView game)
		{
			CurrentGame = game;
			OnStateChanged?.Invoke(this);
		}

		private void SetCustomer(CustomerView customer)
		{
			CurrentCustomer = customer;
			OnStateChanged?.Invoke(this);
		}

		public async Promise<LoginModel> Initialize()
		{
			var b = BeamEditorContext.Default;
			await b.InitializePromise;
			SetUser(b.CurrentUser);
			SetRealm(b.CurrentRealm);
			SetGame(b.ProductionRealm);

			StartedWithConfiguration = BeamEditorContext.ConfigFileExists;
			StartedWithUser = b.Requester.Token != null;

			Customer.Clear();

			CurrentCustomer = b.CurrentCustomer;
			if (b.HasCustomer && !string.IsNullOrEmpty(b.CurrentCustomer.Alias))
			{
				Customer.SetCidPid(b.CurrentCustomer.Alias, CurrentRealm?.Pid);
			}
			else
			{
				var configService = b.ServiceScope.GetService<ConfigDefaultsService>();
				var maybeAlias = configService.Alias;
				var maybePid = configService.Pid;
				Customer.SetCidPid(maybeAlias.Value, maybePid.Value);
			}


			b.OnUserChange += OnUserChanged;
			b.OnRealmChange += SetRealm;
			b.OnCustomerChange += SetCustomer;

			if (b.HasCustomer)
			{
				// TODO: 
				Games = b.EditorAccount.CustomerGames;
			}

			if (b.HasToken && b.HasCustomer)
			{
				Customer.Role = b.CurrentUser.GetPermissionsForRealm(b.CurrentRealm?.Pid).Role;
				Customer.SetUserInfo(b.CurrentUser.id, b.CurrentUser.email);
			}

			return this;
		}

		private void OnUserChanged(EditorUser user)
		{
			Customer.Role = user?.GetPermissionsForRealm(Customer.Pid).Role;
			SetUser(user);
			if (user == null)
			{
				Customer.SetUserInfo(0, null);
			}
			else
			{
				Customer.SetUserInfo(user.id, user.email);
			}
		}

	}

	public class CustomerModel
	{
		public string CidOrAlias { get; private set; }
		public string Pid { get; private set; }

		public long Id { get; private set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public string Password { get; set; }
		public string Code { get; set; }
		public string PasswordConfirmation { get; set; }


		public bool HasCid => !string.IsNullOrEmpty(CidOrAlias);
		public bool HasGame => !string.IsNullOrEmpty(Pid);
		public bool HasUser => !string.IsNullOrEmpty(Email);
		public bool HasRole => !string.IsNullOrEmpty(Role);

		public event Action OnUpdated;

		public bool HasData => !string.IsNullOrEmpty(CidOrAlias) && string.IsNullOrEmpty(Pid);

		public void SetExistingCustomerData(string cidOrAlias, string email, string password)
		{
			Pid = null;
			PasswordConfirmation = null;
			Id = 0;
			CidOrAlias = cidOrAlias;
			Email = email?.Trim();
			Password = password;
			OnUpdated?.Invoke();
		}
		public void SetNewCustomer(string alias, string gameName, string email, string password)
		{
			Id = 0;
			CidOrAlias = alias;
			Pid = gameName;
			Email = email?.Trim();
			Password = password;
			PasswordConfirmation = null;
			OnUpdated?.Invoke();
		}

		public void SetCustomerData(string email, string password)
		{
			PasswordConfirmation = null;
			Id = 0;
			Email = email?.Trim();
			Password = password;
			OnUpdated?.Invoke();
		}

		public void SetCidPid(string cid, string pid)
		{
			CidOrAlias = cid;
			Pid = pid;
			OnUpdated?.Invoke();
		}

		public void SetPid(string pid)
		{

			Pid = pid;
			OnUpdated?.Invoke();
		}

		public void SetUserInfo(long id, string email)
		{
			Id = id;
			Email = email?.Trim();
			OnUpdated?.Invoke();
		}

		public void SetPasswordForgetData(string cid, string email)
		{
			Email = email?.Trim();
			CidOrAlias = cid;
			Pid = null;
			Id = 0;
			Password = null;
			Code = null;
			OnUpdated?.Invoke();

		}


		public void Clear()
		{
			Code = null;
			Id = 0;
			Email = null;
			Password = null;
			PasswordConfirmation = null;
			CidOrAlias = null;
			Role = null;
			Pid = null;
			OnUpdated?.Invoke();
		}

		public void SetPasswordCode(string code, string password)
		{
			Password = password;
			Code = code;

			OnUpdated?.Invoke();

		}
	}

}
