using System;
using System.Collections.Generic;
using Beamable;
using Beamable.Announcements;
using Modules.Inventory.Prototype;
using Modules.Inventory.Prototype.GenericComponents;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Modules.Content
{
    public class AnnouncementsPresenter : CollectionPresenter<AnnouncementsCollection>
    {
#pragma warning disable CS0649
        [SerializeField] private GameObject announcementRowPrefab;
        [SerializeField] private TextMeshProUGUI noItemsText;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Button closeButton;
#pragma warning restore CS0649

        private void Awake()
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            Collection = new AnnouncementsCollection(OnCollectionUpdated);
        }

        private void OnDisable()
        {
            Collection?.Unsubscribe();
            Collection = null;
        }

        private void OnCollectionUpdated()
        {
            noItemsText.gameObject.SetActive(Collection.Count == 0);

            foreach (var announcement in Collection)
            {
                AnnouncementSummary row = Instantiate(announcementRowPrefab, listRoot).GetComponent<AnnouncementSummary>();
                Assert.IsNotNull(row, $"Instantiation of {nameof(AnnouncementSummary)} failed");
                row.Setup(announcement.title, announcement.body);
            }
        }

        public async void OnClaimAll()
        {
            List<string> ids = new List<string>();
            foreach (var announcement in Collection)
            {
                ids.Add(announcement.id);
            }

            var api = await API.Instance;
            await api.AnnouncementService.Claim(ids);
        }
    }
}
