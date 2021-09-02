using System.Collections;
using System.Collections.Generic;
using Beamable.UI.Scripts;
using UnityEngine;

namespace Beamable.Tournaments
{

    public class TournamentInfoDetailContainerBehaviour : MonoBehaviour
    {
        public TextReference Title;
        public RectTransform Container;

        private Dictionary<TournamentInfoPageSection, GameObject> _instanceTable = new Dictionary<TournamentInfoPageSection, GameObject>();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Set(TournamentInfoPageSection infoPageSection)
        {
            Title.Value = infoPageSection.DetailTitle;
            foreach (var other in _instanceTable.Values)
            {
                other.SetActive(false);
            }
            if (_instanceTable.TryGetValue(infoPageSection, out var instance))
            {
                instance.SetActive(true);
            }
            else
            {
                var newPage = Instantiate(infoPageSection.DetailPrefab, Container);
                _instanceTable.Add(infoPageSection, newPage.gameObject);
            }
        }
    }
}