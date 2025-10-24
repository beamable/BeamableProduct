namespace Beamable.Common.BeamCli.Contracts
{
	public static class CliConstants
	{
		
		
		/// <summary>
		/// A magical constant that is used in the 'beam project generate-props` call to signify that the
		/// $(SolutionDir) should be set to the same place as the Directory.Build.Props file.
		/// </summary>
		public const string GENERATE_PROPS_SLN_NEXT_TO_PROPS = "DIR.PROPS";

		public const string PROP_PREFIX = "Beam";
		public const string PROP_BEAM_PROJECT_TYPE = PROP_PREFIX + "ProjectType";
		public const string PROP_BEAMO_ID = PROP_PREFIX + "Id";
		public const string PROP_LOG_PROVIDER = PROP_PREFIX + "LogProvider";
		public const string PROP_BEAM_ENABLED = PROP_PREFIX + "Enabled";
		public const string PROP_BEAM_SERVICE_GROUP = PROP_PREFIX + "ServiceGroup";
		public const string PROP_BEAM_STORAGE_DATA_VOLUME_NAME = PROP_PREFIX + "StorageDockerDataVolume";
		public const string PROP_BEAM_STORAGE_FILES_VOLUME_NAME = PROP_PREFIX + "StorageDockerFilesVolume";
		public const string PROP_BEAM_STORAGE_MONGO_BASE_IMAGE = PROP_PREFIX + "StorageLocalMongoImageTag";
		public const string ATTR_BEAM_REF_TYPE = PROP_PREFIX + "RefType";
		public static readonly char[] SPLIT_OPTIONS = new char[] { ',', ';' };
		public const string UNITY_ASSEMBLY_ITEM_NAME = "UnityAssembly";
		public const string HINT_PATH_ITEM_TAG = "HintPath";
		public const string PROJECT_BEAMABLE_SETTING = "BeamableSetting";
		public const int CONTENT_PUBLISH_BATCH_SIZE = 20;

		public static string BeamableSettingLabel(string beamId) => "CLI_SETTINGS_" + beamId;
	}
}
