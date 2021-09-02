using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using UnityEngine.UI;

namespace Beamable.Installer.Editor
{

	public class VerisonNumberPrompt : EditorWindow
	{
		private string VersionNumber = "";
		private Action<string> _callback;

		/// <summary>
		/// Create a Centered screen-relative rectangle, given a parent editor window
		/// </summary>
		/// <param name="window"></param>
		public static Rect GetCenteredScreenRectForWindow(EditorWindow window, Vector2 size)
		{
			var pt = window.position.center;

			var halfSize = size * .5f;
			return new Rect(pt.x - halfSize.x, pt.y - halfSize.y, size.x, size.y);
		}

		public static void GetNumber(EditorWindow source, Action<string> cb)
		{
			var window = EditorWindow.CreateInstance<VerisonNumberPrompt>();
			window._callback = cb;
			window.ShowPopup();
			window.position = GetCenteredScreenRectForWindow(source, new Vector2(300, 150));

		}

		void OnGUI()
		{
			VersionNumber = EditorGUILayout.TextField("Beamable Version", VersionNumber);

			var wasEnabled = GUI.enabled;
			GUI.enabled = !string.IsNullOrEmpty(VersionNumber);
			if (GUILayout.Button("Install"))
			{
				this.Close();
				_callback?.Invoke(VersionNumber);
			}

			GUI.enabled = wasEnabled;

			if (GUILayout.Button("Cancel"))
			{
				this.Close();
				_callback?.Invoke(null);
			}
		}

	}

	[CustomEditor(typeof(BeamableInstallerReadme))]
	[InitializeOnLoad]
	public class BeamableInstallerReadmeEditor : UnityEditor.Editor
	{

//		static string kShowedReadmeSessionStateName = "Beamable.Installer.ReadmeEditor.showedReadme";

		static float kSpace = 16f;
		private static bool hasPackage;

		static BeamableInstallerReadmeEditor()
		{
			EditorApplication.delayCall += SelectReadmeAutomatically;
		}

		static void SelectReadmeAutomatically()
		{
			BeamableInstaller.HasBeamableInstalled(installed =>
			{
				hasPackage = installed;
				if (installed) return;

//				if (!EditorPrefs.GetBool(kShowedReadmeSessionStateName, false))
				{
					var readme = SelectReadme();
//					EditorPrefs.SetBool(kShowedReadmeSessionStateName, true);

					if (readme && !readme.loadedLayout)
					{
						readme.loadedLayout = true;
					}
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

				Selection.objects = new UnityEngine.Object[] {readmeObject};

				return (BeamableInstallerReadme) readmeObject;
			}
			else
			{
				return null;
			}
		}

		protected override void OnHeaderGUI()
		{
			var readme = (BeamableInstallerReadme) target;
			Init();

			var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

			GUILayout.BeginHorizontal("In BigTitle");
			{
				GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
				GUILayout.Label(readme.title, TitleStyle);
			}
			GUILayout.EndHorizontal();
		}

		public override void OnInspectorGUI()
		{
			var readme = (BeamableInstallerReadme) target;
			Init();

			foreach (var section in readme.sections)
			{
				if (!hasPackage && section.onlyShowWhenInstalled)
				{
					continue;
				}

				if (hasPackage && section.onlyShowWhenNotInstalled)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(section.heading))
				{
					GUILayout.Label(section.heading, HeadingStyle);
				}

				if (!string.IsNullOrEmpty(section.text))
				{
					GUILayout.Label(section.text, BodyStyle);
				}

				if (section.Action != InstallerActionType.None)
				{
					var buttonStyle = new GUIStyle("button");
					buttonStyle.fontSize = 16;

					var enabled = GUI.enabled;
					GUI.enabled = !BeamableInstaller.IsBusy;
					if (GUILayout.Button(section.ActionText, buttonStyle, GUILayout.Height(30)))
					{
						var source = GUILayoutUtility.GetLastRect();

						Event current = Event.current;
						if (Event.current.button == 0)
						{
							BeamableInstaller.RunAction(section.Action);
						}
						else if (section.Action == InstallerActionType.Install && section.IncludeRightClickOptions && Event.current.button == 1)
						{
							GenericMenu menu = new GenericMenu();

							menu.AddItem(new GUIContent("Install Stable Build"), false, func: BeamableInstaller.InstallStable);
							menu.AddItem(new GUIContent("Install Release Candidate"), false, func: BeamableInstaller.InstallRC);
							menu.AddItem(new GUIContent("Install Nightly Build"), false,  BeamableInstaller.InstallDev);
							menu.AddItem(new GUIContent("Install Specific Version"), false, () =>
							{

								VerisonNumberPrompt.GetNumber(EditorWindow.mouseOverWindow, (version) =>
								{
									if (version != null)
									{
										BeamableInstaller.InstallSpecific(version);
									}
								});

							});
							menu.ShowAsContext();

							current.Use();
						}
					}

					GUI.enabled = enabled;
				}

				if (!string.IsNullOrEmpty(section.linkText))
				{
					if (LinkLabel(new GUIContent(section.linkText)))
					{
						Application.OpenURL(section.url);
					}
				}

				GUILayout.Space(kSpace);
			}
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

		void Init()
		{
			if (m_Initialized)
				return;
			m_BodyStyle = new GUIStyle(EditorStyles.label);
			m_BodyStyle.wordWrap = true;
			m_BodyStyle.fontSize = 14;

			m_TitleStyle = new GUIStyle(m_BodyStyle);
			m_TitleStyle.fontSize = 26;

			m_HeadingStyle = new GUIStyle(m_BodyStyle);
			m_HeadingStyle.fontSize = 18;

			m_LinkStyle = new GUIStyle(m_BodyStyle);
			m_LinkStyle.wordWrap = false;
			// Match selection color which works nicely for both light and dark skins
			m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
			m_LinkStyle.stretchWidth = false;

			m_Initialized = true;
		}

		bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
		{
			var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

			Handles.BeginGUI();
			Handles.color = LinkStyle.normal.textColor;
			Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
			Handles.color = Color.white;
			Handles.EndGUI();

			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

			return GUI.Button(position, label, LinkStyle);
		}
	}

}