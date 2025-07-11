using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.Accounts;
using Beamable.Editor.Content;
using Beamable.Editor.Library;
using Beamable.Editor.Microservice.UI2;
using Beamable.Editor.UI.ContentWindow;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace Beamable.Editor.UI
{
	/// <summary>
	///
	/// </summary>
	/// <typeparam name="TWindow"></typeparam>
	public abstract class BeamEditorWindow<TWindow> 
		: EditorWindow, ISerializationCallbackReceiver 
		, IDelayedActionWindow
		where TWindow : BeamEditorWindow<TWindow>, new()
	{
		private List<Action> _actions = new List<Action>();
		
		/// <summary>
		/// The default <see cref="BeamEditorWindowInitConfig{TWindow}"/> struct that is used when initializing this window via <see cref="GetFullyInitializedWindow"/>.
		/// </summary>
		protected static BeamEditorWindowInitConfig WindowDefaultConfig;

		/// <summary>
		/// Static function to be set on any sub-type's initialization. It's used to add constraints to the generic-constrained version of this type's <see cref="BeamEditor.DelayedInitializationCall"/>.
		/// </summary>
		protected static BeamEditorInitializedDelayClause CustomDelayClause;

		/// <summary>
		/// Whether or not the current window is instantiated. TODO: once we no longer support Unity 2018, change this into HasOpenInstance<TWindow>.
		/// </summary>
		public static bool IsInstantiated { get; protected set; }

		/// <summary>
		/// The <see cref="BeamEditorWindowInitConfig{TWindow}"/> struct that was used to initialize this window.
		/// </summary>
		public BeamEditorWindowInitConfig InitializedConfig;

		/// <summary>
		/// A promise that gets completed when the window finishes running it's <see cref="OnEnable"/> method. Await this to guarantee the <see cref="BeamEditorWindow{TWindow}"/> instance is completely initialized.
		/// TODO: We shouldn't really need this. We have it now as a lot of the Model initialization happens during Window initialization. This model is then accessed, through the window instance, in certain parts
		/// TODO: of the code-base. This is a problem in certain Unity-event flows causing the model to not be initialized due to the window that manages it not being initialized when its accessed.
		/// TODO: In order to keep this current flow working and not have to refactor everything at once, we have this promise and <see cref="GetFullyInitializedWindow()"/>.
		/// TODO: This allows us to keep the current flow, but enables us to incrementally move away from this pattern of initializing System/Model instances inside the window initialization.
		/// </summary>
		public Promise FullyInitializedWindowPromise;

		/// <summary>
		/// Reference to the current <see cref="BeamEditorContext"/> that is feeding this window with data.
		/// </summary>
		public BeamEditorContext ActiveContext;

		/// <summary>
		/// Computed reference to the current dependency scope for the <see cref="ActiveContext"/>
		/// </summary>
		public IDependencyProviderScope Scope => ActiveContext?.ServiceScope;

		/// <summary>
		/// Creates, initializes then waits for the window instance to be completely ready for use before returning that instance.
		/// See <see cref="FullyInitializedWindowPromise"/> to understand why this is necessary.
		/// </summary>
		public static async Task<TWindow> GetFullyInitializedWindow()
		{
			await InitBeamEditorWindow(WindowDefaultConfig);
			var contentMangerWindow = GetWindow<TWindow>();
			IsInstantiated = true;
			await contentMangerWindow.FullyInitializedWindowPromise;
			return contentMangerWindow;
		}

		/// <summary>
		/// Creates, initializes then waits for the window instance to be completely ready for use before returning that instance.
		/// See <see cref="FullyInitializedWindowPromise"/> to understand why this is necessary.
		/// </summary>
		public static async Task<TWindow> GetFullyInitializedWindow(BeamEditorWindowInitConfig config)
		{
			await InitBeamEditorWindow(config);
			var contentMangerWindow = GetWindow<TWindow>();
			await contentMangerWindow.FullyInitializedWindowPromise;
			return contentMangerWindow;
		}

		/// <summary>
		/// Function that initializes a window based on it's given <paramref name="config"/>.
		/// </summary>
		/// <param name="config">A set of configuration parameters informing us how to initialize the window.</param>
		protected static Task InitBeamEditorWindow(BeamEditorWindowInitConfig config)
		{
			var requireLoggedUser = config.RequireLoggedUser;
			if (requireLoggedUser)
			{
				// var loginPromise = LoginWindow.CheckLogin();
			}

			// If it was already instantiated -- destroy the existing window.
			var dockPreference = string.IsNullOrEmpty(config.DockPreferenceTypeName) ? null : Type.GetType(config.DockPreferenceTypeName);
			var title = config.Title;
			var focus = config.FocusOnShow;

			var _instance = GetWindow<TWindow>(title, focus, dockPreference);
			_instance.InitializedConfig = config;

			if (_instance.FullyInitializedWindowPromise == null || !_instance.FullyInitializedWindowPromise.IsCompleted)
				_instance.FullyInitializedWindowPromise = new Promise();

			_instance.Show(true);
			return Task.CompletedTask;
		}

		public virtual void OnEnable()
		{
			BeamEditor.DelayedInitializationCall(() =>
			{
				BuildWithDefaultContext();

				// If the GetWindow call in InitBeamEditorWindow, is called and BeamEditor.IsInitialized is true, this section of code will run as part of that GetWindow call.
				// This means that the subsequent lines of InitBeamEditorWindow where we initialize the promise wouldn't have run by the time we go through this and the FullyInitializedWindowPromise would be null.
				// In that case, we can set the FullyInitializedWindowPromise as a successful promise by default.
				// This is also true of cases in cases where OnEnable runs outside of the InitBeamEditorWindow flow, such as DomainReloads after recompiles or play-mode entering.
				if (FullyInitializedWindowPromise == null)
					FullyInitializedWindowPromise = Promise.Success;

				// Otherwise, if we did in fact delay the function call due to BeamEditor.IsInitialized being false, the promise will be set by InitBeamEditorWindow's lines after GetWindow.
				// So the promise here won't be null and we can complete it.
				else
					FullyInitializedWindowPromise.CompleteSuccess();
			}, true, CustomDelayClause);
		}

		public async Promise Load()
		{
			// TODO: render for loading...
			await OnLoad();

			OnRender();
		}

		public virtual bool ShowLoading => false;

		public virtual Promise OnLoad()
		{
			return Promise.Success;
		}

		public virtual void OnRender()
		{

		}

		public virtual void OnDestroy() => IsInstantiated = false;

		public virtual void OnBeforeSerialize() { }

		/// <summary>
		/// This base implementation guarantees that the <see cref="Instance"/> field will always point to the deserialized version.
		/// Without this, the ordering of callbacks can cause two instances of a window to exist and both instances to be incorrectly initialized.
		/// </summary>
		public virtual void OnAfterDeserialize() { }

		/// <summary>
		/// Implement this instead of <see cref="OnEnable"/>. The <see cref="OnEnable"/> implementation for <see cref="BeamEditorWindow{T}"/> contains the guard described in
		/// <see cref="BeamEditor.DelayedInitializationCall"/>.
		///
		/// The default implementation here guarantees this window has an <see cref="ActiveContext"/> to fill out it's data.
		/// </summary>
		public void BuildWithDefaultContext() => BuildWithContext(BeamEditorContext.Default);

		/// <summary>
		/// Overload of <see cref="BuildWithContext(BeamEditorContext)"/> that uses the given <paramref name="code"/> and <see cref="BeamEditorContext.ForEditorUser(string)"/> to find the
		/// <see cref="BeamEditorContext"/> instance to rebuild with.
		/// </summary>
		public void BuildWithContext(string code) => BuildWithContext(BeamEditorContext.ForEditorUser(code));


		/// <summary>
		/// Overload of <see cref="BuildWithContext(BeamEditorContext)"/> that uses the given <paramref name="index"/> and <see cref="BeamEditorContext.ForEditorUser(int)"/> to find the
		/// <see cref="BeamEditorContext"/> instance to rebuild with.
		/// </summary>
		public void BuildWithContext(int index) => BuildWithContext(BeamEditorContext.ForEditorUser(index));


		protected virtual async Promise BuildSetup()
		{
			await Load();
			await BuildAsync();
		}
		
		/// <summary>
		/// Rebuilds the window's entire content.
		/// If it cares about whether or not the given <paramref name="context"/> is/isn't authenticated, it'll invoke <see cref="Build"/>
		/// If the given <paramref name="context"/> is null, it will rebuild with the current <see cref="ActiveContext"/>.
		/// </summary>
		/// <param name="context">The <see cref="BeamEditorContext"/> to rebuild this window with. When null, re-uses the existing context.</param>
		public void BuildWithContext(BeamEditorContext context = null)
		{
			ActiveContext = context ?? ActiveContext;

			var dispatcher = ActiveContext.ServiceScope.GetService<BeamableDispatcher>();
			dispatcher.Schedule(async () =>
			{
				try
				{
					if (InitializedConfig.RequireLoggedUser)
					{
						if (ActiveContext.IsAuthenticated)
						{
							await BuildSetup();
						}
					}
					else
					{
						await BuildSetup();
					}
				}
				catch (Exception ex)
				{
					Debug.LogError(ex?.InnerException ?? ex);
					Debug.LogError("Cannot load window");
				}
			});
		}

		protected abstract void Build();

		protected virtual Promise BuildAsync()
		{
			Build();
			return Promise.Success;
		}

		private void OnGUI()
		{
			BeamGUI.LoadAllIcons();
			BeamGUI.CreateButtonStyles();
			
			titleContent = new GUIContent(InitializedConfig.Title, BeamGUI.iconBeamableSmall);
			
			var ctx = ActiveContext;
			if (ctx == null)
			{
				DrawNoContextGui();
				return;
			}
			
			if (!ctx.InitializePromise.IsCompleted)
			{
				DrawWaitingForContextGui();
				return;
			}

			if (InitializedConfig.RequireLoggedUser && !ctx.IsAuthenticated)
			{
				DrawNotLoggedInGui();
				return;
			}
			
			if (InitializedConfig.RequirePid && ctx.BeamCli?.Pid == null)
			{
				DrawNoRealmGui();
				return;
			}
			
			
			DrawGUI();
		}

		protected virtual void DrawGUI()
		{
			
		}

		protected void RunDelayedActions()
		{
			// copy the actions into a separate list, so if there is an error, at least they clear.
			var copy = _actions.ToList();
			_actions.Clear();
			
			// perform delayed actions
			foreach (var act in copy)
			{
				act?.Invoke();
			}


		}


		public void AddDelayedAction(Action act)
		{
			_actions.Add(act);
		}
		
		protected void DrawNoContextGui()
		{
			DrawBlockLoading("Resolving Beamable...");
		}
		
		protected void DrawWaitingForContextGui()
		{
			DrawBlockLoading("Loading Beamable...");
		}

		protected void DrawNoRealmGui()
		{
			BeamGUI.DrawHeaderSection(this, ActiveContext, () => { }, () => { }, () => { }, () => { });
			EditorGUILayout.Space(12, false);
			EditorGUILayout.LabelField("There is no Realm selected. Please select a Realm. ");
		}

		protected virtual void DrawBlockLoading(string message)
		{
			BeamGUI.DrawHeaderSection(this, ActiveContext, () => { }, () => { }, () => { }, () => { });
			EditorGUILayout.Space(12, false);
			BeamGUI.LoadingSpinnerWithState(message);
		}


		protected virtual void DrawNotLoggedInGui()
		{
			EditorGUILayout.BeginVertical(new GUIStyle
			{
				margin = new RectOffset(12, 12, 12, 12)
			});

			BeamGUI.DrawLogoBanner();

			EditorGUILayout.Space(12, false);
			
			{ // draw a notice
				EditorGUILayout.HelpBox(
					$"Welcome to Beamable! You must Log-in before you can use the {titleContent.text} window. " +
					$"If you don't have an account, use the Log-in button to create an account. ",
					MessageType.Info);
			}
			
			EditorGUILayout.Space(12, false);

			{ // draw button to open
				if (BeamGUI.PrimaryButton(new GUIContent("Log in")))
				{

					Action onQuit = BeamLibraryWindow.Init;
					string onQuitMessage = "Open Library";
					switch (this)
					{
						case UsamWindow:
							onQuit = UsamWindow.Init;
							onQuitMessage = "Open Services";
							break;
						case ContentWindow.ContentWindow:
							onQuit = () => _ = ContentWindow.ContentWindow.Init();
							onQuitMessage = "Open Content";
							break;
					}
					AccountWindow.Init(this, onQuitMessage, onQuit);
					// var _ = LoginWindow.CheckLogin();
				}
			}

			EditorGUILayout.EndVertical();
			
		}

	}

	/// <summary>
	/// Data that configures how a specific type of <see cref="BeamEditorWindow{TWindow}"/> should be initialized.
	/// </summary>
	[Serializable]
	public struct BeamEditorWindowInitConfig
	{
		public string Title;
		public string DockPreferenceTypeName;
		public bool FocusOnShow;
		public bool RequireLoggedUser;
		public bool RequirePid;
	}
}
