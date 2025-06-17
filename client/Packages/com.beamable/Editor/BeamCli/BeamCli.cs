using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Environment;
using System.Threading.Tasks;
using Beamable.Common.Api.Realms;
using Beamable.Editor.Modules.Account;
using UnityEngine;

namespace Beamable.Editor.BeamCli
{
	public class BeamCli 
		: ILoadWithContext
		, IUserContext
		, IRuntimeConfigProvider
	{
		private readonly IDependencyProvider _provider;
		private readonly BeamEditorContext _ctx;

		public BeamConfigCommandResult latestConfig;
		public BeamAccountMeCommandOutput latestAccount;
		public BeamRealmsListCommandOutput latestRealmInfo;
		public List<RealmView> latestRealms = new List<RealmView>();
		public Dictionary<string, RealmView> pidToRealm = new Dictionary<string, RealmView>();

		public EditorUser latestUser;
		
		// public Dictionary<string, BeamOrgRealmData> pidToRealm = new Dictionary<string, BeamOrgRealmData>();

		public BeamCli(IDependencyProvider provider, BeamEditorContext ctx)
		{
			_provider = provider;
			_ctx = ctx;
		}

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
			// TODO: allow cancelling inflight commands
			// TODO: check that data stays serialized
			
			// TODO: 
			// var extraPaths = BeamablePackages.GetManifestFileReferences();
			// args.saveExtraPaths = extraPaths.ToArray();

			// TODO:
			// var linkCommand = Command.ProjectAddUnityProject(new ProjectAddUnityProjectArgs
			// {
			// 	path = "."
			// });
			
			var configInvoke = Command.Config(new ConfigArgs());
			configInvoke.OnError(dp =>
			{
				Debug.LogError("Failed to fetch config...");
				// need to show a login flow
			});

			configInvoke.OnStreamConfigCommandResult(dp =>
			{
				latestConfig = dp.data;
			});
			
			var meInvoke = Command.Me();
			meInvoke.OnError(dp =>
			{
				Debug.Log("Not signed in");
			});
			meInvoke.OnStreamAccountMeCommandOutput(dp =>
			{
				latestAccount = dp.data;

				latestUser = new EditorUser
				{
					email = latestAccount.email,
					id = latestAccount.id,
					roles = latestAccount.roles.Select(r => new EditorUserRealmPermission
					{
						projectId = r.pid,
						role = r.role
					}).ToList()
				};
			});

			var realmsInvoke = Command.OrgRealms(new OrgRealmsArgs());
			realmsInvoke.OnError(dp =>
			{
				Debug.Log($"failed to fetch realms err=[{dp.data.message}]");
			});
			realmsInvoke.OnStreamRealmsListCommandOutput(dp =>
			{
				latestRealmInfo = dp.data;
				
				// construct the realmViews
				var dtos = latestRealmInfo.VisibleRealms.Select(realm => new ProjectViewDTO
				{
					archived = false,
					cid = long.Parse(realm.Cid),
					pid = realm.Pid,
					projectName = realm.ProjectName,
					parent = realm.ParentPid
				}).ToList();
				latestRealms = RealmsService.ProcessProjects(latestRealmInfo.VisibleRealms[0].Cid, dtos);

				
				pidToRealm = latestRealms.ToDictionary(realm => realm.Pid);
			});
			
			var configPromise = configInvoke.Run();
			var mePromise = meInvoke.Run();

			await configPromise;
			await mePromise;
		}
		
		public async Promise Logout()
		{
			latestAccount = null;
			await Command.Logout().Run();
		}

		public async Promise SwitchRealms(string pid)
		{
			var args = new InitArgs
			{
				noTokenSave = true,
			};
			Command.ModifierNextDefault(defArgs =>
			{
				defArgs.cid = latestRealmInfo.Cid;
				defArgs.pid = pid;
			});
			var initInvoke = Command.Init(args);

			
			var initPromise = initInvoke.Run();
			await initPromise;
		}
		
		public RealmView CurrentRealm
		{
			get
			{
				if (pidToRealm.TryGetValue(Pid, out var realm))
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
		public string Cid => latestConfig.cid;
		public string Pid => latestConfig.pid;
		public long UserId => latestAccount.id;

		public UserPermissions Permissions => new UserPermissions("admin"); // todo; fix this;
	}
}
