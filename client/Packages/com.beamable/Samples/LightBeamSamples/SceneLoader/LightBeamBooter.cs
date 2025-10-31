using Beamable.Runtime.LightBeams;
using System;
using System.Collections.Generic;
using Beamable;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Config;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[BeamContextSystem]
public class LightBeamBooter : MonoBehaviour
{
	[Header("Asset References")]
	public LightBeamSceneConfigObject config;
	public SceneDisplayBehaviour sceneDisplayTemplate;

	[Header("Scene References")]
	public RectTransform sceneContainer;
	public CanvasGroup loadingBlocker;

	// Start is called before the first frame update
	void Start()
	{
		loadingBlocker.gameObject.SetActive(true);
		loadingBlocker.alpha = 1;


		var args = GetArgs();
		LightBeamUtilExtensions.Hints = args;

		SetBeamEnv(args);
		var index = GetSceneName(args, config);

		Debug.Log("LIGHTBEAM_BOOT_COMPLETE");
		if (string.IsNullOrEmpty(index))
		{
			loadingBlocker.alpha = 0;
			loadingBlocker.gameObject.SetActive(false);

			sceneContainer.Clear();
			for (var i = 1; i < config.scenes.Count; i++)
			{
				if (!config.scenes[i].includeInToc) continue;
				
				var instance = Instantiate(sceneDisplayTemplate, sceneContainer);
				instance.Configure(config.scenes[i]);
			}
		}
		else
		{
			SceneManager.LoadSceneAsync(index);
		}
	}

	static Dictionary<string, string> GetArgs()
	{
		var url = Application.absoluteURL;
		var queryStartIndex = url.IndexOf("?", StringComparison.Ordinal);
		if (queryStartIndex == -1)
		{
			Debug.LogWarning("no query args");
			return new Dictionary<string, string>();
		}

		var queryStr = url.Substring(queryStartIndex + 1); // consume the ?
		var queryArgStrs = queryStr.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

		var queryArgs = new Dictionary<string, string>();

		foreach (var query in queryArgStrs)
		{
			var parts = query.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
			switch (parts.Length)
			{
				case 1:
					queryArgs[parts[0]] = "";
					break;
				case 2:
					queryArgs[parts[0]] = UnityWebRequest.UnEscapeURL(parts[1]);
					break;
				default:
					break;
			}
		}

		return queryArgs;
	}

	static string GetSampleHint(Dictionary<string, string> args)
	{
		return args.TryGetValue("hint", out var hint) ? hint : null;
	}

	static void SetBeamEnv(Dictionary<string, string> args)
	{
		if (args.TryGetValue("cid", out var cid))
		{
			wrapper.SetCid(cid);
		}

		if (args.TryGetValue("pid", out var pid))
		{
			wrapper.SetPid(pid);
		}

		if (args.TryGetValue("host", out var host))
		{
			wrapper.SetHost(host);
		}

		if (args.TryGetValue("refresh_token", out var refresh))
		{
			wrapper.RefreshToken = refresh;
		}
		if (args.TryGetValue("access_token", out var access))
		{
			wrapper.AccessToken = refresh;
		}
	}

	public static CidPidWrapper wrapper = new CidPidWrapper();


#if BEAM_LIGHTBEAM
	[RegisterBeamableDependencies(origin: RegistrationOrigin.RUNTIME_GLOBAL)]
	public static void Configure(IDependencyBuilder builder)
	{
		builder.RemoveIfExists<IRuntimeConfigProvider>();
		builder.AddSingleton<IRuntimeConfigProvider, CidPidWrapper>(wrapper);

		builder.RemoveIfExists<IBeamDeveloperAuthProvider>();
		builder.AddSingleton<IBeamDeveloperAuthProvider>(wrapper);
	}
#endif

	public class CidPidWrapper : IRuntimeConfigProvider, IBeamDeveloperAuthProvider
	{
		private ConfigDatabaseProvider _fileBased;

		private string customCid, customPid, customHost;

		public string Cid => customCid ?? _fileBased.Cid;
		public string Pid => customPid ?? _fileBased.Pid;
		public string HostUrl
		{
			get
			{
				return customHost ?? _fileBased.HostUrl;
			}
		}

		public string PortalUrl => _fileBased.PortalUrl;

		public CidPidWrapper()
		{
			_fileBased = new ConfigDatabaseProvider();
		}

		public void SetCid(string value)
		{
			Debug.Log("Overriding cid to " + value);
			customCid = value;
		}

		public void SetPid(string value)
		{
			Debug.Log("Overriding pid to " + value);
			customPid = value;
		}

		public void SetHost(string value)
		{
			Debug.Log("Overriding host to " + value);
			customHost = value;
		}

		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
	}

	static string GetSceneName(Dictionary<string, string> args, LightBeamSceneConfigObject config)
	{
		if (!args.TryGetValue("scene", out var sceneName))
		{
			Debug.LogWarning("no scene arg");
			return null;
		}

		if (config.TryGetScene(sceneName, out var scene))
		{
			return scene.sceneName;
		}

		Debug.LogWarning($"no parsable scene arg- There are {config.scenes} scenes...");
		foreach (var existing in config.scenes)
		{
			Debug.LogWarning("\t" + existing.label);
		}
		return null;

	}

}
