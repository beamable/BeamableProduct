using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Beamable.Installer.Editor
{
    [CustomEditor(typeof(BeamableInstallerReadme))]
    [InitializeOnLoad]
    public class BeamableInstallerReadmeEditor : UnityEditor.Editor
    {
        private bool _advancedFoldout;
        private bool _searchFoldout;
        private Vector2 _searchScrollView = Vector2.zero;
        private bool _searchAllVersionsTypes;
        private string _searchField;
        private string _selectedVersion;

        static float kSpace = 16f;

        static BeamableInstallerReadmeEditor()
        {
            EditorApplication.delayCall += SelectReadmeAutomatically;
        }

        static void SelectReadmeAutomatically()
        {
            BeamableInstaller.UpdateInstalledVersionInfo(_ =>
            {
                if (BeamableInstaller.Installed) return;

                var readme = SelectReadme();

                if (readme && !readme.loadedLayout)
                {
                    readme.loadedLayout = true;
                }
            });
        }

        [MenuItem(BeamableInstaller.BeamableMenuPath + "Show Readme", priority = 100)]
        static BeamableInstallerReadme SelectReadme()
        {
            var ids = AssetDatabase.FindAssets($"Readme t:{nameof(BeamableInstallerReadme)}");
            if (ids.Length == 1)
            {
                var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                Selection.objects = new UnityEngine.Object[] { readmeObject };

                return (BeamableInstallerReadme)readmeObject;
            }
            else
            {
                return null;
            }
        }

        protected override void OnHeaderGUI()
        {
            var readme = (BeamableInstallerReadme)target;
            Init();

            var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);
            
            Rect iconRect = default;
            Rect titleRect = default;
            GUILayout.BeginHorizontal("In BigTitle");
            {
                GUILayout.Space(12);

                iconRect = GUILayoutUtility.GetRect(new GUIContent(readme.icon), EditorStyles.label, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));

                GUILayout.BeginVertical();
                titleRect = GUILayoutUtility.GetRect(new GUIContent(readme.title), TitleStyle);
               
                GUILayout.FlexibleSpace();
                GUILayout.Label($"(installer version {readme.semver})", EditorStyles.miniLabel);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            var headerRect = GUILayoutUtility.GetLastRect();

            EditorGUI.DrawPreviewTexture(headerRect, readme.headerBackground, readme.headerMaterial, ScaleMode.ScaleAndCrop);

            iconRect = new Rect(iconRect.x - 12, iconRect.y - 12, iconRect.width + 12, iconRect.height + 12);
            GUI.DrawTextureWithTexCoords(iconRect, readme.icon, new Rect(.2f, .18f, .7f, .72f), true);
            
            GUI.Label(titleRect, readme.title, TitleStyle);
        }

        public override void OnInspectorGUI()
        {
            var readme = (BeamableInstallerReadme)target;
            Init();

            foreach (var section in readme.sections)
            {
                if (!section.ShouldShow())
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(section.heading))
                {
                    GUILayout.Label(section.heading, HeadingStyle);
                    EditorGUILayout.Space();
                }

                if (!string.IsNullOrEmpty(section.text))
                {
                    GUILayout.Label(section.text, BodyStyle);
                    EditorGUILayout.Space();
                }

                var enabled = GUI.enabled;
                GUI.enabled = !BeamableInstaller.IsBusy;
                switch (section.Action)
                {
                    case InstallerActionType.Install:
                        InstallActionGUI(section);
                        break;
                    case InstallerActionType.None:
                        break;
                    default:
                        if (GUILayout.Button(section.GetActionText(), ActionButtonStyle, GUILayout.Height(30)))
                        {
                            if (Event.current.button == 0)
                            {
                                BeamableInstaller.RunAction(section.Action);
                            }
                        }
                        break;
                }

                GUI.enabled = enabled;
                LinkLabel(section);
            }
        }

        private void InstallActionGUI(BeamableInstallerReadme.Section section)
        {
            string actionText = section.GetActionText();
            var beamVersionsCache = BeamableInstaller.BeamVersionsCache;
            if (beamVersionsCache == null)
            {
                return;
            }

            var installSpecificVersion = !string.IsNullOrWhiteSpace(_selectedVersion) && _searchFoldout;
            if (installSpecificVersion)
            {
                actionText += $" {_selectedVersion}";
            }
            var pressed = GUILayout.Button(actionText, ActionButtonStyle, GUILayout.Height(30));
            if (pressed)
            {
                Event current = Event.current;
                switch (current.button)
                {
                    case 0 when installSpecificVersion:
                        BeamableInstaller.InstallSpecific(_selectedVersion);
                        break;
                    case 0:
                        BeamableInstaller.RunAction(section.Action);
                        break;
                    case 1:
                    {
                        var menu = InstallVersionContextMenu();
                        menu.ShowAsContext();
                        current.Use();
                        break;
                    }
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            _searchFoldout = EditorGUILayout.Foldout(_searchFoldout, "Search for specific version");
            if (_searchFoldout)
            {
                EditorGUILayout.Separator();
                _searchField = EditorGUILayout.TextField("Search", _searchField);
                _searchAllVersionsTypes = EditorGUILayout.ToggleLeft(
                    "Show all versions (include release candidates and nightly builds)",
                    _searchAllVersionsTypes);

                var fittingVersions = beamVersionsCache.ToArray();
                if (!_searchAllVersionsTypes)
                {
                    fittingVersions = fittingVersions
                        .Where(v => v.versionType.Equals(BeamableInstaller.VersionType.Stable)).ToArray();
                }

                if (!string.IsNullOrEmpty(_searchField))
                {
                    var versionSplited = _searchField.Split(new[] { ' ' });
                    fittingVersions = fittingVersions.Where(v => versionSplited.All(v.version.Contains))
                        .ToArray();
                }

                EditorGUILayout.Separator();
                if (fittingVersions.Length > 0)
                {
                    _searchScrollView = EditorGUILayout.BeginScrollView(_searchScrollView, false, false,
                        GUILayout.MaxHeight(150.0f));
                    int index = 0;
                    if (!string.IsNullOrWhiteSpace(_selectedVersion))
                    {
                        for (int i = 0; i < fittingVersions.Length; i++)
                        {
                            if (fittingVersions[i].version.Equals(_selectedVersion))
                            {
                                index = i;
                                break;
                            }
                        }
                    }

                    index = GUILayout.SelectionGrid(index,
                        fittingVersions.Select(x => $"{x.version} ( {x.Date.ToLongDateString()} )").ToArray(),
                        1);
                    _selectedVersion = fittingVersions[index].version;
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    _selectedVersion = string.Empty;
                }

                EditorGUILayout.Separator();
            }

            EditorGUILayout.Space();
            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, "Advanced Setup");
            if (_advancedFoldout)
            {
                EditorGUILayout.BeginVertical();

                GUI.enabled = false;
                EditorGUILayout.ToggleLeft("Install com.beamable",
                    true); // forced true on purpose, because otherwise, whats the point of the installer??
                GUI.enabled = true;
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 10 + 25);
                GUILayout.Label(
                    "The basic Beamable package. This must be installed if any other Beamable packages will be installed. This package provides Beamable frictionless authentication, content, game economy, player inventory, and more.",
                    AdvancedStyle);
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                BeamableInstaller.InstallServerPackage =
                    EditorGUILayout.ToggleLeft("Install legacy com.beamable.server for older versions",
                        BeamableInstaller.InstallServerPackage);

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 10 + 25);
                GUILayout.Label(
                    "In older versions of the Beamable SDK (before 3.0.0), Beamable was split into two packages, <i>com.beamale</i> and <i>com.beamable.server</i>. " +
                    "The server package allowed you to create and deploy Microservices " +
                    "and Microstorages for your game. However, in Beamable 3.0.0, the packages were merged into a single package, <i>com.beamable</i>. " +
                    "If you don't install this now, you " +
                    "can install it later from the Beamable Toolbox.",
                    AdvancedStyle);
                GUILayout.EndHorizontal();


                EditorGUILayout.Space();
                BeamableInstaller.InstallDependencies =
                    EditorGUILayout.ToggleLeft("Install Unity packages dependencies",
                        BeamableInstaller.InstallDependencies);

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 10 + 25);
                GUILayout.Label(
                    "TMPro and Addressable Asset packages are used heavily in Beamable. If you don't install this now, you can install it later from the Beamable Toolbox or Unity Packages window.",
                    AdvancedStyle);
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        private static GenericMenu InstallVersionContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            GUIContent BuildVersionItemName(BeamableInstaller.VersionType type)
            {
                string text = "Install latest ";
                switch (type)
                {
                    case BeamableInstaller.VersionType.Stable:
                        text += "Stable build";
                        break;
                    case BeamableInstaller.VersionType.Rc:
                        text += "Release Candidate";
                        break;
                    case BeamableInstaller.VersionType.Nightly:
                        text += "Nightly build";
                        break;
                }

                if (BeamableInstaller.TryGetLatestVersionForType(type, out var version))
                {
                    text += $"- {version}";
                }

                return new GUIContent(text);
            }

            menu.AddItem(BuildVersionItemName(BeamableInstaller.VersionType.Stable), false,
                func: BeamableInstaller.InstallStable);
            menu.AddItem(BuildVersionItemName(BeamableInstaller.VersionType.Rc), false,
                func: BeamableInstaller.InstallRC);
            menu.AddItem(BuildVersionItemName(BeamableInstaller.VersionType.Nightly), false,
                BeamableInstaller.InstallDev);
            return menu;
        }


        bool m_Initialized;

        GUIStyle LinkStyle
        {
            get { return m_LinkStyle; }
        }

        [SerializeField] GUIStyle m_LinkStyle;

        GUIStyle TitleStyle
        {
            get { return m_TitleStyle; }
        }

        [SerializeField] GUIStyle m_TitleStyle;

        GUIStyle HeadingStyle
        {
            get { return m_HeadingStyle; }
        }

        [SerializeField] GUIStyle m_HeadingStyle;

        GUIStyle BodyStyle
        {
            get { return m_BodyStyle; }
        }

        [SerializeField] GUIStyle m_BodyStyle;

        GUIStyle AdvancedStyle
        {
            get { return m_AdvancedStyle; }
        }

        [SerializeField] GUIStyle m_AdvancedStyle;

        [SerializeField] GUIStyle m_ActionButtonStyle;
        
        
        GUIStyle ActionButtonStyle
        {
            get { return m_ActionButtonStyle; }
        }

        void Init()
        {
            if (m_Initialized)
                return;
            m_BodyStyle = new GUIStyle(EditorStyles.label);
            m_BodyStyle.wordWrap = true;
            m_BodyStyle.fontSize = 14;

            m_AdvancedStyle = new GUIStyle(EditorStyles.label);
            m_AdvancedStyle.wordWrap = true;
            m_AdvancedStyle.fontSize = 10;
            m_AdvancedStyle.richText = true;

            m_TitleStyle = new GUIStyle(m_BodyStyle);
            m_TitleStyle.fontSize = 26;
            m_TitleStyle.padding = new RectOffset(0, 0, 6, 0);

            m_HeadingStyle = new GUIStyle(m_BodyStyle);
            m_HeadingStyle.fontSize = 18;

            m_LinkStyle = new GUIStyle(m_BodyStyle);
            m_LinkStyle.wordWrap = false;
            // Match selection color which works nicely for both light and dark skins
            m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
            m_LinkStyle.stretchWidth = false;
            m_ActionButtonStyle  = new GUIStyle("button")
            {
                fontSize = 16
            };

            m_Initialized = true;
        }

        void LinkLabel(BeamableInstallerReadme.Section section, params GUILayoutOption[] options)
        {
            if (string.IsNullOrEmpty(section.linkText))
            {
                GUILayout.Space(kSpace);
                return;
            }

            var label = new GUIContent(section.linkText);
            var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

            Handles.BeginGUI();
            Handles.color = LinkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            if (GUI.Button(position, label, LinkStyle))
            {
                Application.OpenURL(section.url);
            }

            GUILayout.Space(kSpace);
        }
    }
}