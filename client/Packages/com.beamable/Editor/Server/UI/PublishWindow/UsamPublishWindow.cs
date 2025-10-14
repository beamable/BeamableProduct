using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2.PublishWindow
{
	public partial class UsamPublishWindow : EditorWindow
	{
		public static int instanceCount = 0;
		public class PlanRow
		{
			public BeamPlanReleaseProgress progress;
			public string serviceName; // lifted out of the progress in case it isn't sent every time

			public bool IsService => !string.IsNullOrEmpty(serviceName);
		}

		public enum State
		{
			PLAN, 
			REVIEW,
			UPLOAD,
			SUCCESS,
			FATAL_ERROR
		}
		
		private DeploymentPlanWrapper _planCommand;
		private Promise _planPromise;
		private State state;
		private string manifestComment;

		private HashSet<string> _failedServices = new HashSet<string>();
		private bool isCancelPressed;

		private Dictionary<string, PlanRow> _planProgressNameToRatio = new Dictionary<string, PlanRow>();
	
		private Dictionary<string, PlanRow> _releaseProgressToRatio = new Dictionary<string, PlanRow>();
		
		private int _repaintRequest;
		private ErrorOutput _fatalError;

		private readonly Color backdropColor = new Color(0, 0, 0, .3f);
		private readonly Color loadingPrimary = new Color(.25f, .5f, 1f, 1);
		private readonly Color loadingFailed = new Color(1, .3f, .25f, 1);
		private static BeamEditorContext _ctx;
		private static Texture _animationTexture;
		private static GUIStyle _primaryButtonStyle;
		private BeamDeploymentPlanMetadata _planMetadata;
		private DeploymentReleaseWrapper _releaseCommand;
		private Promise _releasePromise;
		private int windowMinHeight;
		private DeploymentPlanArgs _planArgs;

		const int padding = 10;
		const int labelWidth = 200;

		public static void Init(BeamEditorContext ctx)
		{
			_ctx = ctx;
			var window = CreateInstance<UsamPublishWindow>();
			window.titleContent = new GUIContent("Planning Deployment");
			window.StartPlan();
			window.minSize = new Vector2(450, 500);
			window.ShowUtility();
			
		}
		
		public void OnEnable()
		{
			instanceCount++;
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			instanceCount--;
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			Repaint();

			// if (_repaintRequest > 0)
			// {
			// 	Repaint();
			// }
			_repaintRequest = 0;

		}
		
		void StartPlan()
		{
			// run the plan operation
			if (_planCommand == null)
			{
				_planProgressNameToRatio["fetching latest"] = new PlanRow
				{
					progress = new BeamPlanReleaseProgress {name = "fetching latest"}
				};
				
				state = State.PLAN;
				var config = _ctx.ServiceScope.GetService<MicroserviceConfiguration>();

				_planArgs = new DeploymentPlanArgs
				{
					runHealthChecks = config.EnablePrePublishHealthCheck, 
					merge = config.EnableMergeDeploy
				};
				_planCommand = _ctx.Cli.DeploymentPlan(_planArgs);
				_planCommand.OnProgressPlanReleaseProgress(cb =>
				{
					var progressName = cb.data.name;
					if (!_planProgressNameToRatio.TryGetValue(progressName, out var existing))
					{
						_planProgressNameToRatio[progressName] = existing = new PlanRow
						{
							progress = cb.data,
							serviceName = cb.data.serviceName
						};
					}
					
					existing.progress = cb.data;
					_repaintRequest++;

				});
				_planCommand.OnBuildErrorsRunProjectBuildErrorStream(cb =>
				{
					_failedServices.Add(cb.data.serviceId);
					Debug.LogError($"Failed to build {cb.data.serviceId}");
					foreach (var err in cb.data.report.errors)
					{
						Debug.LogError(" " + err.formattedMessage);
					}

				});
				_planCommand.OnStreamDeploymentPlanMetadata(cb =>
				{
					_planMetadata = cb.data;
					titleContent = new GUIContent("Review Deployment");
				});
				_planCommand.OnError(err =>
				{
					if (_failedServices.Count > 0) return;
					state = State.FATAL_ERROR;
					_fatalError = err.data;
				});
				_planCommand.OnLog(cb =>
				{
					var levelChar = cb.data.logLevel.ToLowerInvariant()[0];
					if (levelChar == 'e' || levelChar == 'f') // error or fatal 
					{
						Debug.LogError(cb.data.message);
					}
				});
				_planPromise = _planCommand.Run();
			}
		}

		void StartRelease()
		{
			_contentScroll = Vector2.zero;
			state = State.UPLOAD;
			_releaseCommand = _ctx.Cli.DeploymentRelease(new DeploymentReleaseArgs
			{
				fromPlan = _planMetadata.planPath,
				comment = $"\"{manifestComment}\""
			});

			_releaseCommand.OnProgressPlanReleaseProgress(cb =>
			{
				var progressName = cb.data.name;
				if (!_releaseProgressToRatio.TryGetValue(progressName, out var existing))
				{
					_releaseProgressToRatio[progressName] = existing = new PlanRow
					{
						progress = cb.data,
						serviceName = cb.data.serviceName
					};
				}

				existing.progress = cb.data;
				_repaintRequest++;
			});
			_releaseCommand.OnError(err =>
			{
				state = State.FATAL_ERROR;
				_fatalError = err.data;
			});

			_releasePromise = _releaseCommand.Run();
		}

		private void OnGUI()
		{
			{ // create textures
				
				if (_primaryButtonStyle == null)
				{
					_primaryButtonStyle = BeamGUI.ColorizeButton(loadingPrimary);
					_primaryButtonStyle.padding = new RectOffset(6, 6, 6, 6);
				}
			}
			
			if (_ctx == null)
			{
				Close();
				return;
			}

			// reserve rect for top loading.
			{
				var totalRatio = TotalRatio;
				var isErr = _failedServices.Count > 0;
				var loadingRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
				                                           GUILayout.Height(8),
				                                           GUILayout.ExpandWidth(true));
				AddLoadingBarIfLoading(loadingRect, totalRatio, isErr);
				EditorGUI.DrawRect(loadingRect, backdropColor);
				var loadingProgressRect = GetLoadingRectHorizontal(loadingRect, totalRatio);
				EditorGUI.DrawRect(loadingProgressRect, isErr ? loadingFailed:  loadingPrimary);
			}

			switch (state)
			{
				case State.PLAN:
					DrawPlanUI();
					break;
				case State.REVIEW:
					DrawReviewUi(false);
					break;
				case State.UPLOAD:
					DrawReviewUi(true);
					break;
				case State.FATAL_ERROR:
					DrawFatalError();
					break;
			}
			

			

			{ // handle min size
				// minSize = new Vector2(minSize.x, Mathf.Max(minSize.y, finalRect.yMax));
				minSize = new Vector2(minSize.x, windowMinHeight);

			}

			{ // handle button actions
				if (isCancelPressed)
				{
					isCancelPressed = false;
					Close();
				}
			}
		}

		private void OnDestroy()
		{
			if (_releaseCommand != null)
			{
				_releaseCommand.Cancel();
			}
		}
		

		void AddLoadingBarIfLoading(Rect loadingRect, float value, bool isFailed)
		{
			if (!isFailed && value > 0 && value < .99f)
			{
				// Repaint();
				_repaintRequest++;
				loadingRect = GetLoadingRectHorizontal(loadingRect, value);
				
				if (_animationTexture == null)
				{
					_animationTexture =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/loading_animation.png");
				}
				
				{
					var time = (float)((EditorApplication.timeSinceStartup * .7) % 1);
					GUI.DrawTextureWithTexCoords(loadingRect, _animationTexture,
					                             new Rect(-time, 0, 1.2f, 1));
				}
			}
			
		}

		void DoFlexHeight()
		{
			var finalRect = GUILayoutUtility.GetLastRect();
			const int paddingForCommentsAndButtons = 85;
			windowMinHeight = (int)(finalRect.yMax + paddingForCommentsAndButtons);
			windowMinHeight = Mathf.Min(windowMinHeight, 1000);
			
			GUILayout.FlexibleSpace();
		}

		void DrawManifestComment()
		{
			EditorGUILayout.BeginVertical(new GUIStyle()
			{
				padding = new RectOffset(padding, padding, 0, 0)
			});
			GUI.enabled = _releasePromise == null;
			manifestComment = BeamGUI.LayoutPlaceholderTextField(manifestComment, "Deployment comment", new GUIStyle(EditorStyles.textArea)
			{
				alignment = TextAnchor.UpperLeft,
			}, GUILayout.Height(EditorGUIUtility.singleLineHeight * 3));
			EditorGUILayout.EndVertical();
			GUI.enabled = true;
			EditorGUILayout.Space(10, expand: false);
		}

		void DrawHeader(string text)
		{
			{ // show upload section
				EditorGUILayout.Space(15, expand: false);
				var centerStyle = new GUIStyle(EditorStyles.largeLabel)
				{
					alignment = TextAnchor.MiddleCenter, wordWrap = true
				};
				EditorGUILayout.SelectableLabel(text, centerStyle);
				EditorGUILayout.Space(10, expand: false);
			}
		}

		void DrawLoadingBar(string label, float value, bool failed=false, GUIStyle labelStyleBase=null, Action drawBelowLoadingBarUI=null)
		{
			EditorGUILayout.BeginHorizontal(new GUIStyle
			{
				// leave some room for a loading indicator... 
				padding = new RectOffset(0, 30, 0, 0)
			});
			var labelStyle = new GUIStyle(labelStyleBase ?? EditorStyles.miniLabel)
			{
				alignment = TextAnchor.UpperLeft, 
				wordWrap = true,
				richText = true
			};


			EditorGUILayout.BeginVertical(GUILayout.Width(labelWidth));
			EditorGUILayout.LabelField(new GUIContent(label), labelStyle, GUILayout.MaxWidth(labelWidth), GUILayout.Width(labelWidth));
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			
			// reserve a rect that acts as top padding
			GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
			                         GUILayout.Height(2),
			                         GUILayout.ExpandWidth(true));
			var progressRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
			                                         GUILayout.Height(5),
			                                         GUILayout.ExpandWidth(true));
			
			// reserve a rect that acts as lower padding
			GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
			                         GUILayout.Height(2),
			                         GUILayout.ExpandWidth(true));
			
			progressRect = new Rect(progressRect.x, progressRect.y, progressRect.width - 4,
			                                                progressRect.height);
	        EditorGUI.DrawRect(progressRect, backdropColor);

	        var color = failed ? loadingFailed : loadingPrimary;
	        
	        EditorGUI.DrawRect(GetLoadingRectHorizontal(progressRect, value), color);
	        AddLoadingBarIfLoading(new Rect(progressRect.x, progressRect.y, progressRect.width, progressRect.height), value, failed);
			                        
			drawBelowLoadingBarUI?.Invoke();
			EditorGUILayout.EndVertical();

			var numericRect = new Rect(progressRect.xMax + 4, 2 + progressRect.y - EditorGUIUtility.singleLineHeight*.5f, 30, EditorGUIUtility.singleLineHeight);
			EditorGUI.SelectableLabel(numericRect, value < .01f ? "--" : $"{(value*100):00}%", new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = new GUIStyleState
				{
					textColor = value < .01f 
						? new Color(0, 0, 0, .4f)
						: color
				}
			});

			EditorGUILayout.EndHorizontal();
		}

		void DrawConfigurationWarnings()
		{
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(padding - 2, expand:false);
				// draw warning about additive

				EditorGUILayout.BeginVertical();
				if (_planArgs.merge)
				{
					EditorGUILayout.HelpBox(
						"The release is in 'merge' mode. This is controlled through Project Settings. This means that if a service has been deleted locally, it will not removed on the Realm", MessageType.Info);
					EditorGUILayout.Space(padding, expand:false);
				}

				// if (!_planArgs.runHealthChecks)
				// {
				// 	EditorGUILayout.HelpBox($"Services are only compiled, but they are not being runtime verified. To change this, change the {nameof(MicroserviceConfiguration.EnablePrePublishHealthCheck)} setting in Project Settings. ", MessageType.Warning);
				// 	EditorGUILayout.Space(padding, expand:false);
				// }
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(padding - 2, expand:false);

				EditorGUILayout.EndHorizontal();
			}
		}

		private float TotalRatio
		{
			get
			{
				if (_releaseCommand != null)
				{
					return _releaseProgressToRatio.Sum(x => x.Value.progress.ratio) / _releaseProgressToRatio.Count;
				}
				return _planProgressNameToRatio.Sum(x => x.Value.progress.ratio) / _planProgressNameToRatio.Count;
			}
		}

		public static Rect GetLoadingRectHorizontal(Rect fullRect, float ratio)
		{
			return new Rect(fullRect.x, fullRect.y, fullRect.width * ratio, fullRect.height);
		}
	}
}
