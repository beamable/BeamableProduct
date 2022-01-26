using Beamable.Common;
using Beamable.Editor.Modules.Account;
using Beamable.Editor.Realms;
using System;
using System.Collections.Generic;

namespace Beamable.Editor.Login.UI.Model
{
	public class LoginModel
	{
		public CustomerModel Customer { get; } = new CustomerModel();
		//public CustomerModel LoadedCustomer { get; } = new CustomerModel();

		public List<RealmView> Games { get; set; } = new List<RealmView>();

		public bool StartedWithConfiguration { get; private set; } = false;
		public bool StartedWithUser { get; private set; } = false;
		public string LastError { get; private set; }

		public bool ReadLegalCopy { get; set; }

		public event Action<string> OnError;
		public event Action OnErrorCleared;
		public event Action<List<RealmView>> OnGamesUpdated;

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

		public Promise<List<RealmView>> ResetGames()
		{
			return EditorAPI.Instance.FlatMap(b => b.RealmService.GetGames().Map(games =>
			{
				Games = games;
				OnGamesUpdated?.Invoke(games);
				return games;
			}));
		}

		public void Destroy()
		{
			EditorAPI.Instance.Then(b =>
			{
				b.OnUserChange -= OnUserChanged;
				b.OnRealmChange -= SetRealm;
			});
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

		public Promise<LoginModel> Initialize()
		{
			return EditorAPI.Instance.Map(b =>
			{
				SetUser(b.User);
				SetRealm(b.Realm);
				SetGame(b.ProductionRealm);

				StartedWithConfiguration = b.HasConfiguration;
				StartedWithUser = b.HasToken;

				Customer.Clear();
				if (b.HasConfiguration)
				{
					if (b.HasCustomer)
					{
						Customer.SetCidPid(b.CidOrAlias, b.Pid);
					}
					else
					{
						Customer.Clear();
					}
				}

				CurrentCustomer = b.CustomerView;

				b.OnUserChange += OnUserChanged;
				b.OnRealmChange += SetRealm;
				b.OnCustomerChange += SetCustomer;

				b.RealmService.GetGames().Then(games =>
			 {
				   Games = games;
				   OnGamesUpdated?.Invoke(games);
			   });

				if (b.HasToken)
				{
					Customer.Role = b.User.roleString;
					Customer.SetUserInfo(b.User.id, b.User.email);
				}

				return this;
			});
		}

		private void OnUserChanged(EditorUser user)
		{
			Customer.Role = user?.roleString;
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
