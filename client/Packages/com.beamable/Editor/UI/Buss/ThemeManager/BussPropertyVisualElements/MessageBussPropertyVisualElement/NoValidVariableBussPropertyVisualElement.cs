using Beamable.UI.Buss;

namespace Beamable.Editor.UI.Components
{
	public class NoValidVariableBussPropertyVisualElement : MessageBussPropertyVisualElement
	{
		protected override string Message => "No valid variable found!";

		public NoValidVariableBussPropertyVisualElement(IBussProperty property) : base(property) { }
	}
}
