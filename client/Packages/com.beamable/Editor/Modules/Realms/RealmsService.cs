using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Editor.Realms
{
	public class RealmServiceException : Exception
	{

		public RealmServiceException(string message) : base(message)
		{

		}
	}

	public class RealmsService
	{
		private readonly IBeamableRequester _requester;
		private readonly EditorAPI _api;

		public RealmsService(IBeamableRequester requester, EditorAPI api)
		{
			_requester = requester;
			_api = api;
		}

		public Promise<CustomerView> GetCustomerData()
		{
			if (string.IsNullOrEmpty(_api.CidOrAlias))
			{
				return Promise<CustomerView>.Successful(new CustomerView());
			}

			return _requester.Request<GetCustomerResponseDTO>(Method.GET, $"/basic/realms/customer", useCache: true).Map(resp => new CustomerView
			{
				Cid = resp.customer.cid.ToString(),
				Alias = resp.customer.alias,
				DisplayName = resp.customer.name,
				Projects = ProcessProjects(resp.customer.projects)
			});
		}

		public Promise<List<RealmView>> GetGames()
		{
			if (string.IsNullOrEmpty(_api.CidOrAlias))
			{
				return Promise<List<RealmView>>.Failed(new Exception("No Cid Available"));
			}

			return _requester.Request<GetGameResponseDTO>(Method.GET, $"/basic/realms/games", useCache: true)
			   .Map(resp =>
			   {

				   var processed = ProcessProjects(resp.projects);
				   return processed;
			   })
			   .Recover(ex =>
			{
				if (ex is PlatformRequesterException err && err.Status == 403)
				{
					return new List<RealmView>(); // empty list.
				}
				throw ex;
			});
		}

		public Promise<RealmView> GetRealm()
		{
			if (string.IsNullOrEmpty(_api.CidOrAlias))
			{
				return Promise<RealmView>.Failed(new RealmServiceException("No Cid Available"));
			}
			if (string.IsNullOrEmpty(_api.Pid))
			{
				return Promise<RealmView>.Successful(null);
			}

			return GetRealms().Map(all => { return all.Find(v => v.Pid == _api.Pid); });
		}

		private List<RealmView> ProcessProjects(List<ProjectViewDTO> projects)
		{
			var map = projects.Select(p => new RealmView
			{
				Cid = _api.Cid,
				Pid = p.pid,
				ProjectName = p.projectName,
				Archived = p.archived,
				Children = new List<RealmView>()
			}).ToDictionary(p => p.Pid);

			// Identify the root, and sort out parent/child relationships
			var rootPids = new List<string>();
			foreach (var p in projects)
			{
				var self = map[p.pid];
				if (!string.IsNullOrEmpty(p.parent))
				{
					var parent = map[p.parent];
					self.Parent = parent;
					parent.Children.Add(self);
				}
				else
				{
					rootPids.Add(p.pid);
				}
			}

			// perform a BFS to identify the depth of each node
			foreach (var rootPid in rootPids)
			{
				var toExplore = new Queue<RealmView>();
				toExplore.Enqueue(map[rootPid]);
				var visited = new HashSet<RealmView>();
				while (toExplore.Count > 0)
				{
					var curr = toExplore.Dequeue();
					if (visited.Contains(curr))
					{
						continue; // we've already seen this node. Don't do anything. This is a safety measure, it should never happen, but it COULD given malformed data from the server.
					}

					visited.Add(curr);
					foreach (var child in curr.Children)
					{
						child.Depth = curr.Depth + 1;
						toExplore.Enqueue(child);
					}
				}
			}

			return map.Values.ToList();
		}

		public Promise<List<RealmView>> GetRealms(RealmView game = null)
		{
			var pid = game?.Pid ?? _api.Pid;
			if (string.IsNullOrEmpty(pid))
			{
				return Promise<List<RealmView>>.Successful(new List<RealmView>());
			}

			// TODO: Consider using helper methods here to do the parent/child stuff, and the bfs-depth-find stuff
			return _requester.Request<GetGameResponseDTO>(Method.GET, $"/basic/realms/game?rootPID={pid}", useCache: true)
			   .Map(resp => ProcessProjects(resp.projects))
			   .Recover(ex =>
			   {
				   if (ex is PlatformRequesterException err && err.Status == 403)
				   {
					   return new List<RealmView>(); // empty set.
				   }

				   throw ex;
			   });

		}
	}

	public class CustomerView
	{
		public string Cid;
		public string Alias;
		public string DisplayName;
		public List<RealmView> Projects;
	}

	public class RealmView : ISearchableElement
	{
		private const string PRODUCTION_DROPDOWN_CLASS_NAME = "production";
		private const string STAGING_DROPDOWN_CLASS_NAME = "staging";

		public string Pid;
		public string Cid;
		public string ProjectName;
		public bool Archived;
		public List<RealmView> Children = new List<RealmView>();
		public RealmView Parent;
		public int Depth { get; set; }
		public string DisplayName { get => IsProduction ? $"[PROD] {ProjectName}" : ProjectName; }

		public bool IsProduction => Depth == 0;
		public bool IsStaging => Depth == 1;

		public override bool Equals(object obj)
		{
			return Equals(obj as RealmView);
		}

		public bool Equals(RealmView other)
		{
			if (other == null) return false;
			return Pid == other.Pid;
		}

		public override int GetHashCode()
		{
			var hashCode = (Pid != null ? Pid.GetHashCode() : 0);
			return hashCode;
		}

		public RealmView FindRoot()
		{
			return Parent == null
				   ? this
				   : Parent.FindRoot();
		}

		public int GetOrder()
		{
			return -Depth;
		}

		public bool IsAvailable()
		{
			return !Archived;
		}

		public bool IsToSkip(string filter)
		{
			return !string.IsNullOrEmpty(filter) && !ProjectName.ToLower().Contains(filter);
		}

		public string GetClassNameToAdd()
		{
			if (IsProduction)
			{
				return PRODUCTION_DROPDOWN_CLASS_NAME;
			}
			else if (IsStaging)
			{
				return STAGING_DROPDOWN_CLASS_NAME;
			}

			return string.Empty;
		}
	}

	[System.Serializable]
	public class GetGameResponseDTO
	{
		public List<ProjectViewDTO> projects;
	}

	[System.Serializable]
	public class GetCustomerResponseDTO
	{
		public CustomerViewDTO customer;
	}
	[System.Serializable]
	public class ProjectViewDTO
	{
		public string projectName;
		public string pid;
		public long cid;
		public bool sharded;
		public string parent;
		public List<string> children;
		public bool archived;
	}

	[System.Serializable]
	public class CustomerViewDTO
	{
		public List<ProjectViewDTO> projects;
		public long cid;
		public string name;
		public string alias;
	}
}
