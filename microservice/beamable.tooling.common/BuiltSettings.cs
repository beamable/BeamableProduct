using Beamable.Common;
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Beamable.Server.Common
{
	/// <summary>
	/// Built settings are defined in the build of the project. They are intended to be used with Microservice projects
	/// that are referencing the Beamable.Microservice.Runtime nuget package. That package introduces a custom build target
	/// that will create an embedded resource in the final dll for the service. The resource file bakes in values from
	/// <c>BeamableSetting</c> item elements in the csproj file. The "Include" attribute of the <c>BeamableSetting</c> acts
	/// as a key, and the "Value" attribute acts as the value.
	///
	/// <para>
	/// Use the <see cref="TryGetSetting"/> and <see cref="TryGetSettingFromJson{T}"/> functions to read this data.
	/// </para>
	/// </summary>
	public class BuiltSettings : IEnumerable<KeyValuePair<string, string>>
	{
		private IReadOnlyDictionary<string, string> _settings;

		/// <summary>
		/// Create a <see cref="BuiltSettings"/> from a dictionary
		/// </summary>
		/// <param name="settings"></param>
		public BuiltSettings(Dictionary<string, string> settings = null)
		{
			_settings = settings ?? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Create a <see cref="BuiltSettings"/> from the built-in resources
		/// </summary>
		/// <returns></returns>
		public static BuiltSettings FromResource()
		{
			return new BuiltSettings(ReadBuiltSettings());
		}

		/// <summary>
		/// Read a <c>BeamableSetting</c> from the .csproj file. 
		/// </summary>
		/// <param name="key">The key should be the "Include" attribute, case insensitive.</param>
		/// <param name="value">The output value will the contents of the "Value" attribute. </param>
		/// <returns>false if there was no setting. </returns>
		public bool TryGetSetting(string key, out string value) =>
			_settings.TryGetValue(key, out value);

		/// <summary>
		/// Read a <c>BeamableSetting</c> from the .csproj file, and assume that the value is well formatted JSON
		/// for the <typeparamref name="T"/> schema. 
		/// </summary>
		/// <param name="key">The key should be the "Include" attribute, case insensitive.</param>
		/// <param name="value">The output value will the contents of the "Value" attribute, deserialized from JSON</param>
		/// <typeparam name="T"></typeparam>
		/// <returns>false if there was no setting. </returns>
		public bool TryGetSettingFromJson<T>(string key, out T value)
		{
			value = default;
			if (!TryGetSetting(key, out var json)) return false;

			value = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { IncludeFields = true });
			return true;
		}

		/// <summary>
		/// Reads any <see cref="IFederation.ILocalSettings"/>-implementing structure from the <see cref="BuiltSettings"/>.
		/// </summary>
		public bool TryGetFederationLocalSettingFromJson<T>(string key, out T value) where T : class, IFederation.ILocalSettings
		{
			if (!FederationUtils.TrySplitLocalSettingKey(key, out _, out _))
				throw new ArgumentException($"The given key is not a valid {nameof(IFederation.ILocalSettings)} key. You should never see this. If you do, please contact Beamable support.");

			return TryGetSettingFromJson(key, out value);
		}

		public static IMicroserviceAttributes ReadServiceAttributes(IMicroserviceArgs args)
		{
			var settings = new Dictionary<string, string>();
			var assembly = Assembly.GetEntryAssembly();

			using var stream =
				assembly.GetManifestResourceStream(Constants.Features.Config.BEAMABLE_REQUIRED_SETTINGS_RESOURCE_NAME);
			if (stream == null)
			{
				if (args.AllowStartupWithoutBeamableSettings)
				{
					return null;
				}
				throw new InvalidOperationException($"Cannot start without {Constants.Features.Config.BEAMABLE_REQUIRED_SETTINGS_RESOURCE_NAME} resource");
			}

			var attributes = new DefaultMicroserviceAttributes();
			
			using var reader = new StreamReader(stream);
			while (!reader.EndOfStream)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrEmpty(line)) continue;
				var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2) continue;
				var name = parts[0].ToLowerInvariant().Trim();
				switch (name)
				{
					case "beamid":
						attributes.MicroserviceName = parts[1].Trim();
						break;
					// TODO: add more?
				}
				
			}

			return attributes;
		}
		
		/// <summary>
		/// Reads the embedded resource, Beamable.properties, and returns a dictionary of the key value pairs.
		/// 
		/// <para>
		/// In a .csproj file, you may use BeamableSetting items. The Include attribute will be the key, and the Value attribute will be the value
		/// </para>
		/// </summary>
		/// <returns>
		/// If there is no embedded resource, this will return an empty dictionary.
		/// </returns>
		public static Dictionary<string, string> ReadBuiltSettings()
		{
			var settings = new Dictionary<string, string>();
			var assembly = Assembly.GetEntryAssembly();

			using var stream =
				assembly.GetManifestResourceStream(Constants.Features.Config.BEAMABLE_SETTINGS_RESOURCE_NAME);
			if (stream == null) return new Dictionary<string, string>();

			using var reader = new StreamReader(stream);
			string result = reader.ReadToEnd();
			var lines = result.Split(new string[] { Constants.Features.Config.BEAMABLE_SETTINGS_RESOURCE_SPLITTER }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2) continue;

				var key = parts[0].Trim();
				var value = parts[1].Trim();

				if (!settings.ContainsKey(key))
				{
					settings[key] = value;
				}
			}

			return settings;
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _settings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
