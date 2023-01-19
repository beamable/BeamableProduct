using Beamable.EasyFeatures;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

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
		
		[MenuItem("BEAM_TEST/TOGGLE")]
		public static void Toggle()
		{
			var srvc = BeamEditorContext.Default.ServiceScope.GetService<BussPrefabSceneManager>();
			srvc.TogglePrefabScene();
			
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
		private int _prefabHeight;
		private int _prefabWidth;

		public void SetSource(BussPrefabLoaderSource source)
		{
			this.source = source;
		}

		private void OnDuringSceneGui(SceneView obj)
		{
			
			#region draw labels
			foreach (var label in labels)
			{
				Handles.color = Color.red;
				var topLeft = new Vector3(label.bounds.center.x - label.bounds.extents.x, label.bounds.max.y, 0) + label.lineOffset;
				
				var x = obj.camera.WorldToScreenPoint(topLeft) - obj.camera.WorldToScreenPoint(topLeft + new Vector3(0, label.textHeight, 0));
				var fontSize = Mathf.RoundToInt(Mathf.Abs(x.y));
				var labelPos = topLeft + label.textOffset;

				var inRange = IsSceneViewCameraInRange(obj.camera, labelPos, 8000);
				if (!inRange) continue;
				Handles.Label(labelPos, label.label, new GUIStyle(label.textStyle)
				{
					fontSize = fontSize
				});
			}
			#endregion
			
			#region draw buttons
			Handles.BeginGUI();
			{
				var style = new GUIStyle(GUI.skin.button);
				style.alignment = TextAnchor.MiddleCenter;
				GUILayout.BeginArea(new Rect(0,obj.position.height - 90, obj.position.width, 50));
				{

					GUILayout.BeginHorizontal();
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

				}
				GUILayout.EndArea();
			}
			Handles.EndGUI();
			#endregion

		}

		private static Bounds GetLocalBounds(GameObject gameObject)
		{
			RectTransform component1;
			if (gameObject.TryGetComponent<RectTransform>(out component1))
				return new Bounds((Vector3) component1.rect.center + gameObject.transform.position, (Vector3) component1.rect.size);
			MeshFilter component2;
			if (gameObject.TryGetComponent<MeshFilter>(out component2) && (UnityEngine.Object) component2.sharedMesh != (UnityEngine.Object) null)
				return component2.sharedMesh.bounds;
			// if (gameObject.TryGetComponent<Canvas>(out var canvas))
				// return new Bounds(canvasRect.position, new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0));
			return new Bounds(Vector3.zero, Vector3.zero);
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

		private void PopulateEasyFeatures(out float xPosition, out float yPosition)
		{
			yPosition = 0;
			xPosition = 0;
			var featureObjects = new List<GameObject>();
			foreach (var element in source.easyFeatures)
			{
				var parent = new GameObject(element.label + " Flows");
				parent.transform.localPosition = new Vector3(0, yPosition, 0);
				featureObjects.Add(parent);
				parent.name = element.label;
				

				xPosition = 0f;
				var xPadding = 40;
				var yPadding = 500;
				yPosition += _prefabHeight + yPadding;
				// var first = true;
				for (var i = 0; i < element.prefab.transform.childCount; i++)
				{
					var childGob = element.prefab.transform.GetChild(i).gameObject;
					var view = childGob.GetComponent<ISyncBeamableView>();
					if (view == null) continue;
					
					
					var instance = PrefabUtility.InstantiatePrefab(element.prefab, parent.transform) as GameObject;
					instance.name = childGob.name;
					var canvas = instance.GetComponentInChildren<Canvas>();
					var canvasRect = instance.transform as RectTransform;
					canvasRect.pivot = new Vector2(0, 0);
					
					// labelEntry.bounds = canvas.
					canvas.renderMode = RenderMode.WorldSpace;

					// TODO: put in some sort of forced aspect ratio option?
					canvas.transform.localScale = Vector3.one;

					canvasRect.sizeDelta = new Vector2(_prefabWidth, _prefabHeight);
					canvasRect.anchoredPosition = new Vector2(xPosition, 0);
					canvasRect.ForceUpdateRectTransforms();

					xPosition += canvasRect.sizeDelta.x + xPadding;
					
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


			foreach (var featureObject in featureObjects)
			{
				Bounds featureBounds = default;

				for (var i = 0; i < featureObject.transform.childCount; i++)
				{
					var flowObject = featureObject.transform.GetChild(i).gameObject;
					var instanceEntry = new BussPrefabSceneGroupLabel
					{
						gob = flowObject,
						label = flowObject.name, 
						bounds = GetLocalBounds(flowObject),//new Bounds(canvasRect.position, new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0)),
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
					if (i == 0)
					{
						featureBounds = instanceEntry.bounds;
					}
					else
					{
						featureBounds.Encapsulate(instanceEntry.bounds);
					}
				}
				
				
				var buttonEntry = new BussPrefabGroupButton {label = featureObject.name, gob = featureObject, bounds = featureBounds};
				buttons.Add(buttonEntry);

			
				var labelEntry = new BussPrefabSceneGroupLabel
				{
					gob = featureObject,
					label = featureObject.name, 
					bounds = featureBounds,
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
			}
		}

		private void PopulateComponents(float xPos, float yPos)
		{
			var root = new GameObject("Components");
			
			var canvas = root.AddComponent<Canvas>();
			var canvasRect = canvas.transform as RectTransform;
			canvasRect.pivot = new Vector2(0, 0);
			root.AddComponent<CanvasScaler>();
			root.AddComponent<GraphicRaycaster>();

			// temp...
			root.transform.localPosition += new Vector3(0, yPos, 0);

			// var categoryBoundsToCompute = new List<Tuple<GameObject, Action>>();
			var categoryObjects = new List<GameObject>();

			var first = 0f;
			for (var i = 0 ; i < source.categories.Count; i ++)
			{
				var category = source.categories[i];
				var gob = new GameObject(category.category);
				gob.transform.SetParent(root.transform);
				categoryObjects.Add(gob);
				
				xPos = 0f;
				Bounds categoryBounds = default;
				for (var j = 0 ; j < category.components.Count; j ++)
				{
					
					var component = category.components[j];
					var element = PrefabUtility.InstantiatePrefab(component.prefab, gob.transform) as BussElement;
					var instance = element.gameObject;
					element.name = component.label;
					var rectTransform = instance.transform as RectTransform;
					rectTransform.pivot = new Vector2(0, 1);

					if (component.forcedHeight.HasValue)
					{
						rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, component.forcedHeight.Value);
					}
					if (component.forcedWidth.HasValue)
					{
						rectTransform.sizeDelta = new Vector2(component.forcedWidth, rectTransform.sizeDelta.y);
					}

					
					instance.transform.localPosition = new Vector3(xPos, 0, 0);
					
					rectTransform.ForceUpdateRectTransforms();
					
					var bounds = GetLocalBounds(instance);
					if (category.minComponentSpaceWidth.HasValue)
					{
						var oldSize = bounds.size;
						bounds.size = new Vector3(Mathf.Max(bounds.size.x, category.minComponentSpaceWidth.Value), bounds.size.y);
						var diff = bounds.size - oldSize;
						bounds.center += diff * .5f;
					}

					if (j == 0)
					{
						categoryBounds = bounds;
					}
					else
					{
						categoryBounds.Encapsulate(bounds);
					}
					xPos += bounds.size.x + 40;
					
					instance.name = component.label;
					
				}

				
				yPos += categoryBounds.size.y + 500; // TODO: it should be the max height of any component
				if (i == 0)
				{
					first = yPos;
				}
				gob.transform.localPosition = new Vector3(0, yPos, 0);
			}

			Bounds allBounds = default;
			for (var i = 0; i < categoryObjects.Count; i ++ )
			{
				var categoryObject = categoryObjects[i];
				categoryObject.transform.localPosition -= Vector3.up * first;
				Bounds categoryBounds = default;

				for (var j = 0; j < categoryObject.transform.childCount; j++)
				{
					var componentObject = categoryObject.transform.GetChild(j).gameObject;
					
					var bounds = GetLocalBounds(componentObject);

					var instanceEntry = new BussPrefabSceneGroupLabel
					{
						gob = componentObject,
						label = componentObject.name, 
						bounds = bounds,
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
					if (j == 0)
					{
						categoryBounds = bounds;
					}
					else
					{
						categoryBounds.Encapsulate(bounds);
					}
				}
				
				if (i == 0)
				{
					allBounds = categoryBounds;
				}
				else
				{
					allBounds.Encapsulate(categoryBounds);
				}
				
				var categoryEntry = new BussPrefabSceneGroupLabel
				{
					gob = categoryObject,
					label = categoryObject.name, 
					bounds = categoryBounds,
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold
					},
					lineWidth = 2
				};
				categoryEntry.textStyle.normal.textColor = Color.white;
				labels.Add(categoryEntry);

			}
			
			buttons.Add(new BussPrefabGroupButton
			{
				bounds = allBounds,
				gob = root,
				label = "Components"
			});
			
			EditorSceneManager.MoveGameObjectToScene(root, scene);
		}
		
		protected override bool OnOpenStage()
		{
			if (!base.OnOpenStage())
			{
				return false;
			}
			
			StyleCache.Instance.Erase();

			
			_prefabHeight = 2000;
			_prefabWidth = 1100;
			PopulateEasyFeatures(out var xPos, out var yPos);
			yPos += 500;
			PopulateComponents(xPos, yPos);
			return true;
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

		public void TogglePrefabScene()
		{
			if (IsPrefabSceneOpen())
			{
				StageUtility.GoBackToPreviousStage();
			}
			else
			{
				OpenPrefabScene();
			}
		}

		public bool IsPrefabSceneOpen()
		{
			var currentStage = StageUtility.GetCurrentStage();
			return currentStage is BussStage;
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
