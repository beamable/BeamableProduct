using Beamable.EasyFeatures;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Beamable.Editor.UI.Buss
{
	public class BussThemeSceneUtility
	{
		/// <summary>
		/// When the camera is 8000 units away, don't bother trying to render text.
		/// </summary>
		private const int TEXT_RANGE_DISTANCE = 8000;
		
		public BussPrefabLoaderSource source;

		private List<BussPrefabSceneGroupLabel> labels = new List<BussPrefabSceneGroupLabel>();
		private List<BussPrefabGroupButton> buttons = new List<BussPrefabGroupButton>();
		private int _prefabHeight;
		private int _prefabWidth;
		private Scene scene;
		
		public BussThemeSceneUtility()
		{
			
		}

		public void Open(Scene scene)
		{
			this.scene = scene;
			StyleCache.Instance.Erase();
			_prefabHeight = 2000;
			_prefabWidth = 1100;
			PopulateEasyFeatures(out var xPos, out var yPos);
			yPos += 500;
			PopulateComponents(xPos, yPos);
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

				var inRange = IsSceneViewCameraInRange(obj.camera, labelPos, TEXT_RANGE_DISTANCE);
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
			return new Bounds(Vector3.zero, Vector3.zero);
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

		private void PopulateEasyFeatures(out float xPosition, out float yPosition)
		{
			yPosition = 0;
			xPosition = 0;
			var featureObjects = new List<GameObject>();
			
			#region generate prefabs
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
					
					canvas.renderMode = RenderMode.WorldSpace;

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

			#endregion
			#region generate bounds
			foreach (var featureObject in featureObjects)
			{
				Bounds featureBounds = default;

				for (var i = 0; i < featureObject.transform.childCount; i++)
				{
					var flowObject = featureObject.transform.GetChild(i).gameObject;
					var instanceEntry = new BussPrefabSceneGroupLabel
					{
						label = flowObject.name, 
						bounds = GetLocalBounds(flowObject),//new Bounds(canvasRect.position, new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0)),
						lineOffset = new Vector3(0, 3, 0),
						textOffset = new Vector3(0, 80, 0),
						textHeight = 40,
						textStyle = new GUIStyle(EditorStyles.label) {normal = {textColor = Color.white}}
					};
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
					label = featureObject.name, 
					bounds = featureBounds,
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold,
						normal = {textColor = Color.white}
					},
				};
				labels.Add(labelEntry);
			}
			#endregion
		}

		private void PopulateComponents(float xPos, float yPos)
		{
			var root = new GameObject("Components");
			var canvas = root.AddComponent<Canvas>();
			var canvasRect = canvas.transform as RectTransform;
			canvasRect.pivot = new Vector2(0, 0);
			root.AddComponent<CanvasScaler>();
			root.AddComponent<GraphicRaycaster>();

			root.transform.localPosition += new Vector3(0, yPos, 0);

			var categoryObjects = new List<GameObject>();

			var first = 0f;
			#region generate components
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

				
				yPos += categoryBounds.size.y + 500;
				if (i == 0)
				{
					first = yPos;
				}
				gob.transform.localPosition = new Vector3(0, yPos, 0);
			}
			EditorSceneManager.MoveGameObjectToScene(root, scene);
			#endregion
			#region generate bounds
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
						label = componentObject.name, 
						bounds = bounds,
						lineOffset = new Vector3(0, 3, 0),
						textOffset = new Vector3(0, 80, 0),
						textHeight = 40,
						textStyle = new GUIStyle(EditorStyles.label) {normal = {textColor = Color.white}},
					};
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
					label = categoryObject.name, 
					bounds = categoryBounds,
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold,
						normal = {textColor = Color.white}
					},
				};
				labels.Add(categoryEntry);

			}
			
			buttons.Add(new BussPrefabGroupButton
			{
				bounds = allBounds,
				gob = root,
				label = "Components"
			});
			#endregion
			
		}
		
	}
	
	#if UNITY_2020_1_OR_NEWER
	public class BussStage : PreviewSceneStage
	{
		/// <summary>
		/// When the camera is 8000 units away, don't bother trying to render text.
		/// </summary>
		private const int TEXT_RANGE_DISTANCE = 8000;
		
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

				var inRange = IsSceneViewCameraInRange(obj.camera, labelPos, TEXT_RANGE_DISTANCE);
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
			
			#region generate prefabs
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
					
					canvas.renderMode = RenderMode.WorldSpace;

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

			#endregion
			#region generate bounds
			foreach (var featureObject in featureObjects)
			{
				Bounds featureBounds = default;

				for (var i = 0; i < featureObject.transform.childCount; i++)
				{
					var flowObject = featureObject.transform.GetChild(i).gameObject;
					var instanceEntry = new BussPrefabSceneGroupLabel
					{
						label = flowObject.name, 
						bounds = GetLocalBounds(flowObject),//new Bounds(canvasRect.position, new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0)),
						lineOffset = new Vector3(0, 3, 0),
						textOffset = new Vector3(0, 80, 0),
						textHeight = 40,
						textStyle = new GUIStyle(EditorStyles.label) {normal = {textColor = Color.white}}
					};
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
					label = featureObject.name, 
					bounds = featureBounds,
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold,
						normal = {textColor = Color.white}
					},
				};
				labels.Add(labelEntry);
			}
			#endregion
		}

		private void PopulateComponents(float xPos, float yPos)
		{
			var root = new GameObject("Components");
			var canvas = root.AddComponent<Canvas>();
			var canvasRect = canvas.transform as RectTransform;
			canvasRect.pivot = new Vector2(0, 0);
			root.AddComponent<CanvasScaler>();
			root.AddComponent<GraphicRaycaster>();

			root.transform.localPosition += new Vector3(0, yPos, 0);

			var categoryObjects = new List<GameObject>();

			var first = 0f;
			#region generate components
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

				
				yPos += categoryBounds.size.y + 500;
				if (i == 0)
				{
					first = yPos;
				}
				gob.transform.localPosition = new Vector3(0, yPos, 0);
			}
			EditorSceneManager.MoveGameObjectToScene(root, scene);
			#endregion
			#region generate bounds
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
						label = componentObject.name, 
						bounds = bounds,
						lineOffset = new Vector3(0, 3, 0),
						textOffset = new Vector3(0, 80, 0),
						textHeight = 40,
						textStyle = new GUIStyle(EditorStyles.label) {normal = {textColor = Color.white}},
					};
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
					label = categoryObject.name, 
					bounds = categoryBounds,
					lineOffset = new Vector3(0, 50, 0),
					textOffset = new Vector3(0, 170, 0),
					textHeight = 70,
					textStyle = new GUIStyle(EditorStyles.label)
					{
						fontStyle = FontStyle.Bold,
						normal = {textColor = Color.white}
					},
				};
				labels.Add(categoryEntry);

			}
			
			buttons.Add(new BussPrefabGroupButton
			{
				bounds = allBounds,
				gob = root,
				label = "Components"
			});
			#endregion
			
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
#endif

	[Serializable]
	public class BussPrefabSceneGroupLabel
	{
		public string label;
		public Bounds bounds;
		public Vector3 lineOffset;
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

	public interface IBussPrefabSceneManager
	{
		void OpenPrefabScene();
		void TogglePrefabScene();
		bool IsPrefabSceneOpen();
		bool TryGetPrefabScene(out Scene scene);
	}
	
	#if UNITY_2019
	public class BussPrefabSceneManager : IBussPrefabSceneManager
	{
		public void OpenPrefabScene()
		{
			
		}

		public void TogglePrefabScene()
		{
		}

		public bool IsPrefabSceneOpen()
		{
			return false;
		}

		public bool TryGetPrefabScene(out Scene scene)
		{
			scene = default;
			return false;
		}
	}
	#elif UNITY_2020_1_OR_NEWER
	public class BussPrefabSceneManager : IBussPrefabSceneManager
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
#endif
}
