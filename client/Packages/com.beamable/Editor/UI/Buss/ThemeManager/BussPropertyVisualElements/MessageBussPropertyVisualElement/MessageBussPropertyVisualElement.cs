using Beamable.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public abstract class MessageBussPropertyVisualElement : BussPropertyVisualElement<IBussProperty>
	{
		protected abstract string Message
		{
			get;
		}

		public MessageBussPropertyVisualElement(IBussProperty property) : base(property) { }

		public override void Init()
		{
			base.Init();
			var label = new Label(Message);
			AddBussPropertyFieldClass(label);
			label.style.SetFontSize(8f);
			label.AddTextWrapStyle();
			Root.Add(label);
		}

		public override void OnPropertyChangedExternally()
		{

		}
	}
}
