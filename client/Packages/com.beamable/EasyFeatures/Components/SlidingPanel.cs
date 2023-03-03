using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable.EasyFeatures.Components
{
	public class SlidingPanel : CustomOverlay, IDragHandler, IEndDragHandler
	{
		public Vector2 SlideAxis;
		public RectTransform Transform;
		public Vector2 VisibleAnchoredPosition;
		public Vector2 HiddenAnchoredPosition;
		public float SlideSpeed = 20;

		public event Action OnHidden;

		public void OnDrag(PointerEventData eventData)
		{
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

		private bool IsCloserToVisiblePosition() =>
			Vector2.Distance(Transform.anchoredPosition, VisibleAnchoredPosition) <
			Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition);

		public bool IsHidden() => Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition) <= 1;
		
		protected void SlideIn()
		{
			StopAllCoroutines();
			StartCoroutine(SlidePopup(VisibleAnchoredPosition));
		}

		protected void SlideOut()
		{
			StopAllCoroutines();
			StartCoroutine(SlidePopup(HiddenAnchoredPosition, OnHidden));
		}

		private IEnumerator SlidePopup(Vector2 targetAnchoredPosition, Action onSlideFinished = null)
		{
			while (Vector2.Distance(Transform.anchoredPosition, targetAnchoredPosition) > 1)
			{
				Transform.anchoredPosition =
					Vector2.Lerp(Transform.anchoredPosition, targetAnchoredPosition, Time.deltaTime * SlideSpeed);
				yield return null;
			}

			Transform.anchoredPosition = targetAnchoredPosition;
			onSlideFinished?.Invoke();
		}

		public override void Show()
		{
			gameObject.SetActive(true);
		}
	}
}
