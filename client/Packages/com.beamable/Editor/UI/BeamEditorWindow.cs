using Beamable.Common;
using Beamable.Editor.Login.UI;
using Beamable.Editor.NoUser;
using Beamable.Editor.Toolbox.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.UIElements;
using UnityEngine;

namespace Beamable.Editor.UI
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TWindow"></typeparam>
	public abstract class BeamEditorWindow<TWindow> : EditorWindow, ISerializationCallbackReceiver where TWindow : BeamEditorWindow<TWindow>, new()
	{
		protected delegate bool DelayClause();

		/// <summary>
		/// The default <see cref="BeamEditorWindowInitConfig{TWindow}"/> struct that is used when initializing this window via <see cref="GetFullyInitializedWindow"/>.  
		/// </summary>
		protected static BeamEditorWindowInitConfig WindowDefaultConfig;

		/// <summary>
		/// Static function to be set on any sub-type's initialization. It's used to add constraints to the generic-constrained version of this type's <see cref="DelayedInitializationCall"/>.
		/// </summary>
		protected static DelayClause CustomDelayClause;

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
		protected static async Task InitBeamEditorWindow(BeamEditorWindowInitConfig config)
		{
			var requireLoggedUser = config.RequireLoggedUser;
			if (requireLoggedUser) await LoginWindow.CheckLogin(typeof(SceneView));

			// If it was already instantiated -- destroy the existing window.
			var dockPreference = string.IsNullOrEmpty(config.DockPreferenceTypeName) ? null : Type.GetType(config.DockPreferenceTypeName);
			var title = config.Title;
			var focus = config.FocusOnShow;

			var _instance = GetWindow<TWindow>(title, focus, dockPreference);
			_instance.InitializedConfig = config;
			
			if(_instance.FullyInitializedWindowPromise == null || !_instance.FullyInitializedWindowPromise.IsCompleted)
				_instance.FullyInitializedWindowPromise = new Promise();
			
			_instance.Show(true);
		}

		/// <summary>
		/// Utility function to delay an initialization call (from within any of Unity's callbacks) until we have initialized our default <see cref="BeamEditorContext"/>.
		/// This must be used to wrap any logic dependent on <see cref="BeamEditorContext"/> or <see cref="BeamEditor"/> systems that is being executed from within a unity event function that initializes things.
		/// These are: OnEnable, OnValidate, OnAfterDeserialize and others like it. Essentially, this guarantees our initialization has finished running, before the given action runs.
		/// <para/>
		/// This is especially used to handle first-import cases and several other edge-cases that happen when these unity event functions are called with our windows opened. In these cases, if we don't delay
		/// our windows cases, the following issues have arisen in the past:
		/// <list type="bullet">
		/// <item><see cref="BeamEditorContext.Default"/> is null; which should be impossible, but happens (probably has to do with DomainReloads)</item>
		/// <item>The window tries to make calls to a partially initialized <see cref="BeamEditorContext"/> and throws.</item>
		/// </list>  
		/// </summary>
		/// <param name="onInitializationFinished">
		/// The that must be scheduled to run from a Unity callback, but is dependent on our initialization being done.
		/// </param>
		/// <param name="forceDelay">
		/// Whether or not we should force the call to be delayed. This is used to guarantee that the callback in <see cref="OnEnable"/> is
		/// called only after the <see cref="InitializedConfig"/> was set during the <see cref="InitBeamEditorWindow"/> flow.
		/// </param>
		public static void DelayedInitializationCall(Action onInitializationFinished, bool forceDelay)
		{
			var hasCustomDelay = CustomDelayClause != null;
			if (!BeamEditor.IsInitialized || forceDelay || (hasCustomDelay && CustomDelayClause()))
			{
				EditorApplication.delayCall += () => DelayedInitializationCall(onInitializationFinished, false);
				return;
			}

			onInitializationFinished?.Invoke();
		}
		
		public virtual void OnEnable()
		{
			DelayedInitializationCall(() =>
			{
				BuildWithDefaultContext();
				
				// If the GetWindow call in InitBeamEditorWindow, is called and BeamEditor.IsInitialized is true, this section of code will run as part of that GetWindow call.
				// This means that the subsequent lines of InitBeamEditorWindow where we initialize the promise wouldn't have run by the time we go through this and the FullyInitializedWindowPromise would be null.
				// In that case, we can set the FullyInitializedWindowPromise as a successful promise by default.
				// This is also true of cases in cases where OnEnable runs outside of the InitBeamEditorWindow flow, such as DomainReloads after recompiles or play-mode entering.
				if(FullyInitializedWindowPromise == null)
					FullyInitializedWindowPromise = Promise.Success;
				
				// Otherwise, if we did in fact delay the function call due to BeamEditor.IsInitialized being false, the promise will be set by InitBeamEditorWindow's lines after GetWindow.
				// So the promise here won't be null and we can complete it.
				else
					FullyInitializedWindowPromise.CompleteSuccess();
			}, true);
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
		/// <see cref="DelayedInitializationCall"/>.
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

		/// <summary>
		/// Rebuilds the window's entire content.
		/// If it cares about whether or not the given <paramref name="context"/> is/isn't authenticated, it'll invoke either <see cref="Build"/> or <see cref="BuildWhenNotAuthenticated"/>.
		/// If the given <paramref name="context"/> is null, it will rebuild with the current <see cref="ActiveContext"/>. 
		/// </summary>
		/// <param name="context">The <see cref="BeamEditorContext"/> to rebuild this window with. When null, re-uses the existing context.</param>
		public void BuildWithContext(BeamEditorContext context = null)
		{
			ActiveContext = context ?? ActiveContext;

			if (InitializedConfig.RequireLoggedUser)
			{
				if (ActiveContext.IsAuthenticated)
					Build();
				else
					BuildWhenNotAuthenticated();

				return;
			}

			Build();
		}

		protected abstract void Build();

		protected virtual void BuildWhenNotAuthenticated()
		{
			var root = this.GetRootVisualContainer();
			root.Clear();
			var noUserVisualElement = new NoUserVisualElement();
			root.Add(noUserVisualElement);
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
	}
}
