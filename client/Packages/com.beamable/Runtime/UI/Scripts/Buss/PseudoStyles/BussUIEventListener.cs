using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Beamable.UI.Buss.PseudoStyles
{
	[RequireComponent(typeof(BussElement))]
	public class BussUIEventListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private BussElement _bussElement;

		private void Start()
		{
			_bussElement = GetComponent<BussElement>();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_bussElement.SetPseudoClass(PseudoClassNames.Hover, true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_bussElement.SetPseudoClass(PseudoClassNames.Hover, false);
		}
	}
}
