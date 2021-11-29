using System.Collections;
using System.Collections.Generic;
using Beamable.AccountManagement;
using Beamable.Coroutines;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.Avatars
{
    [System.Serializable]
    public class AccountAvatarEvent : UnityEvent<AccountAvatar> { }

    public class AvatarPickerBehaviour : MonoBehaviour
    {
        public RectTransform ContentContainer;
        public ScrollRect AvatarScroller;

        public GameObject AvatarSelectionIndicator;
        public AccountAvatarBehaviour AvatarPreviewPrefab;
        public AccountAvatarEvent OnSelected;

        public AccountAvatar Selected
        {
            get => _selected;
            set
            {
                Select(value?.Name);
            }
        }

        private AccountAvatar _selected;

        private List<AccountAvatarBehaviour> _avatarPreviews;

        // Start is called before the first frame update
        void Start()
        {
            Refresh();
            StartCoroutine(DeferScrollUpdate());

        }

        public void Select(string avatarName)
        {
            if (_avatarPreviews == null)
            {
                Refresh();
            }

            var found = _avatarPreviews.Find(a => string.Equals(avatarName, a.AvatarName));
            PositionSelection(found);
            _selected = found?.Avatar;
        }

        public void Refresh()
        {
            // remove all avatars, and recreate them.
            PositionSelection(null);

            for (var i = 0; i < ContentContainer.childCount; i++)
            {
                Destroy(ContentContainer.GetChild(i).gameObject);
            }

            var avatars = AccountManagementConfiguration.Instance.Overrides.GetAvailableAvatars();
            _avatarPreviews = new List<AccountAvatarBehaviour>();
            foreach (var avatar in avatars)
            {
                var avatarPreview = Instantiate(AvatarPreviewPrefab, ContentContainer);
                avatarPreview.Button.onClick.AddListener(() => SetPreviewAvatar(avatarPreview, avatar));
                _avatarPreviews.Add(avatarPreview);
                avatarPreview.Set(avatar);
            }
        }

        IEnumerator DeferScrollUpdate()
        {
            yield return Yielders.EndOfFrame;
            OnScrollUpdate();
        }


        public void SetPreviewAvatar(AccountAvatarBehaviour instance, AccountAvatar avatar)
        {
            Selected = avatar;
            OnSelected?.Invoke(Selected);
        }

        public void PositionSelection(AccountAvatarBehaviour selected)
        {
            if (selected == null)
            {
                AvatarSelectionIndicator.transform.SetParent(transform, false);
                AvatarSelectionIndicator?.SetActive(false);
                return;
            }
            AvatarSelectionIndicator.SetActive(true);
            AvatarSelectionIndicator.transform.SetParent(selected.transform, false);
            AvatarSelectionIndicator.transform.localPosition = Vector3.zero;

            Canvas.ForceUpdateCanvases();
            var currentScrollPosition = AvatarScroller.horizontalScrollbar.value * (AvatarScroller.content.rect.width - AvatarScroller.viewport.rect.width);
            var desiredScrollPosition = selected.transform.localPosition.x - .5f * AvatarScroller.viewport.rect.width;
            AvatarScroller.velocity = new Vector2(2 * (currentScrollPosition - desiredScrollPosition), 0);
        }

        public void OnScrollUpdate()
        {

            for (var i = 0; i < _avatarPreviews.Count; i++)
            {
                /*
                 * CanvasRenderers do not support MaterialPropertyBlocks, so getting custom
                 * data into each instance is hard.  https://forum.unity.com/threads/big-problem-with-lacking-materialpropertyblock-for-ui-image.506941/
                 * The work-around is to bake the relevant data into the per-instance color channel
                 */
                var totalWidth = AvatarScroller.viewport.rect.width +
                                 _avatarPreviews[i].Renderer.rectTransform.rect.width;
                var position = AvatarScroller.viewport.InverseTransformPoint(_avatarPreviews[i].transform.position).x;
                var xPercentage = ((position) / totalWidth) + .5f ;

                _avatarPreviews[i].Renderer.color = new Color(
                    // red channel represents the % x position
                    xPercentage,
                    // encode the number of elements in the entire scroll
                    1f / _avatarPreviews.Count,
                    // the current value of the scoller
                    AvatarScroller.horizontalScrollbar.value,
                    0);
            }

        }

    }
}
