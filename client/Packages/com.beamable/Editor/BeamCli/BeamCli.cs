using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Environment;
using System.Threading.Tasks;
using Beamable.Api;
using Beamable.Common.Api.Realms;
using Beamable.Editor.Modules.Account;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Editor.Usam;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli
{
	public static class RealmViewExtensions
	{
		public static string GetDisplayName(this RealmView realmView)
		{
			if (realmView == null) return null;

			var name = realmView.DisplayName;
			var pid = realmView.Pid;
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pid)) return null;
			
			return $"{name} - {pid}";
		}
	}
	
	[Serializable]
	public class BeamCli 
		: IStorageHandler<BeamCli>, Beamable.Common.Dependencies.IServiceStorable
		, ILoadWithContext
		, IUserContext
		, IRuntimeConfigProvider
		, IPlatformRequesterHostResolver
		, ISerializationCallbackReceiver
	,
		  IBeamDeveloperAuthProvider
	{
		private readonly IDependencyProvider _provider;

		public BeamConfigCommandResult latestConfig;
		public BeamAccountMeCommandOutput latestAccount;
		public BeamRealmsListCommandOutput latestRealmInfo;
		public BeamGameListCommandResults latestGames;
		public string latestAlias;
		public string latestLoginError;
		
		
		[NonSerialized]
		public List<RealmView> latestRealms = new List<RealmView>();
		[NonSerialized]
		public Dictionary<string, RealmView> pidToRealm = new Dictionary<string, RealmView>();

		public AccessToken latestToken;
		public EditorUser latestUser;
		public BeamConfigRoutesCommandResults latestRouteInfo;
		private StorageHandle<BeamCli> _handle;
		private ConfigWrapper _configInvoke;
		private MeWrapper _meInvoke;
		private OrgRealmsWrapper _realmsInvoke;
		private OrgGamesWrapper _gamesInvoke;
		private ConfigRoutesWrapper _routesInvoke;
		private ProjectAddUnityProjectWrapper _linkCommand;
		private ProjectAddPathsWrapper _addPathsInvoke;

		// public Dictionary<string, BeamOrgRealmData> pidToRealm = new Dictionary<string, BeamOrgRealmData>();

		public BeamCli(IDependencyProvider provider)
		{
			_provider = provider;
		}
		
		public void ReceiveStorageHandle(StorageHandle<BeamCli> handle)
		{
			_handle = handle;
		}

		public bool CanBuildCommands => _provider != null;
		public BeamCommands Command => DependencyBuilder.Instantiate<BeamCommands>(_provider);

		public async Promise<bool> IsAvailable()
		{
			var comm = new BeamCommand(_provider.GetService<BeamableDispatcher>());

			var instance = Command.Version();
			comm.AutoLogErrors = false;
			try
			{
				await instance.Run();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public async Promise Refresh()
		{
			_addPathsInvoke?.Cancel();
			var configPath = Path.Combine(latestConfig?.configPath ?? ".beamable", "config.beam.json");
			BeamConfigFile file = new BeamConfigFile();
			if (File.Exists(configPath))
			{
				var configJson = File.ReadAllText(configPath);
				file = JsonUtility.FromJson<BeamConfigFile>(configJson);
			}

			
			var args = new ProjectAddPathsArgs
			{
				pathsToIgnore = BeamablePackages.CliPathsToIgnore.ToArray(),
				saveExtraPaths = BeamablePackages.GetManifestFileReferences().ToArray()
			};
			var pathsToIgnoreMatch =
				new HashSet<string>(args.pathsToIgnore).IsSubsetOf(new HashSet<string>(file.ignoredProjectPaths));
			var pathsToAddMatch =
				new HashSet<string>(args.saveExtraPaths).IsSubsetOf(new HashSet<string>(file.additionalProjectPaths));
			if (!pathsToAddMatch || !pathsToIgnoreMatch)
			{
				_addPathsInvoke = Command.ProjectAddPaths(args);
				await _addPathsInvoke.Run();
			}
			var regeneratePromise = Command.ProjectOpen(new ProjectOpenArgs
			{
				onlyGenerate = true,
				sln = UsamService.SERVICES_SLN_PATH,
				fromUnity = true
			}).Run();
			
			_configInvoke?.Cancel();
			_configInvoke = Command.Config(new ConfigArgs());
			_configInvoke.OnError(dp =>
			{
				Debug.LogError("Failed to fetch config...");
				// need to show a login flow
			});

			_configInvoke.OnStreamConfigCommandResult(dp =>
			{
				latestConfig = dp.data;
			});
			
			_meInvoke?.Cancel();
			_meInvoke = Command.Me();
			_meInvoke.OnError(dp =>
			{
				if (!string.Equals("Not logged in", dp.data.message, StringComparison.InvariantCultureIgnoreCase))
				{
					Debug.LogError($"Could not fetch Beamable account info. Error=[{dp.data.message}]");
				}
			});
			_meInvoke.OnStreamAccountMeCommandOutput(dp =>
			{
				latestAccount = dp.data;
				ReconstituteUser();
			});

			

			_routesInvoke?.Cancel();
			_routesInvoke = Command.ConfigRoutes(new ConfigArgs());
			_routesInvoke.OnStreamConfigRoutesCommandResults(dp =>
			{
				latestRouteInfo = dp.data;
			});

			var configPromise = _configInvoke.Run();
			var routePromise = _routesInvoke.Run();
			var mePromise = _meInvoke.Run();

			_linkCommand?.Cancel();
			_linkCommand = Command.ProjectAddUnityProject(new ProjectAddUnityProjectArgs
			{
				path = "."
			});
			var linkPromise = _linkCommand.Run();
			
			await configPromise;
			await mePromise;
			await routePromise;
			await linkPromise;
			await regeneratePromise;
			
			
			if (IsLoggedOut)
			{
				pidToRealm.Clear();
				latestRealms.Clear();
				latestRealmInfo = default;
			}
			else
			{
				_realmsInvoke?.Cancel();
				_realmsInvoke = Command.OrgRealms(new OrgRealmsArgs());
				_realmsInvoke.OnError(dp =>
				{
					Debug.LogError($"failed to fetch realms err=[{dp.data.message}]");
				});
				_realmsInvoke.OnStreamRealmsListCommandOutput(dp =>
				{
					latestRealmInfo = dp.data;
					latestAlias = latestRealmInfo.CustomerAlias;
					ReconstituteRealmData();
				});

				_gamesInvoke?.Cancel();
				_gamesInvoke = Command.OrgGames(new OrgGamesArgs());
				_gamesInvoke.OnError(dp =>
				{
					Debug.LogError($"failed to fetch games. err=[{dp.data.message}]");
				});
				_gamesInvoke.OnStreamGameListCommandResults(dp =>
				{
					latestGames = dp.data;
				});
				
				var realmsPromise = _realmsInvoke.Run();
				var gamesPromise = _gamesInvoke.Run();
				await realmsPromise;
				await gamesPromise;
			}
		}

		void ReconstituteUser()
		{
			if (string.IsNullOrEmpty(latestAccount?.email))
			{
				latestUser = null;
				latestToken = null;
				return;
			}
			
			latestUser = new EditorUser
			{
				email = latestAccount.email,
				id = latestAccount.id,
#pragma warning disable CS0618 // Type or member is obsolete
				roleString = latestAccount.roleString,
#pragma warning restore CS0618 // Type or member is obsolete
				roles = latestAccount.roles.Select(r => new EditorUserRealmPermission
				{
					projectId = r.pid,
					role = r.role
				}).ToList()
			};

			var storage = new AccessTokenStorage("editor");
			latestToken = new AccessToken(storage,
			                              latestAccount.tokenCid, 
			                              latestAccount.tokenPid,
			                              latestAccount.accessToken, 
			                              latestAccount.refreshToken,
			                              latestAccount.tokenExpiresIn);
		}
		
		void ReconstituteRealmData()
		{
			// construct the realmViews
			if ( (latestRealmInfo?.VisibleRealms?.Length ?? 0) == 0)
			{
				latestRealms?.Clear();
				pidToRealm?.Clear();
				return;
			}
			
			var dtos = latestRealmInfo.VisibleRealms.Select(realm => new ProjectViewDTO
			{
				archived = false,
				cid = long.Parse(realm.Cid),
				pid = realm.Pid,
				projectName = realm.RealmName,
				parent = realm.ParentPid
			}).ToList();
			
			latestRealms = RealmsService.ProcessProjects(latestRealmInfo.VisibleRealms[0].Cid, dtos);
				
			pidToRealm = latestRealms.ToDictionary(realm => realm.Pid);
		}
		
		public async Promise Logout()
		{
			latestAccount = null;
			latestUser = null;
			latestToken = null;
			await Command.Logout().Run();
		}

		// public async Promise Login()
		
		public async Promise Login(string host, string cidOrAlias, string email, string password)
		{
			latestLoginError = null;
			await Command.Config(new ConfigArgs())
			             .OnStreamConfigCommandResult(dp =>
			             {
				             var beamFolder = dp.data.configPath;
				             var overridesFile = Path.Combine(beamFolder, "temp", "overrides", "connection-configuration.json");
				             if (File.Exists(overridesFile))
				             {
					             File.Delete(overridesFile);
				             }
			             })
			             .OnError(_ =>
			             {
				             // there is no beamable folder to clear out; so no harm no foul
			             })
			             .Run();
			
			var initArgs = new InitArgs
			{
				host = host,
				email = email,
				password = password,
				ignorePid = true,
				pathsToIgnore = BeamablePackages.CliPathsToIgnore.ToArray(),
				saveExtraPaths = BeamablePackages.GetManifestFileReferences().ToArray()
			};
			var command = Command;
			command.ModifierNextDefault(args =>
			{
				args.cid = cidOrAlias;
				args.host = host;
			});

			var initInvoke = command.Init(initArgs);
			initInvoke.OnStreamInitCommandResult(dp =>
			{
			});
			initInvoke.OnErrorLoginFailedError(dp =>
			{
				latestLoginError = $"Failed to log in, {dp.data.message}";
			});
			initInvoke.OnError(dp =>
			{
				latestLoginError = "Failed to log in.";
			});
			
			
			var initPromise = initInvoke.Run();
			await initPromise;

			await Refresh();
		}

		public async Promise SwitchRealms(string pid)
		{
			var configArgs = new ConfigArgs
			{
				set = true,
			};
			var command = Command;
			command.ModifierNextDefault(defArgs =>
			{
				defArgs.pid = pid;
			});
			var configInvoke = command.Config(configArgs);
			configInvoke.OnStreamConfigCommandResult(dp =>
			{
				latestConfig = dp.data;
			});
			var configPromise = configInvoke.Run();
			await configPromise;

			await Refresh();
		}
		
		public RealmView CurrentRealm
		{
			get
			{
				RealmView realm = null;
				if (Pid == null) return null;
				if (pidToRealm?.TryGetValue(Pid, out realm) ?? false)
				{
					return realm;
				}

				return null;
			}
		}

		public RealmView ProductionRealm
		{
			get
			{
				var gamePid = CurrentRealm?.GamePid;
				if (string.IsNullOrEmpty(gamePid)) return null;

				return pidToRealm[gamePid];
			}
		}
		public bool HasCid => !string.IsNullOrEmpty(Cid);
		public string Cid => latestConfig?.cid;
		public string Alias => latestRealmInfo?.CustomerAlias;
		public string Pid => latestConfig?.pid;
		public long UserId => latestAccount.id;
		public bool IsLoggedOut => string.IsNullOrEmpty(latestAccount?.email);
		public UserPermissions Permissions => latestUser?.GetPermissionsForRealm(Pid) ?? new UserPermissions("");

		public string HostUrl => latestConfig?.host;
		public string Host => latestConfig?.host;
		public string PortalUrl => latestRouteInfo.portalUri;
		public PackageVersion PackageVersion => BeamableEnvironment.SdkVersion;
		public void OnBeforeSerialize()
		{
		}
		
		public void OnAfterDeserialize()
		{
			ReconstituteRealmData();
			ReconstituteUser();
		}

		public void OnBeforeSaveState()
		{
		}
		public void OnAfterLoadState()
		{
			OnAfterDeserialize();
		}

		string IBeamDeveloperAuthProvider.AccessToken => latestToken.Token;
		string IBeamDeveloperAuthProvider.RefreshToken => latestToken.RefreshToken;

		[Serializable]
		public class BeamConfigFile
		{
			public List<string> additionalProjectPaths = new List<string>();

			public List<string> ignoredProjectPaths = new List<string>();
			// "additionalProjectPaths": [],
			// "ignoredProjectPaths": [
			// "Packages/com.beamable",
			// "Library/PackageCache/com.beamable",
			// "Library/PackageCache/com.beamable.server"
			// ],
		}
	}
}
