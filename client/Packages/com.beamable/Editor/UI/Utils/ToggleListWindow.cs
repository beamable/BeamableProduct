using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI2.Utils
{
    public class ToggleListWindow : EditorWindow, IDelayedActionWindow
    {
        private List<Action> _actions = new();
        private SearchData contentSearchData;
        private Action<string, bool> _onChangeItem;
        private Dictionary<string, bool> _items;
        private Vector2 _scrollPosition;

        public Action<string, bool> OnChangeItem
        {
	        get => _onChangeItem;
	        set => _onChangeItem = value;
        }

        public Dictionary<string, bool> Items
        {
	        get => _items;
	        set => _items = value;
        }

        public ToggleListWindow()
        {
            contentSearchData = new SearchData() { onEndCheck = Repaint };
        }
        
        public static void Show(Rect buttonRect, Vector2 windowSize, Dictionary<string, bool> items, Action<string, bool> onChangeItem = null)
        {
            var window = CreateInstance<ToggleListWindow>();
            window.titleContent = new GUIContent("Search Options");
            window.Items = new Dictionary<string, bool>(items);
            window.OnChangeItem = onChangeItem;
            window.ShowAsDropDown(buttonRect.GetRectProperPosition(), windowSize);
            window.Focus();
        }

        
        

        void OnGUI()
        {
            this.DrawSearchBar(contentSearchData);

            // Scrollable list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Draw filtered items with toggles
            var filtered = GetFilteredItems(contentSearchData.searchText, _items.Keys.ToList());
            foreach (var item in filtered)
            {
                bool state = _items[item];
                state = EditorGUILayout.ToggleLeft(item, state);
                if (state != _items[item])
                {
	                _items[item] = state;
                    GUI.changed = true;
                    _onChangeItem?.Invoke(item, state);
                }
            }
            
            EditorGUILayout.EndScrollView();

            RunDelayedActions();
            
            // Close on click outside
            if (Event.current.type == EventType.MouseDown && !position.Contains(Event.current.mousePosition))
                Close();
        }

        private void RunDelayedActions()
        {
            var copy = _actions.ToList();
            _actions.Clear();
            
            foreach (var act in copy)
            {
                act?.Invoke();
            }
        }

        private static IEnumerable<string> GetFilteredItems(string search, IEnumerable<string> menuItems)
        {
            if (string.IsNullOrEmpty(search))
                return menuItems;
            
            return menuItems.Where(x => x.ToLower().Contains(search.ToLower())).ToList();
        }

        public void AddDelayedAction(Action act)
        {
            _actions.Add(act);
        }
    }
}
