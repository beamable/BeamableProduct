using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Realms;

namespace Beamable.Editor.Tests
{
	public class MockRealmServiceException : Exception
	{

		public MockRealmServiceException(string message) : base(message)
		{

		}
	}

	public class MockRealmsService
	{
		private readonly PlatformRequester _requester;

		public MockRealmsService(PlatformRequester requester)
		{
			//no platformRequester
			//_requester = requester;

			//make mock version ov PlatformRequester
			//delete this mock realms service

			//MockPlatformAPI already exist
		}

		public Promise<CustomerView> GetCustomerData()
		{
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
			if (string.IsNullOrEmpty(_requester.Cid))
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
			if (string.IsNullOrEmpty(_requester.Cid))
			{
				return Promise<RealmView>.Failed(new MockRealmServiceException("No Cid Available"));
			}
			if (string.IsNullOrEmpty(_requester.Pid))
			{
				return Promise<RealmView>.Successful(null);
			}

			return GetRealms().Map(all => { return all.Find(v => v.Pid == _requester.Pid); });
		}

		private List<RealmView> ProcessProjects(List<ProjectViewDTO> projects)
		{
			var map = projects.Select(p => new RealmView
			{
				Cid = _requester.Cid,
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
			var pid = game?.Pid ?? _requester.Pid ?? _requester.AccessToken?.Pid;
			return GetRealms(pid);
		}
		public Promise<List<RealmView>> GetRealms(string pid)
		{
			//Get fake Realms Data
			RealmView[] fakeRealms =
			{
				new RealmView()
				{
					Pid = "fakePid_prod",
					Cid = "fakeCid_0000",
					ProjectName = "fakeProjectName",
					Archived = false,
					Parent = null,
					Depth = 0,
				},

				new RealmView()
				{
					Pid = "fakePid_staging",
					Cid = "fakeCid_0000",
					ProjectName = "fakeProjectName",
					Archived = false,
					Parent = null,
					Depth = 0,
				},

				new RealmView()
				{
					Pid = "fakePid_dev",
					Cid = "fakeCid_0000",
					ProjectName = "fakeProjectName",
					Archived = false,
					Parent = null,
					Depth = 0,
				},

			};

			fakeRealms[1].Parent = fakeRealms[0];
			fakeRealms[2].Parent = fakeRealms[1];

			return Promise<List<RealmView>>.Successful(fakeRealms.ToList());
			//return Promise<List<RealmView>>.Successful(new List<RealmView>());

		}
	}

}
