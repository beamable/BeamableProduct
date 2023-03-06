using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable.EasyFeatures.Components
{
	public class SlidingPanel : CustomOverlay, IDragHandler, IEndDragHandler
	{
		private enum SlidingMode
		{
			PositionBased, SizeBased
		}

		[SerializeField]
		private SlidingMode Mode = SlidingMode.PositionBased;
		
		public Vector2 SlideAxis;
		public RectTransform Transform;
		public float SlideSpeed = 20;
		
		[Header("Position Based Properties")]
		public Vector2 VisibleAnchoredPosition;
		public Vector2 HiddenAnchoredPosition;

		[Header("Size Based Properties")]
		[Tooltip("This size has to be bigger than Folded Size Delta")]
		public Vector2 UnfoldedSizeDelta;
		[Tooltip("This size has to be smaller than Unfolded Size Delta")]
		public Vector2 FoldedSizeDelta;

		public event Action OnHidden;

		public void OnDrag(PointerEventData eventData)
		{
			switch (Mode)
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
					
					// Clamp X
					if (newSize.x < FoldedSizeDelta.x)
					{
						newSize.x = FoldedSizeDelta.x;
					}
					else if (newSize.x > UnfoldedSizeDelta.x)
					{
						newSize.x = UnfoldedSizeDelta.x;
					}

					// Clamp Y
					if (newSize.y < FoldedSizeDelta.y)
					{
						newSize.y = FoldedSizeDelta.y;
					}
					else if (newSize.y > UnfoldedSizeDelta.y)
					{
						newSize.y = UnfoldedSizeDelta.y;
					}
					
					Transform.sizeDelta = newSize;
					
					break;
				
				default:
					throw new ArgumentException($"Unhandled Sliding Mode: '{Mode}'");
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
			if (Mode == SlidingMode.PositionBased)
			{
				return Vector2.Distance(Transform.anchoredPosition, VisibleAnchoredPosition) <
				       Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition);
			}
			
			return Vector2.Distance(Transform.sizeDelta, UnfoldedSizeDelta) <
			       Vector2.Distance(Transform.sizeDelta, FoldedSizeDelta);
		}
			

		public bool IsHidden() => Vector2.Distance(Transform.anchoredPosition, HiddenAnchoredPosition) <= 1;
		
		protected void SlideIn()
		{
			StopAllCoroutines();
			Vector2 targetVector = Mode == SlidingMode.PositionBased ? VisibleAnchoredPosition : UnfoldedSizeDelta;
			StartCoroutine(SlidePopup(targetVector));
		}

		protected void SlideOut()
		{
			StopAllCoroutines();
			Vector2 targetVector = Mode == SlidingMode.PositionBased ? HiddenAnchoredPosition : FoldedSizeDelta;
			StartCoroutine(SlidePopup(targetVector, OnHidden));
		}

		private IEnumerator SlidePopup(Vector2 targetVector, Action onSlideFinished = null)
		{
			if (Mode == SlidingMode.PositionBased)
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
