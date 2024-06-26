using Beamable.Runtime.LightBeams;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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

		var index = GetSceneName(args, config);

		if (string.IsNullOrEmpty(index))
		{
			loadingBlocker.alpha = 0;
			loadingBlocker.gameObject.SetActive(false);

			sceneContainer.Clear();
			for (var i = 1; i < config.scenes.Count; i++)
			{
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
