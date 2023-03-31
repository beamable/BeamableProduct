using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable.EasyFeatures.Components
{
	public class SlidingPanel : CustomOverlay, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private enum SlidingMode
		{
			PositionBased, SizeBased
		}

		[SerializeField]
		private SlidingMode SlideMode = SlidingMode.PositionBased;

		public RectTransform Transform;
		public Vector2 SlideAxis;
		public float SlideSpeed = 20;

		[Header("Position Based Properties")]
		public Vector2 VisibleAnchoredPosition;
		public Vector2 HiddenAnchoredPosition;

		[Header("Size Based Properties")]
		[Tooltip(
			"Optional reference to the transform hidden beneath this panel. It's used to automatically set folded and unfolded sizes. Unfolded is the size on `Start`.")]
		public RectTransform TransformToShow;
		[Tooltip("The margin added to the automatically set size based on the `Transform To Show` above.")]
		public float Margin = 5;
		[Tooltip("This size has to be bigger than Folded Size Delta")]
		public Vector2 UnfoldedSizeDelta;
		[Tooltip("This size has to be smaller than Unfolded Size Delta")]
		public Vector2 FoldedSizeDelta;

		public event Action OnDragStarted;
		public event Action OnHidden;

		private void Start()
		{
			if (Transform == null)
			{
				Transform = GetComponent<RectTransform>();
			}

			if (TransformToShow)
			{
				UnfoldedSizeDelta = Transform.sizeDelta;
				FoldedSizeDelta = UnfoldedSizeDelta - TransformToShow.sizeDelta * SlideAxis - Margin * SlideAxis;
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			OnDragStarted?.Invoke();
		}

		public void OnDrag(PointerEventData eventData)
		{
			switch (SlideMode)
			{
				case SlidingMode.PositionBased:
					Vector2 position = Transform.anchoredPosition + eventData.delta * SlideAxis;

					// make sure the current position is between hidden and visible positions
					// otherwise clamp the position to the closer one (hidden or visible)
					var lhs = VisibleAnchoredPosition - position;
					var rhs = HiddenAnchoredPosition - position;
					var dot = Vector2.Dot(lhs.normalized, rhs.normalized);
					if (dot > 0)
					{
						position = IsCloserToVisiblePosition() ? VisibleAnchoredPosition : HiddenAnchoredPosition;
					}

					Transform.anchoredPosition = position;

					break;

				case SlidingMode.SizeBased:
					Vector2 newSize = Transform.sizeDelta + eventData.delta * SlideAxis;

					newSize.x = Mathf.Clamp(newSize.x, FoldedSizeDelta.x, UnfoldedSizeDelta.x);
					newSize.y = Mathf.Clamp(newSize.y, FoldedSizeDelta.y, UnfoldedSizeDelta.y);

					Transform.sizeDelta = newSize;

					break;

				default:
					throw new ArgumentException($"Unhandled Sliding Mode: '{SlideMode}'");
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (IsCloserToVisiblePosition())
			{
				SlideIn();
			}
			else
			{
				SlideOut();
			}
		}

		private bool IsCloserToVisiblePosition()
		{
			if (SlideMode == SlidingMode.PositionBased)
			{
				return Vector2.Distance(Transform.anchoredPosition, VisibleAnchoredPosition) <
				       Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition);
			}

			return Vector2.Distance(Transform.sizeDelta, UnfoldedSizeDelta) >
			       Vector2.Distance(Transform.sizeDelta, FoldedSizeDelta);
		}

		public bool IsHidden()
		{
			if (SlideMode == SlidingMode.PositionBased)
			{
				return Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition) <= 1;
			}

			return Vector2.Distance(Transform.sizeDelta, UnfoldedSizeDelta) <= 1;
		}

		protected void SlideIn()
		{
			StopAllCoroutines();
			Vector2 targetVector = SlideMode == SlidingMode.PositionBased ? VisibleAnchoredPosition : FoldedSizeDelta;
			StartCoroutine(SlidePopup(targetVector));
		}

		protected void SlideOut()
		{
			StopAllCoroutines();
			Vector2 targetVector = SlideMode == SlidingMode.PositionBased ? HiddenAnchoredPosition : UnfoldedSizeDelta;
			StartCoroutine(SlidePopup(targetVector, OnHidden));
		}

		private IEnumerator SlidePopup(Vector2 targetVector, Action onSlideFinished = null)
		{
			if (SlideMode == SlidingMode.PositionBased)
			{
				while (Vector2.Distance(Transform.anchoredPosition, targetVector) > 1)
				{
					Transform.anchoredPosition =
						Vector2.Lerp(Transform.anchoredPosition, targetVector, Time.deltaTime * SlideSpeed);
					yield return null;
				}

				Transform.anchoredPosition = targetVector;
			}
			else
			{
				while (Vector2.Distance(Transform.sizeDelta, targetVector) > 1)
				{
					Transform.sizeDelta =
						Vector2.Lerp(Transform.sizeDelta, targetVector, Time.deltaTime * SlideSpeed);
					yield return null;
				}

				Transform.sizeDelta = targetVector;
			}

			onSlideFinished?.Invoke();
		}

		public override void Show()
		{
			gameObject.SetActive(true);
		}
	}
}
