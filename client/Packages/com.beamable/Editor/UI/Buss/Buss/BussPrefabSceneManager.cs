using Beamable.EasyFeatures;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beamable.Editor.UI.Buss
{
	public class BussHelper
	{
		[MenuItem("BEAM_TEST/OPEN")]
		public static void OpenScene()
		{
			var srvc = BeamEditorContext.Default.ServiceScope.GetService<BussPrefabSceneManager>();
			srvc.OpenPrefabScene();
		}
	}
	
	[InitializeOnLoad]
	public class PointLabelHandlersEditor : UnityEditor.Editor
	{
		private static bool _someToggle;
 
		static PointLabelHandlersEditor()
		{
		}
 

	}

	public class BussStage : PreviewSceneStage
	{
		public BussPrefabLoaderSource source;

		private List<BussPrefabSceneGroupLabel> labels = new List<BussPrefabSceneGroupLabel>();
		private List<BussPrefabGroupButton> buttons = new List<BussPrefabGroupButton>();
		private Action _onClose;

		public void SetSource(BussPrefabLoaderSource source)
		{
			this.source = source;
		}

		public void OnClose(Action onClose)
		{
			_onClose = onClose;
		}
		
		private void OnDuringSceneGui(SceneView obj)
		{
			// var buttonHeight = 40;
			Handles.BeginGUI();
			{
				
				var style = new GUIStyle(GUI.skin.button);
				style.alignment = TextAnchor.MiddleCenter;
				GUILayout.BeginArea(new Rect(0,obj.position.height - 90, obj.position.width, 50));
				{

					GUILayout.BeginHorizontal();
					// GUI.Button()
					GUILayout.FlexibleSpace();
					foreach (var button in buttons)
					{
						if (GUILayout.Button(button.label, style, GUILayout.Height(30)))
						{
							Selection.activeObject = button.gob;
							obj.Frame(button.bounds, false);
						}
					}
					GUILayout.FlexibleSpace();

					GUILayout.EndHorizontal();

					// _someToggle= GUILayout.Toggle(_someToggle, "Some toggle!");
				}
				GUILayout.EndArea();
			}
			Handles.EndGUI();


			// HandleUtility.
			foreach (var label in labels)
			{
				Handles.color = Color.red;
				var topLeft = new Vector3(label.bounds.center.x - label.bounds.extents.x, label.bounds.max.y, 0) + label.lineOffset;
				var topRight = new Vector3(label.bounds.center.x + label.bounds.extents.x, label.bounds.max.y, 0) + label.lineOffset;
				
				// Handles.DrawWireCube(label.bounds.min, Vector3.one);
				
				var x = obj.camera.WorldToScreenPoint(topLeft) - obj.camera.WorldToScreenPoint(topLeft + new Vector3(0, label.textHeight, 0));
				var fontSize = Mathf.RoundToInt(Mathf.Abs(x.y));
				var handleSize = HandleUtility.GetHandleSize(topLeft);
				var labelPos = topLeft + label.textOffset;

				var inRange = IsSceneViewCameraInRange(obj.camera, labelPos, 8000);
				if (!inRange) continue;
				Handles.Label(labelPos, label.label, new GUIStyle(label.textStyle)
				{
					fontSize = fontSize
					// fontSize = (int)(label.textStyle.fontSize * obj.camera.pixelHeight * .002f)
				});
			}
		}

		protected override void OnEnable()
		{
			SceneView.duringSceneGui += OnDuringSceneGui;
			
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			SceneView.duringSceneGui -= OnDuringSceneGui;
			base.OnDisable();
		}

		protected override bool OnOpenStage()
		{
			if (!base.OnOpenStage())
			{
				return false;
			}
			
			StyleCache.Instance.Erase();

			var yPosition = 0;
			
			
			foreach (var element in source.easyFeatures)
			{
				var parent = new GameObject(element.label + " Flows");
				parent.transform.localPosition = new Vector3(0, yPosition, 0);
				
				var buttonEntry = new BussPrefabGroupButton {label = element.label, gob = parent};
				var labelEntry = new BussPrefabSceneGroupLabel
				{
					gob = parent,
					label = element.label, 
					bounds = new Bounds(),
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold
					},
					lineWidth = 2
				};
				labelEntry.textStyle.normal.textColor = Color.white;
				labels.Add(labelEntry);
				buttons.Add(buttonEntry);

				var xPosition = 0f;
				var xPadding = 40;
				var yPadding = 500;
				var height = 2000;
				var width = 1100;
				yPosition += height + yPadding;
				var first = true;
				for (var i = 0; i < element.prefab.transform.childCount; i++)
				{
					var childGob = element.prefab.transform.GetChild(i).gameObject;
					var view = childGob.GetComponent<ISyncBeamableView>();
					if (view == null) continue;
					
					
					var instance = PrefabUtility.InstantiatePrefab(element.prefab, parent.transform) as GameObject;
					instance.name = childGob.name;


					var instanceEntry = new BussPrefabSceneGroupLabel
					{
						gob = instance,
						label = instance.name, 
						bounds = new Bounds(),
						lineOffset = new Vector3(0, 3, 0),
						textOffset = new Vector3(0, 80, 0),
						textHeight = 40,
						textStyle = new GUIStyle(EditorStyles.label)
						{
						},
						lineWidth = 2
					};
					instanceEntry.textStyle.normal.textColor = Color.white;
					labels.Add(instanceEntry);
					
					var canvas = instance.GetComponentInChildren<Canvas>();
					
					// labelEntry.bounds = canvas.
					var canvasRect = instance.transform as RectTransform;
					canvas.renderMode = RenderMode.WorldSpace;

					// TODO: put in some sort of forced aspect ratio option?
					canvas.transform.localScale = Vector3.one;

					canvasRect.sizeDelta = new Vector2(width, height);
					canvasRect.anchoredPosition = new Vector2(xPosition, 0);
					canvasRect.ForceUpdateRectTransforms();

					xPosition += canvasRect.sizeDelta.x + xPadding;
					
					var canvasBounds = new Bounds(canvasRect.position, new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0));
					instanceEntry.bounds = canvasBounds;
					if (first)
					{
						labelEntry.bounds = canvasBounds;
						first = false;
					}
					else
					{
						labelEntry.bounds.Encapsulate(canvasBounds);
					}
					buttonEntry.bounds = labelEntry.bounds;
					
					for (var j = 0; j < instance.transform.childCount; j++)
					{
						var instanceChildGob = instance.transform.GetChild(j).gameObject;
						var childView = instanceChildGob.GetComponent<ISyncBeamableView>();
						if (childView == null) continue;
						var isSelected = instanceChildGob.name == childGob.name;
						instanceChildGob.SetActive(isSelected);
					}
					
					PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				}
				
				EditorSceneManager.MoveGameObjectToScene(parent, scene);
			}
			
			
			return true;
		}

		protected override void OnCloseStage()
		{
			base.OnCloseStage();
		}

		protected override GUIContent CreateHeaderContent()
		{
			return new GUIContent("Buss Prefabs");
		}
		
		public static bool IsSceneViewCameraInRange(Camera cam, Vector3 position, float distance)
		{
			Vector3 cameraPos = cam.WorldToScreenPoint(position);
			return ((cameraPos.x >= 0) &&
			        (cameraPos.x <= cam.pixelWidth) &&
			        (cameraPos.y >= 0) &&
			        (cameraPos.y <= cam.pixelHeight) &&
			        (cameraPos.z > 0) &&
			        (cameraPos.z < distance));
		}
		

	}

	[Serializable]
	public class BussPrefabSceneGroupLabel
	{
		public string label;
		public GameObject gob;
		public Bounds bounds;
		public Vector3 lineOffset;
		public int lineWidth;
		public Vector3 textOffset;
		public float textHeight;
		public GUIStyle textStyle;
	}

	[Serializable]
	public class BussPrefabGroupButton
	{
		public string label;
		public GameObject gob;
		public Bounds bounds;
	}
	
	
	
	public class BussPrefabSceneManager
	{
		private readonly BussPrefabLoaderSourceProvider _sourceLoader;
		private BussStage _stage;

		public BussPrefabSceneManager(BussPrefabLoaderSourceProvider sourceLoader)
		{
			_sourceLoader = sourceLoader;
		}
		
		public void OpenPrefabScene()
		{
			_stage = BussStage.CreateInstance<BussStage>();
			_stage.SetSource(_sourceLoader.LoadSource());
			
			
			// TODO: in 2019, this won't work, so we'll need to find another way to open the stage. 
			// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/AssetDatabase.OpenAsset.html
			StageUtility.GoToStage(_stage, true);
		}

		public bool TryGetPrefabScene(out Scene scene)
		{
			scene = default;
			if (StageUtility.GetCurrentStage() is PreviewSceneStage previewSceneStage)
			{
				scene = previewSceneStage.scene;
				return true;
			}

			return false;
		}
	}
}
