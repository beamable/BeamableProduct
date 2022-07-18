using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Editor.Content;
using Beamable.Editor.Environment;
using Beamable.Config;
using Beamable.Editor.Config;
using Beamable.Editor.Modules.Account;
using Beamable.Editor.Realms;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.VersionControl;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Task = System.Threading.Tasks.Task;

namespace Beamable.Editor
{

   public class EditorAPI
   {
      private static Promise<EditorAPI> _instance;
      public PlatformRequester Requester => _requester;
      public static Promise<EditorAPI> Instance
      {
         get
         {
            if (_instance == null)
            {
               var de = new EditorAPI();
               _instance = de.Initialize().Error(err =>
               {
                  Debug.LogError(err);
                  de.Logout();
                  _instance = null;
               });
            }

            return _instance;
         }
      }

      // Services
      private AccessTokenStorage _accessTokenStorage;
      private PlatformRequester _requester;
      public EditorAuthService AuthService;
      public ContentIO ContentIO;
      public ContentPublisher ContentPublisher;
      public RealmsService RealmService;
      public event Action<EditorUser> OnUserChange;
      public event Action<RealmView> OnRealmChange;

      public event Action<CustomerView> OnCustomerChange;

      // Info
      private string _cidOrAlias;
      public string CidOrAlias
      {
         get => _cidOrAlias;
         private set => _cidOrAlias = value;
      }

      public string Cid => _requester.Cid;

      public CustomerView CustomerView { get; private set; }

      public string Pid
      {
         get => _requester.Pid;
         set => _requester.Pid = value;
      }

      public string Host => _requester.Host;

      public RealmView Realm { get; private set; }
      public RealmView ProductionRealm { get; private set; }

      public UserPermissions Permissions => User?.GetPermissionsForRealm(Realm.Pid) ?? new UserPermissions(null);

      public EditorUser User;
      public AccessToken Token => _requester.Token;

      public bool HasConfiguration { get; private set; }
      public bool HasToken => Token != null;
      public bool HasCustomer => !string.IsNullOrEmpty(CidOrAlias);
      public bool HasRealm => !string.IsNullOrEmpty(Pid);

      private Promise<EditorAPI> Initialize()
      {
         if (!Application.isPlaying)
         {
            var promiseHandlerConfig = CoreConfiguration.Instance.DefaultUncaughtPromiseHandlerConfiguration;
            switch (promiseHandlerConfig)
            {
               case CoreConfiguration.EventHandlerConfig.Guarantee:
               {
                  if(!PromiseBase.HasUncaughtErrorHandler)
                     PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler();

                  break;
               }
               case CoreConfiguration.EventHandlerConfig.Replace:
               case CoreConfiguration.EventHandlerConfig.Add:
               {
                  PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler(promiseHandlerConfig == CoreConfiguration.EventHandlerConfig.Replace);
                  break;
               }
               default:
                  throw new ArgumentOutOfRangeException();
            }
         }

         // Register services
         BeamableEnvironment.ReloadEnvironment();
         _accessTokenStorage = new AccessTokenStorage("editor.");
         _requester = new PlatformRequester(BeamableEnvironment.ApiUrl, _accessTokenStorage, null);
         _requester.RequestTimeoutMs = $"{30 * 1000}";
         AuthService = new EditorAuthService(_requester);
         _requester.AuthService = AuthService;
         ContentIO = new ContentIO(_requester);
         ContentPublisher = new ContentPublisher(_requester, ContentIO);
         RealmService = new RealmsService(_requester, this);

         HasConfiguration = ConfigDatabase.HasConfigFile(ConfigDatabase.GetConfigFileName());

         if (!HasConfiguration)
         {
            return Reset();
         }

         ConfigDatabase.Init();

         ConfigDatabase.TryGetString("alias", out var alias);
         var cid = ConfigDatabase.GetString("cid");
         var pid = ConfigDatabase.GetString("pid");
         var platform = ConfigDatabase.GetString("platform");
         CidOrAlias = alias ?? cid;

         if (string.IsNullOrEmpty(CidOrAlias)) // with no cid, we cannot be logged in.
         {
            return Reset();
         }

         ApplyConfig(alias, cid, pid, platform);
         BeamableFacebookImporter.SetFlag();

         return _accessTokenStorage.LoadTokenForCustomer(CidOrAlias).FlatMap(token =>
         {
            if (token == null)
            {
               _requester.Token = null; // show state as logged out.
               return Promise<EditorAPI>.Successful(this);
            }

            return Login(token);
         });
      }

