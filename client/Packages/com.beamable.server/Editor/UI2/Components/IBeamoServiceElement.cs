using Usam;

namespace Beamable.Editor.Microservice.UI2.Components
{
	public interface IBeamoServiceElement
	{
		public void FeedData(IBeamoServiceDefinition definition, BeamEditorContext editorContext);
	}
}
