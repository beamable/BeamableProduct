using UnityEditor;

namespace Beamable.Editor.Config.Model
{
	public class ConfigOption
	{
		public SerializedProperty Property;
		public SerializedObject Object;
		public string Name;
		public string Module;

		public ConfigOption()
		{

		}

		public ConfigOption(SerializedObject obj, BaseModuleConfigurationObject config, SerializedProperty property)
		{
			Object = obj;
			Property = property;
			Name = property.displayName;
			Module = config.GetType().Name.Replace("Configuration", "");
		}
	}

}