      public static void CheckoutPath(string path)
      {
         if (File.Exists(path))
         {
            var fileInfo = new System.IO.FileInfo(path);
            fileInfo.IsReadOnly = false;
         }

         if (!Provider.enabled) return;
         var vcTask = Provider.Checkout(path, CheckoutMode.Asset);
         vcTask.Wait();
         if (!vcTask.success)
         {
            Debug.LogWarning($"Unable to checkout: {path}");
         }
      }

      public Promise<EditorAPI> Reset()
      {
         SaveConfig("", "", BeamableEnvironment.ApiUrl);
         Logout();
         return Promise<EditorAPI>.Successful(this);
      }

      public Promise<Unit> SetGame(RealmView game)
      {
         if (game == null)
         {
            Debug.Log("SetGame: game was null");
            return Promise<Unit>.Failed(new Exception("Cannot set game to null"));
         }

         // we need to remember the last realm the user was on in this game.
         var hadSelectedPid = EditorPrefHelper
            .GetMap(BeamableConstants.REALM_PREFERENCE)
            .TryGetValue($"{CustomerView.Cid}.{game.Pid}", out var existingPid);
         if (!hadSelectedPid)
         {
            existingPid = game.Pid;
         }

         SaveConfig(CustomerView.Alias, existingPid, BeamableEnvironment.ApiUrl, CustomerView.Cid);
         return SwitchRealm(game, existingPid);
      }

      public Promise<Unit> Login(string email, string password)
      {
         return AuthService.Login(email, password, customerScoped: true).FlatMap(tokenRes =>
         {
            var token = new AccessToken(_accessTokenStorage, CidOrAlias, null, tokenRes.access_token, tokenRes.refresh_token, tokenRes.expires_in);

            // use this token.
            return ApplyToken(token);
         });
      }

      public Promise<Unit> LoginCustomer(string cid, string email, string password)
      {
         // Set the config defaults to reflect the new Customer.
         SaveConfig(cid, null, BeamableEnvironment.ApiUrl);

         // Attempt to get an access token.
         return Login(email, password);
      }

      public Promise<Unit> CreateCustomer(string alias, string gameName, string email, string password)
      {

         async Task HandleNewCustomerAndUser(TokenResponse token, string pid)
         {
            SaveConfig(alias, pid);
            await Login(token);
            await DoSilentContentPublish(true);
         }

         var customerName = alias; // TODO: For now...
         SaveConfig(null, null);
         return AuthService.RegisterCustomer(email, password, gameName, customerName, alias).FlatMap(res =>
         {
            var task = HandleNewCustomerAndUser(res.token, res.pid);
            var promise = new Promise<Unit>();
            task.ContinueWith(_ =>
               {
                  // Put the execution back on the Editor thread; lest ye suffer Unity's wrath.
                  EditorApplication.delayCall += () => { promise.CompleteSuccess(PromiseBase.Unit); };
               });
            return promise;
         });
      }

      public Promise<Unit> CreateUser(string cid, string customerEmail, string customerPassword)
      {
         SaveConfig(cid, null, BeamableEnvironment.ApiUrl);

         return AuthService.CreateUser().FlatMap(newToken =>
         {
            var token = new AccessToken(_accessTokenStorage, CidOrAlias, Pid, newToken.access_token,
               newToken.refresh_token, newToken.expires_in);
            _requester.Token = token;
            return AuthService.RegisterDBCredentials(customerEmail, customerPassword).FlatMap(user => Login(token).ToUnit());
         });
      }

      public Promise<Unit> SendPasswordReset(string cid, string email)
      {
         SaveConfig(cid, null, BeamableEnvironment.ApiUrl);
         return AuthService.IssuePasswordUpdate(email).ToUnit();
      }

