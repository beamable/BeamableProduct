using Beamable.Common;
using Beamable.Editor.Modules.Account;
using Beamable.Editor.Realms;
using Beamable.Editor.Toolbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using static Beamable.Common.Constants.Features.Toolbox;

namespace Beamable.Editor.Toolbox.Models
{
	public class ToolboxModel
	{
		public event Action<List<RealmView>> OnAvailableRealmsChanged;
		public event Action<RealmView> OnRealmChanged;
		public event Action<IWidgetSource> OnWidgetSourceChanged;
		public event Action OnQueryChanged;
		public event Action<EditorUser> OnUserChanged;
		public event Action<IEnumerable<AnnouncementModelBase>> OnAnnouncementsChanged;

		public List<RealmView> Realms { get; private set; }
		public RealmView CurrentRealm { get; private set; }
		public EditorUser CurrentUser { get; private set; }
		public IWidgetSource WidgetSource { get; private set; }
		public ToolboxQuery Query { get; private set; }
		public string FilterText { get; private set; }

		private List<AnnouncementModelBase> _announcements = new List<AnnouncementModelBase>();
		public IEnumerable<AnnouncementModelBase> Announcements => _announcements;


		public ToolboxModel()
		{
			WidgetSource = new EmptyWidgetSource();
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

		public bool IsSpecificAnnouncementCurrentlyDisplaying(Type type)
		{
			return Announcements.Any(announcement => announcement.GetType() == type);
		}

		public void UseDefaultWidgetSource()
		{
			WidgetSource = AssetDatabase.LoadAssetAtPath<WidgetSource>($"{BASE_PATH}/Models/toolboxData.asset");
			OnWidgetSourceChanged?.Invoke(WidgetSource);
		}

		// TODO: Add a method that creates a widgetSource derived from networking.

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
			var oldFilterText = FilterText;
			Query = query;
			FilterText = query.ToString();

			if (!string.Equals(oldFilterText, FilterText))
			{
				OnQueryChanged?.Invoke();
			}
		}

		public IEnumerable<Widget> GetFilteredWidgets()
		{
			for (var i = 0; i < WidgetSource.Count; i++)
			{
				var widget = WidgetSource.Get(i);
				if (Query != null && !Query.Accepts(widget)) continue;

				yield return widget;
			}
		}

		public Promise<List<RealmView>> RefreshAvailableRealms()
		{
			return EditorAPI.Instance.FlatMap(api => api.RealmService.GetRealms()).Then(realms =>
			{
				Realms = realms;
				OnAvailableRealmsChanged?.Invoke(realms);
			});
		}



		public void Initialize()
		{
			RefreshAvailableRealms();

			EditorAPI.Instance.Then(api =>
			{
				api.OnRealmChange += API_OnRealmChanged;
				CurrentUser = api.User;
				OnUserChanged?.Invoke(CurrentUser);
				CurrentRealm = api.Realm;
				OnRealmChanged?.Invoke(CurrentRealm);
			});
		}

		private void API_OnRealmChanged(RealmView realm)
		{
			CurrentRealm = realm;
			OnRealmChanged?.Invoke(realm);
		}

		public void Destroy()
		{
			OnAvailableRealmsChanged = null;
			EditorAPI.Instance.Then(api => { api.OnRealmChange -= API_OnRealmChanged; });
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

		public void SetOrientationSupport(WidgetOrientationSupport orientation, bool shouldHaveOrientation)
		{
			var hasOrientation = (Query?.HasOrientationConstraint ?? false) &&
								 Query.FilterIncludes(orientation);
			var nextQuery = new ToolboxQuery(Query);

			if (hasOrientation && !shouldHaveOrientation)
			{
				nextQuery.OrientationConstraint = nextQuery.OrientationConstraint & ~orientation;
				nextQuery.HasOrientationConstraint = nextQuery.OrientationConstraint > 0;
			}
			else if (!hasOrientation && shouldHaveOrientation)
			{
				nextQuery.OrientationConstraint |= orientation;
				nextQuery.HasOrientationConstraint = true;
			}
			SetQuery(nextQuery);
		}
	}
}
