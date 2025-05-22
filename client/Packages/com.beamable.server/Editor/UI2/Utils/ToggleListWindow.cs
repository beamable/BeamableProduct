using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI2.Utils
{
    public class ToggleListWindow : EditorWindow, IDelayedActionWindow
    {
        private List<Action> _actions = new();
        private SearchData contentSearchData;
        private static Dictionary<string, bool> _toggleStates = new();
        private static List<string> _items;
        private static Vector2 _scrollPosition;

        public ToggleListWindow()
        {
            contentSearchData = new SearchData() { onEndCheck = Repaint };
        }
        
        public static void Show(Rect buttonRect, Vector2 windowSize, List<string> items, Dictionary<string, bool> states)
        {
            _items = items;
            _toggleStates = states;
            
            var window = CreateInstance<ToggleListWindow>();
            window.titleContent = new GUIContent("Search Options");
            window.ShowAsDropDown(buttonRect.GetRectProperPosition(), windowSize);
            window.Focus();
        }

        
        

        void OnGUI()
        {
            this.DrawSearchBar(contentSearchData);

            // Scrollable list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Draw filtered items with toggles
            var filtered = GetFilteredItems(contentSearchData.searchText, _items);
            foreach (var item in filtered)
            {
                bool state = _toggleStates[item];
                state = EditorGUILayout.ToggleLeft(item, state);
                if (state != _toggleStates[item])
                {
                    _toggleStates[item] = state;
                    GUI.changed = true;
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

        private static List<string> GetFilteredItems(string search, List<string> menuItems)
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
