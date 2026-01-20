using System;
using Beamable.Common;
using Beamable.Editor.UI;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow : EditorWindow //BeamEditorWindow<AccountWindow>
	{
		private static Action _onQuitAction;
		private static string _onQuitName;
		public BeamCli.BeamCli cli;

		public EditorWindow focusLater;
		
		[NonSerialized]
		public BeamEditorContext context;

		private GUIStyle _errorStyle;
		private static readonly Vector2 LoggedWindowMinSize = new(630, 660);
		private static readonly Vector2 WindowMinSize = new(330, 660);

		[MenuItem(
			Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Constants.Commons.OPEN + " " +
			"Beam Account",
			priority = Constants.MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static void Init()
		{
			var window = GetWindow<AccountWindow>(true, "Beam Account");
			window.minSize = WindowMinSize;
			RefreshCli();
		}

		public static void Init(EditorWindow root, string onQuitName, Action onQuitAction)
		{
			_onQuitName = onQuitName;
			_onQuitAction = onQuitAction;
			var window = GetWindow<AccountWindow>("Beam Account", true, root.GetType());
			window.focusLater = root;
			window.minSize = WindowMinSize;
			RefreshCli();
		}

		static void RefreshCli()
		{
			BeamEditorContext.Default.OnReady.Then(_ =>
			{
				var __ = BeamEditorContext.Default.BeamCli.Refresh();
			});
		}
		
		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			BeamGUI.LoadAllIcons();
			BeamGUI.CreateButtonStyles();
			
			titleContent = new GUIContent("Beam Account", BeamGUI.iconBeamableSmall);
			SetupStyles();

			context = BeamEditorContext.Default;
			
			if (context != null && context.OnReady.IsCompleted)
			{
				cli = context.BeamCli;
			}

			if (cli == null)
			{
				Draw_NoCli();
				return;
			}

			// if (cli.latestConfig)
			{
				// EditorGUILayout.LabelField("Has cid");
			}

			minSize = WindowMinSize;
			if (string.IsNullOrEmpty(cli?.latestAccount?.email) || _loginPromise != null)
			{
				Draw_SignIn();
			}
			else
			{
				if (needsGameSelection)
				{
					Draw_Games();
				}
				else
				{
					minSize = LoggedWindowMinSize;
					Draw_SignedIn();
				}
			}
			
			
			// Draw_SignedIn();
		}

		void Draw_NoCli()
		{
			EditorGUILayout.Space(65, false);
			BeamGUI.LoadingSpinnerWithState("Resolving Beamable...");
		}

		void SetupStyles()
		{
			_headerStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				wordWrap = true
			};
			_titleStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				fontSize = 12,
			};
			_titleStyle.normal.textColor = new Color(1,1,1,.7f);
			
			
			_textboxStyle = new GUIStyle(EditorStyles.textField)
			{
				fontSize = 12,
				fixedHeight = 24,
				alignment = TextAnchor.MiddleLeft
			};
			
			_textboxPlaceholderStyle = new GUIStyle(EditorStyles.textField)
			{
				fontSize = 12,
				fixedHeight = 24,
				alignment = TextAnchor.MiddleLeft,
				normal = new GUIStyleState
				{
					textColor = Color.Lerp(EditorStyles.label.normal.textColor, new Color(1, 1, 1, 0f), .5f),
					background = EditorStyles.textField.normal.background,
					scaledBackgrounds = EditorStyles.textField.normal.scaledBackgrounds
				}
			};
		
			_placeholderStyle = new GUIStyle(EditorStyles.label)
			{
				fontSize = 11,
				// margin = new RectOffset(0,0,4,0),
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(6, 0, 6, 0),
				normal = new GUIStyleState
				{
					textColor = Color.Lerp(EditorStyles.label.normal.textColor, new Color(1, 1, 1, 0f), .5f)
				}
			};

			_errorStyle = new GUIStyle(EditorStyles.miniLabel)
				{ };
			_errorStyle.active.textColor 
				= _errorStyle.normal.textColor 
					= _errorStyle.hover.textColor
						= _errorStyle.focused.textColor
					= new Color(.8f, .2f, .2f, 1f);
			
		}
	}
}
