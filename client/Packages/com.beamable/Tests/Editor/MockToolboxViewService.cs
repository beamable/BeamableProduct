using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.Realms;
using Beamable.Editor.Modules.Account;
using System;
using Beamable.Common;

namespace Beamable.Editor.Tests
{
	public class MockToolboxViewService : IToolboxViewService
	{
		public static MockToolboxViewService Instance;
		public List<RealmView> Realms { get; private set; }

		public RealmView CurrentRealm { get; private set; }

		public EditorUser CurrentUser { get; private set; }

		public IWidgetSource WidgetSource { get; private set; }

		public ToolboxQuery Query { get; private set; }

		public string FilterText { get; private set; }

		private List<AnnouncementModelBase> _announcements = new List<AnnouncementModelBase>();
		public IEnumerable<AnnouncementModelBase> Announcements => throw new NotImplementedException();

		public event Action<List<RealmView>> OnAvailableRealmsChanged;
		public event Action<RealmView> OnRealmChanged;
		public event Action<IWidgetSource> OnWidgetSourceChanged;
		public event Action OnQueryChanged;
		public event Action<EditorUser> OnUserChanged;
		public event Action<IEnumerable<AnnouncementModelBase>> OnAnnouncementsChanged;

		public MockToolboxViewService()
		{
			WidgetSource = new EmptyWidgetSource();

			UseDefaultWidgetSource();
		}

		public void Destroy()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<Widget> GetFilteredWidgets()
		{
			throw new NotImplementedException();
		}

		public void Initialize()
		{
			RefreshAvailableRealms();

			var api = BeamEditorContext.Default;
			api.OnRealmChange += API_OnRealmChanged;
			CurrentUser = api.CurrentUser;
			OnUserChanged?.Invoke(CurrentUser);
			CurrentRealm = api.CurrentRealm;
			OnRealmChanged?.Invoke(CurrentRealm);
		}

		private void API_OnRealmChanged(RealmView realm)
		{
			CurrentRealm = realm;
			OnRealmChanged?.Invoke(realm);
		}

		public bool IsSpecificAnnouncementCurrentlyDisplaying(Type type)
		{
			throw new NotImplementedException();
		}

		public Promise<List<RealmView>> RefreshAvailableRealms()
		{
			var api = BeamEditorContext.Default;
			return api.ServiceScope.GetService<RealmsService>().GetRealms().Then(realms =>
			{
				Realms = realms;
				OnAvailableRealmsChanged?.Invoke(realms);
			}).Error(err => api.Logout());
		}

		public void AddAnnouncement(AnnouncementModelBase announcementModel)
		{
			_announcements.Add(announcementModel);
			OnAnnouncementsChanged?.Invoke(Announcements);
		}

		public void RemoveAnnouncement(AnnouncementModelBase announcementModel)
		{
			_announcements.Remove(announcementModel);
			OnAnnouncementsChanged?.Invoke(Announcements);
		}

		public void SetOrientationSupport(WidgetOrientationSupport orientation, bool shouldHaveOrientation)
		{
			throw new NotImplementedException();
		}

		public void SetQuery(string filter)
		{
			var oldFilterText = FilterText;
			var nextQuery = ToolboxQuery.Parse(filter);
			Query = nextQuery;
			FilterText = filter;
			if (!string.Equals(oldFilterText, FilterText))
			{
				OnQueryChanged?.Invoke();
			}
		}

		public void SetQuery(ToolboxQuery query)
		{
			Debug.Log("In Fake ToolboxViewService");
			var oldFilterText = FilterText;
			Query = query;
			FilterText = query.ToString();

			if (!string.Equals(oldFilterText, FilterText))
			{
				OnQueryChanged?.Invoke();
			}
		}

		public void SetQueryTag(WidgetTags tags, bool shouldHaveTag)
		{
			var hasOrientation = (Query?.HasTagConstraint ?? false) &&
								 Query.FilterIncludes(tags);
			var nextQuery = new ToolboxQuery(Query);

			if (hasOrientation && !shouldHaveTag)
			{
				nextQuery.TagConstraint = nextQuery.TagConstraint & ~tags;
				nextQuery.HasTagConstraint = nextQuery.TagConstraint > 0;
			}
			else if (!hasOrientation && shouldHaveTag)
			{
				nextQuery.TagConstraint |= tags;
				nextQuery.HasTagConstraint = true;
			}
			SetQuery(nextQuery);
		}

		public void UseDefaultWidgetSource()
		{
			//DO NOT USE THIS
			//WidgetSource = AssetDatabase.LoadAssetAtPath<WidgetSource>($"BASE_PATH/Models/toolboxData.asset");
			WidgetSource = new ToolboxMockData();
			OnWidgetSourceChanged?.Invoke(WidgetSource);

			//DEBUG LOG
			/*for (var i = 0; i < WidgetSource.Count; i++)
			{
				var widget = WidgetSource.Get(i);
				Debug.Log(widget.Name);
			}*/

			//come up with different wu=idget source
			//make new mock class widget source
		}

		public int DoSomething(int num)
		{
			return num;
		}
	}
}

