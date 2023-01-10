using Beamable.EasyFeatures.Components;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicAccountManagement
{
	public class SwitchAccountPopup : CustomOverlay, IDragHandler, IEndDragHandler
	{
		public float SlideSpeed = 20;
		public float BottomOffset = 20;
		public Button SignInButton;
		public Button CreateAccountButton;
		public RectTransform Transform;

		private OverlaysController _overlaysController;

		public void Setup(UnityAction signInAction, UnityAction createAccountAction, OverlaysController overlaysController)
		{
			_overlaysController = overlaysController;
			SignInButton.onClick.AddListener(signInAction);
			CreateAccountButton.onClick.AddListener(createAccountAction);
			gameObject.SetActive(true);
		}

		public override void Show()
		{
			// set initial (hidden) position
			Transform.anchoredPosition = new Vector2(Transform.anchoredPosition.x, -Transform.sizeDelta.y);
			
			base.Show();

			SlideIn();
		}

		public override void Hide()
		{
			SlideOut(onSlideFinished: base.Hide);
		}

		private void SlideIn()
		{
			StopAllCoroutines();
			Vector2 targetPosition = Transform.anchoredPosition;
			targetPosition.y = BottomOffset;
			StartCoroutine(SlidePopup(targetPosition));
		}

		private void SlideOut(Action onSlideFinished)
		{
			StopAllCoroutines();
			Vector2 targetPosition = Transform.anchoredPosition;
			targetPosition.y = -Transform.sizeDelta.y;
			StartCoroutine(SlidePopup(targetPosition, onSlideFinished));
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

		public void OnDrag(PointerEventData eventData)
		{
			Vector2 position = Transform.anchoredPosition;
			position.y = Mathf.Clamp(position.y + eventData.delta.y, -Transform.sizeDelta.y, BottomOffset);
			Transform.anchoredPosition = position;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			// snap to visible or hidden position whichever is closer
			Vector2 hiddenPosition = new Vector2(Transform.anchoredPosition.x, -Transform.sizeDelta.y);
			Vector2 visiblePosition = new Vector2(Transform.anchoredPosition.x, BottomOffset);
			if (Vector2.Distance(Transform.anchoredPosition, visiblePosition) <
			    Vector2.Distance(Transform.anchoredPosition, hiddenPosition))
			{
				SlideIn();
			}
			else
			{
				SlideOut(_overlaysController.HideOverlay);
			}
		}
	}
}
