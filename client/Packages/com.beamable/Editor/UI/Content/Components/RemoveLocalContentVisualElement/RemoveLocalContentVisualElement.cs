using Beamable.Editor.Content.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.UI.Components;

#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Content.Components
{
    public class RemoveLocalContentVisualElement : ContentManagerComponent
    {
        private LoadingBarElement _loadingBar;
        private Label _messageLabel;
        public event Action OnCancelled;
        public event Action OnCompleted;
        public ContentDataModel DataModel { get; set; }
        public Promise<DownloadSummary> RemoveSet { get; set; }
        private PrimaryButtonVisualElement _removeBtn;
        private bool _completed;

        public RemoveLocalContentVisualElement() : base(nameof(RemoveLocalContentVisualElement))
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            _loadingBar = Root.Q<LoadingBarElement>();
            _loadingBar.SmallBar = true;
            _loadingBar.Refresh();

            var mainContent = Root.Q<VisualElement>("removeLocal-mainVisualElement");
            var loadingBlocker = Root.Q<LoadingIndicatorVisualElement>();

            _removeBtn = Root.Q<PrimaryButtonVisualElement>("removeBtn");

            _messageLabel = Root.Q<Label>("message");
            _messageLabel.visible = false;

            var removeLocalCountElem = Root.Q<CountVisualElement>("removed");

            var toRemoveElem = Root.Q<Foldout>("toRemove");
            toRemoveElem.text = "To Remove";
            var removeSource = new List<ContentDownloadEntryDescriptor>();
            var removeList = new ListView
            {
                itemHeight = 24,
                itemsSource = removeSource,
                makeItem = MakeElement,
                bindItem = CreateBinder(removeSource)
            };


            toRemoveElem.contentContainer.Add(removeList);

            var cancelBtn = Root.Q<Button>("cancelBtn");
            cancelBtn.clickable.clicked += CancelButton_OnClicked;

            var promise = RemoveSet.Then(clearSet =>
            {
                var entries = DataModel.GetAllContents()?.Where(c => c.Status == ContentModificationStatus.LOCAL_ONLY).ToList();

                SetLocalRemoveMessage();
                removeLocalCountElem.SetValue(entries.Count);

                _removeBtn.Button.clickable.clicked += LocalRemoveButton_OnClicked;

                var noRemoveLabel = Root.Q<Label>("noRemoveLabel");
                noRemoveLabel.text = ContentManagerConstants.LocalRemoveNoDataText;
                noRemoveLabel.AddTextWrapStyle();

                if (entries.Count > 0)
                {
                    noRemoveLabel.parent.Remove(noRemoveLabel);
                }

                foreach (var toClear in entries)
                {
                    if (DataModel.GetDescriptorForId(toClear.Id, out var desc))
                    {
                        var data = new ContentDownloadEntryDescriptor
                        {
                            AssetPath = desc.AssetPath,
                            ContentId = toClear.Id,
                            Tags = desc.ServerTags?.ToArray(),
                            Operation = "delete",
                            Uri = ""
                        };
                        removeSource.Add(data);
                    }
                }

                toRemoveElem.Q<ListView>().style.height = removeList.itemHeight * removeSource.Count;
                removeList.Refresh();

                if (entries.Count == 0)
                {
                    removeList.parent.Remove(removeList);
                    toRemoveElem.parent.Remove(toRemoveElem);
                    _removeBtn.SetText("Okay");
                    _completed = true;
                }
            });

            loadingBlocker.SetPromise(promise, mainContent).SetText(ContentManagerConstants.LocalRemoveMessageLoading);
        }

        private void CancelButton_OnClicked()
        {
            OnCancelled?.Invoke();
        }

        private void LocalRemoveButton_OnClicked()
        {
            if (_completed)
            {
                OnCompleted?.Invoke();
            }
            else
            {
                HandleLocalRemove();
            }
        }

        private void HandleLocalRemove()
        {
            _removeBtn.Load(RemoveSet);
            RemoveSet.Then(_ =>
            {
                DataModel.DeleteLocalOnlyItems();

                _completed = true;
                _messageLabel.text = ContentManagerConstants.LocalRemoveCompleteMessage;
                _removeBtn.SetText("Okay");
                _loadingBar.RunWithoutUpdater = false;
            });
        }

        private ContentPopupLinkVisualElement MakeElement()
        {
            return new ContentPopupLinkVisualElement();
        }

        private Action<VisualElement, int> CreateBinder(List<ContentDownloadEntryDescriptor> source)
        {
            return (elem, index) =>
            {
                var link = elem as ContentPopupLinkVisualElement;
                link.Model = source[index];
                link.Refresh();
            };
        }

        private void SetLocalRemoveMessage()
        {
            EditorAPI.Instance.Then(api =>
            {
                _messageLabel.visible = true;
                _messageLabel.AddTextWrapStyle();
                _messageLabel.text = ContentManagerConstants.LocalRemoveMessagePreview;
            });
        }
    }
}

