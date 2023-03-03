namespace Beamable.EasyFeatures.Components
{
	public class SlidingOverlay : SlidingPanel
	{
		private OverlaysController _overlaysController;

		protected void Setup(OverlaysController overlaysController)
		{
			_overlaysController = overlaysController;
			OnHidden -= OnPopupHidden;
			OnHidden += OnPopupHidden;
		}
		
		public override void Show()
		{
			Transform.anchoredPosition = HiddenAnchoredPosition;
			
			base.Show();

			SlideIn();
		}

		public override void Hide()
		{
			if (!IsHidden())
			{
				SlideOut();
			}
		}

		private void OnPopupHidden()
		{
			_overlaysController.HideOverlay();
			Destroy(gameObject);
		}
	}
}
