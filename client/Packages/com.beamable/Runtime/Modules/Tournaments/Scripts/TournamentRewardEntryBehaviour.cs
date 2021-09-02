using Beamable.Common.Inventory;
using Beamable.Common.Shop;
using Beamable.Inventory;
using Beamable.Shop;
using Beamable.UI.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Tournaments
{
    public class TournamentRewardEntryBehaviour : MonoBehaviour
    {
        public Image Image;
        public TextReference AmountText;
        public Material GreyMaterial;
        private OfferObtainCurrency _data;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Set(TournamentEntryViewData owner, OfferObtainCurrency data)
        {
            _data = data;
            var currencyRef = data.symbol;

            currencyRef.Resolve().Then(async currency =>
            {
                if (!Image) return;
                var sprite = await currency.icon.LoadSprite();
                if (!Image) return;
                Image.sprite = sprite;
                Image.material = owner.IsGrey ? GreyMaterial : null;
                if (!AmountText) return;
                AmountText.Value = TournamentScoreUtil.GetShortScore((ulong)data.amount);
            });
        }
    }
}
