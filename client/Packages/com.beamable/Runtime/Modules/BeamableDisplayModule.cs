using System.Diagnostics;
using Beamable.UnityEngineClone.UI.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Beamable
{
    [ExecuteAlways]
    public class BeamableDisplayModule : BeamableModule
    {
        [HideInInspector] [SerializeField] private bool useThisCanvas;
        [HideInInspector] [SerializeField] private Canvas canvas;

        public void SetVisible(bool visible = true)
        {
            if (useThisCanvas)
            {
                canvas.enabled = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        void Awake()
        {
            CheckForCanvas();
        }

        [Conditional("UNITY_EDITOR")]
        private void CheckForCanvas()
        {
            void StrechRectTransform(RectTransform t)
            {
                t.anchorMin = Vector2.zero;
                t.anchorMax = Vector2.one;
                t.pivot = Vector2.zero;
                t.localScale = Vector3.one;
            }
            void RemoveIfExists<T>() where T : Behaviour
            {
                var component = GetComponent<T>();
                if (component != null)
                {
                    DestroyImmediate(component);
                }
            }
            
            var hasParent = transform.parent != null;

            if (!hasParent || Application.isPlaying)
                return;
            var parent = transform.parent.gameObject;
            var parentCanvas = parent.GetComponentInParent<Canvas>();
            useThisCanvas = parentCanvas == null;

            if (useThisCanvas)
            {
                canvas = GetComponent<Canvas>();
                Assert.IsNotNull(canvas, "Cannot find canvas component in game object or any of his parents");
            }
            else
            {
                RemoveIfExists<CanvasScaler>();
                RemoveIfExists<GraphicRaycaster>();
                RemoveIfExists<Canvas>();
                var parentTransform = parent.GetOrAddComponent<RectTransform>();
                EditorApplication.delayCall += () => StrechRectTransform(parentTransform);
                EditorApplication.delayCall += () => StrechRectTransform(GetComponent<RectTransform>());
            }
        }
    }
}