      public Promise<Unit> SendPasswordResetCode(string code, string newPassword)
      {

         return AuthService.ConfirmPasswordUpdate(code, newPassword).ToUnit();
      }

      public void Logout()
      {
         _requester.DeleteToken();
         User = null;
         OnUserChange?.Invoke(null);
         BeamableEnvironment.ReloadEnvironment();
      }

      public bool HasDependencies()
      {
         var hasAddressables = null != AddressableAssetSettingsDefaultObject.GetSettings(false);
         var hasTextmeshPro = TextMeshProImporter.EssentialsLoaded;

         return hasAddressables && hasTextmeshPro;
      }

      public Promise<Unit> CreateDependencies()
      {
         // import addressables...
         AddressableAssetSettingsDefaultObject.GetSettings(true);

         return TextMeshProImporter.ImportEssentials()
            .FlatMap(_ =>
            {
               AssetDatabase.Refresh();
               ContentIO.EnsureAllDefaultContent();

               ConfigManager.Initialize();

               return ContentIO.OnManifest.FlatMap(serverManifest =>
               {
                  var hasNoContent = serverManifest.References.Count == 0;
                  return hasNoContent ? DoSilentContentPublish() : PromiseBase.SuccessfulUnit;
               });
            });
      }

#if BEAMABLE_DEVELOPER
      [MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Force Refresh Content")]
      public static void ForceRefreshContent()
      {
         // Do these in parallel to simulate startup behavior.
         Instance.Then(editorAPI => editorAPI.ContentIO.BuildLocalManifest());
         Instance.Then(editorAPI => editorAPI.CreateDependencies());
      }
#endif

      /// <summary>
      /// Force a publish operation, with no validation, with no UX popups. Log output will occur.
      /// </summary>
      /// <param name="force">Pass true to force all content to publish. Leave as false to only publish changed content.</param>
      /// <returns>A Promise of Unit representing the completion of the publish.</returns>
      public Promise<Unit> DoSilentContentPublish(bool force=false)
      {
         var clearPromise = force ? ContentPublisher.ClearManifest() : Promise<Unit>.Successful(PromiseBase.Unit);
         return clearPromise
            .FlatMap(_ =>
            {
               return ContentPublisher.CreatePublishSet().FlatMap(set =>
               {
                  return ContentPublisher.Publish(set, progress => {});
               });
            })
            .FlatMap(_ =>
            {
               return ContentIO.FetchManifest();
            }).Map(_ =>
            {
               Debug.Log("Beamable Content Publish: Complete.");
               return PromiseBase.Unit;
            });
      }

      public async Promise<Unit> SwitchRealm(RealmView game, string pid)
      {
         if (game == null)
         {
            throw new Exception("Cannot switch to null game");
         }

         if (!game.IsProduction)
         {

            throw new Exception("Cannot switch to a game that isn't a production realm");
         }

         if (string.IsNullOrEmpty(pid))
         {
            throw new Exception("Cannot switch to a realm with a null pid");
         }

         SaveConfig(CidOrAlias, pid, cid:game.Cid);

         await ContentIO.FetchManifest();
         var realms = await RealmService.GetRealms(game);

         var set = EditorPrefHelper
            .GetMap(BeamableConstants.REALM_PREFERENCE)
            .Set($"{game.Cid}.{game.Pid}", pid)
            .Save();

         var realm = realms.FirstOrDefault(r => string.Equals(r.Pid, pid));

         Realm = realm;
         ProductionRealm = game;
         OnRealmChange?.Invoke(realm);

         return PromiseBase.Unit;
      }

      public Promise<Unit> SwitchRealm(RealmView realm)
      {
         return SwitchRealm(realm.FindRoot(), realm?.Pid);
      }

      public void SaveConfig(string alias, string pid, string host = null, string cid=null, string containerPrefix = null)
      {
         if (string.IsNullOrEmpty(host))
         {
            host = BeamableEnvironment.ApiUrl;
         }

         var config = new ConfigData()
         {
            cid = cid ?? alias,
            alias = alias ?? cid,
            pid = pid,
            platform = host,
            socket = host,
            containerPrefix = containerPrefix
         };

         string path = "Assets/Beamable/Resources/config-defaults.txt";
         var asJson = JsonUtility.ToJson(config, true);

         var writeConfig = true;
         if (File.Exists(path))
         {
            var existingJson = File.ReadAllText(path);
            if (string.Equals(existingJson, asJson))
            {
               writeConfig = false;
            }
         }

         if (writeConfig)
         {
            Directory.CreateDirectory("Assets/Beamable/Resources/");
            CheckoutPath(path);
            File.WriteAllText(path, asJson);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigDatabase.Init();
            AssetDatabase.Refresh();
         }

         HasConfiguration = true;
         ApplyConfig(alias ?? cid, cid ?? alias, pid, host);
      }

      public Promise<EditorAPI> Login(TokenResponse tokenResponse)
      {
         var token = new AccessToken(_accessTokenStorage, CidOrAlias, Pid, tokenResponse.access_token,
            tokenResponse.refresh_token, tokenResponse.expires_in);
         return Login(token);
      }

      public Promise<EditorAPI> Login(AccessToken token)
      {
         return ApplyToken(token)
            .FlatMap(_ =>
            {
               return RealmService.GetRealm()
                  .Recover(ex =>
                  {
                     if (ex is RealmServiceException err)
                     {
                        // there is no realm.
                        return null;
                     }

                     throw ex;
                  })
                  .FlatMap(realm =>
                  {
                     if (realm == null)
                     {
                        return Promise<Unit>.Successful(PromiseBase.Unit); // nothing to do.
                     }

                     return SwitchRealm(realm);
                  });
            })
            .Map(_ => this);
      }

      Promise<Unit> ApplyToken(AccessToken token)
      {
         return token.SaveAsCustomerScoped().FlatMap(_ => InitializeWithToken(token)).ToUnit();
      }

      public Promise<string> GetRealmSecret()
      {
         // TODO this will only work if the current user is an admin.

         return _requester.Request<CustomerResponse>(Method.GET, "/basic/realms/admin/customer").Map(resp =>
         {
            var matchingProject = resp.customer.projects.FirstOrDefault(p => p.name.Equals(Pid));
            return matchingProject?.secret ?? "";
         });
      }

      private void ApplyConfig(string alias, string cid, string pid, string host)
      {
         CidOrAlias = alias;
         Pid = pid;
         _requester.Cid = cid;
         _requester.Pid = pid;
         _requester.Host = host;
      }

      private Promise<Unit> RefreshCustomerData()
      {
         return RealmService.GetCustomerData().Map(data =>
         {
            CustomerView = data;
            SaveConfig(data.Alias, Requester.Pid, cid: data.Cid);
            OnCustomerChange?.Invoke(data);
            return PromiseBase.Unit;
         });
      }

      private Promise<EditorAPI> InitializeWithToken(AccessToken token)
      {
         _requester.Token = token;
         // TODO: This call may fail because we're getting a customer scoped token now..

         return RefreshCustomerData().FlatMap(_ => AuthService.GetUserForEditor().Map(user =>
         {
            User = user;
            OnUserChange?.Invoke(user);
            return this;
         })
            .RecoverWith(ex =>
            {
               if (ex is PlatformRequesterException err && err.Status == 403 )
               {
                  return AuthService.GetUser().Map(user2 =>
                  {
                     User = new EditorUser(user2);
                     OnUserChange?.Invoke(User);
                     return this;
                  });
               } else throw ex;
            })
            .Error(err => { Logout(); }));
      }



   }

   [System.Serializable]
   public class ConfigData
   {
      public string cid;
      public string alias;
      public string pid;
      public string platform;
      public string socket;
      public string containerPrefix;
   }

   [System.Serializable]
   public class CustomerResponse
   {
      public CustomerDTO customer;
   }

   [System.Serializable]
   public class CustomerDTO
   {
      public List<ProjectDTO> projects;
   }

   [System.Serializable]
   public class ProjectDTO
   {
      public string name;
      public string secret;
   }
}
