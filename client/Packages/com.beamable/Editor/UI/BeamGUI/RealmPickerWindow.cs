using Beamable.Common;
using Beamable.Common.Api.Realms;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		public static void LayoutRealmDropdown(EditorWindow rootWindow, BeamEditorContext ctx)
		{
			var realm = ctx.CurrentRealm;
			BeamGUI.LayoutDropDown(rootWindow, new GUIContent(realm.DisplayName), GUILayout.MaxWidth(80),
			                       () =>
			                       {
				                       var wnd = ScriptableObject.CreateInstance<RealmPickerWindow>();
				                       wnd.ctx = ctx;
				                       return wnd;
			                       }, out var dropDownBounds, popupOnLeft: true);
			var bannerBounds = new Rect(dropDownBounds.x, dropDownBounds.y, 4, dropDownBounds.height);
			if (realm.IsProduction)
			{
				EditorGUI.DrawRect(bannerBounds, new Color(1, 0, 0, .8f));

			} else if (realm.IsStaging)
			{
				EditorGUI.DrawRect(bannerBounds, new Color(1, .6f, 0, .8f));

			}
			
		}
	}
	
	public class RealmPickerWindow : EditorWindow
	{
		public static bool isOpen;
		
		public BeamEditorContext ctx;

		public Vector2 scrollPosition;
		public string searchFilter;

		public List<RealmView> view = new List<RealmView>();
		private Promise _refreshPromise;

		private void OnDestroy()
		{
			
		}

		private void OnGUI()
		{
			isOpen = true;
			if (ctx == null)
			{
				Close();
				return;
			}

			bool clickedRefresh = false;
			bool clickedRealm = false;
			maxSize = new Vector2(200, 600);
			minSize = new Vector2(200, 300);

			if (_refreshPromise == null)
			{ // construct realm view
				view.Clear();
				for (var i = 0; i < ctx.EditorAccount.CustomerRealms.Count; i++)
				{
					var realm = ctx.EditorAccount.CustomerRealms[i];
					if (realm.Archived) continue;

					if (!string.IsNullOrEmpty(searchFilter))
					{
						if (!realm.DisplayName.Contains(searchFilter))
						{
							continue;
						}
					}
					
					var isPartOfCurrentGame = realm.GamePid == ctx.EditorAccount.gamePid;
					if (!isPartOfCurrentGame) continue;
					view.Add(realm);
				}
				view.Sort((a, b) => b.Depth.CompareTo(a.Depth));
			}
			
			{ // draw the search bar row
				EditorGUILayout.BeginHorizontal(new GUIStyle
				{
					padding = new RectOffset(4, 2, 4,4 )
				});
				searchFilter = EditorGUILayout.TextField(searchFilter, new GUIStyle(EditorStyles.toolbarSearchField)
				{
					margin = new RectOffset(0, 0, 0, 0)
				});
				
				BeamGUI.ShowDisabled(_refreshPromise == null, () =>
				{
					clickedRefresh = GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), new GUIStyle(EditorStyles.iconButton)
					{
						padding = new RectOffset(2, 2, 2, 2)
					}, GUILayout.Width(15));
					
				});
				
				EditorGUILayout.EndHorizontal();
			}

			{ // draw all the realms!
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				EditorGUILayout.BeginVertical();
				
				BeamGUI.ShowDisabled(_refreshPromise == null, () =>
				{
					clickedRealm = DrawRealms(out var realm);
					if (clickedRealm)
					{
						var p = ctx.SwitchRealm(realm);
						p.Then(_ => Close());
						_refreshPromise = p;
					}
				});
				
				if (_refreshPromise != null)
				{
					if (_refreshPromise.IsCompleted || _refreshPromise.IsFailed)
					{
						_refreshPromise = null;
					}
				}
				
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();

			}
			
			
			if (clickedRefresh)
			{
				_refreshPromise = ctx.EditorAccount.UpdateRealms(ctx.Requester);
			}
		}

		void DrawLoading()
		{
			EditorGUILayout.LabelField("loading...");
			if (_refreshPromise.IsCompleted || _refreshPromise.IsFailed)
			{
				_refreshPromise = null;
			}
		}
		
		bool DrawRealms(out RealmView selectedRealm)
		{
			selectedRealm = null;
			bool anyClick = false;
			for (var i = 0; i < view.Count; i++)
			{
				var realm = view[i];
				var isSelected = realm.Pid == ctx.EditorAccount.realmPid;
				
				int height = 25;
				var clicked = GUILayout.Button(GUIContent.none, new GUIStyle(EditorStyles.miniButton)
				{
					alignment = TextAnchor.MiddleLeft,
					fixedHeight = height,
					margin = new RectOffset(1, 1, 0, 1)
				}, GUILayout.Height(height));

				var rect = GUILayoutUtility.GetLastRect();
				var isHover = rect.Contains(Event.current.mousePosition);

				var bannerRect = new Rect(rect.x, rect.y, 4, rect.height);
				var labelRect = new Rect(bannerRect.x + 12, bannerRect.y, rect.width - 21, rect.height);
				var bannerColor = new Color(1, 1, 1, .4f);
					
					
				if (realm.IsProduction)
				{
					EditorGUI.DrawRect(rect, new Color(.7f, 0, 0, isHover ? .6f : .4f));
					bannerColor = new Color(1, 0, 0, .7f);
				}

				if (realm.IsStaging)
				{
					// EditorGUI.DrawRect(rect, new Color(1, 156/255f, 0, isHover ? .8f : .5f));
					EditorGUI.DrawRect(rect, new Color(.7f, .7f, 0, isHover ? .6f : .4f));
					bannerColor = new Color(1, .6f, 0, .7f);
				}
					
				EditorGUI.DrawRect(bannerRect, bannerColor);

				EditorGUI.LabelField(labelRect, realm.DisplayName, new GUIStyle(EditorStyles.label)
				{
					alignment = TextAnchor.MiddleLeft,
					normal = new GUIStyleState
					{
						textColor = Color.white
					}
				});

				if (isSelected)
				{
					EditorGUI.DrawRect(rect, new Color(0, 0, 0, .2f));
				}

				if (clicked)
				{
					selectedRealm = realm;
					anyClick = true;
				}

			}
			return anyClick;


		}
	}
}
